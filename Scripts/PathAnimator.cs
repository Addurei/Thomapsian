using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// PathAnimator — luxury Google Maps-style navigation path for Thomapsian.
///
/// Attach to any persistent GameObject in the scene (e.g. a "NavigationSystem" empty).
/// Assign the NavArrow material in the Inspector or let Awake() load it automatically.
///
/// API:
///   PathAnimator.Instance.SetDestination(Vector3 worldPos)
///   PathAnimator.Instance.ClearPath()
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class PathAnimator : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static PathAnimator Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Path Appearance")]
    [Tooltip("The NavArrow material (Thomapsian/NavArrow shader). Auto-loaded if blank.")]
    public Material navArrowMaterial;

    [Tooltip("Width of the path line in world units.")]
    public float lineWidth = 1.2f;

    [Tooltip("Y-offset so the path floats above the floor and never clips.")]
    public float yOffset = 0.2f;

    [Tooltip("Sorting order – high value = drawn over everything.")]
    public int sortingOrder = 200;

    [Header("Animation")]
    [Tooltip("Speed at which the chevron arrows scroll toward the destination.")]
    public float scrollSpeed = 1.5f;

    [Header("Arrow Heads")]
    [Tooltip("Draw small directional chevron markers along the path.")]
    public bool showArrowHeads = true;

    [Tooltip("Distance between each arrowhead marker (world units).")]
    public float arrowHeadSpacing = 3.0f;

    [Tooltip("Size of each arrowhead marker.")]
    public float arrowHeadSize = 0.6f;

    // ── Private ───────────────────────────────────────────────────────────────
    private LineRenderer     _line;
    private NavMeshPath      _navPath;
    private float            _scrollOffset;
    private NavMeshAgent     _trackedAgent;

    private class ArrowHeadData
    {
        public LineRenderer lr;
        public float distanceToDest;
    }
    private List<ArrowHeadData> _arrowHeadsData = new List<ArrowHeadData>();
    private static readonly int OffsetProp = Shader.PropertyToID("_Offset");

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _navPath = new NavMeshPath();

        // LineRenderer setup
        _line = GetComponent<LineRenderer>();
        _line.useWorldSpace       = true;
        _line.positionCount       = 0;
        _line.startWidth          = lineWidth;
        _line.endWidth            = lineWidth;
        _line.sortingOrder        = sortingOrder;
        _line.shadowCastingMode   = UnityEngine.Rendering.ShadowCastingMode.Off;
        _line.receiveShadows      = false;
        _line.textureMode         = LineTextureMode.Tile;

        // Assign or load the NavArrow material
        if (navArrowMaterial == null)
        {
            Shader s = Shader.Find("Thomapsian/NavArrow");
            if (s != null)
            {
                navArrowMaterial = new Material(s);
            }
            else
            {
                Debug.LogWarning("[PathAnimator] NavArrow shader not found. " +
                                 "Make sure NavArrow.shader is in Assets/Shaders/.");
                // Fallback: solid yellow unlit
                navArrowMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
                {
                    color = new Color(1f, 0.85f, 0f, 1f)
                };
            }
        }

        _line.material = navArrowMaterial;
        _line.enabled  = false;
    }

    private void Update()
    {
        if (!_line.enabled) return;

        // Scroll the UV offset – drives the shader's _Offset uniform
        _scrollOffset += scrollSpeed * Time.deltaTime;
        _line.material.SetFloat(OffsetProp, _scrollOffset);

        if (_trackedAgent != null)
        {
            if (!_trackedAgent.pathPending && _trackedAgent.hasPath)
            {
                if (_trackedAgent.remainingDistance > 0.2f)
                {
                    Vector3[] corners = _trackedAgent.path.corners;
                    Vector3[] lifted = new Vector3[corners.Length];
                    for (int i = 0; i < corners.Length; i++)
                        lifted[i] = corners[i] + Vector3.up * yOffset;

                    _line.positionCount = lifted.Length;
                    _line.SetPositions(lifted);

                    // Update ArrowHeads visibility
                    float agentDist = _trackedAgent.remainingDistance;
                    foreach (var ah in _arrowHeadsData)
                    {
                        if (ah.lr != null)
                        {
                            bool shouldBeVisible = ah.distanceToDest <= agentDist + 1.5f;
                            ah.lr.enabled = shouldBeVisible;
                        }
                    }
                }
                else
                {
                    ClearPath();
                }
            }
        }

        // Keep arrowhead UVs in sync
        SyncArrowHeadMaterials();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Tracks a NavMeshAgent, dynamically shortening the path and hiding passed arrowheads.
    /// </summary>
    public void TrackAgent(NavMeshAgent agent, Vector3 destination)
    {
        _trackedAgent = null; // Don't auto-track – we show a static path only

        // Snap destination to valid NavMesh (large radius in case building is large)
        NavMeshHit hit;
        if (NavMesh.SamplePosition(destination, out hit, 100f, NavMesh.AllAreas))
        {
            destination = hit.position;
        }

        // IMPORTANT: Do NOT call agent.SetDestination – the character must stay still.
        // Freeze the agent in place so no other script can move it during path display.
        agent.isStopped = true;
        agent.ResetPath();

        // Pre-calculate full path for ArrowHeads so they stay static
        NavMeshPath fullPath = new NavMeshPath();
        if (NavMesh.CalculatePath(agent.transform.position, destination, NavMesh.AllAreas, fullPath))
        {
            DrawPathCorners(fullPath.corners);
        }
        else
        {
            ClearArrowHeads();
            _line.enabled = true;
        }
    }

    /// <summary>
    /// Calculate a static NavMesh path from <paramref name="origin"/> to <paramref name="destination"/>.
    /// If an <paramref name="agent"/> is provided, its radius is used to keep the path centered in corridors.
    /// </summary>
    public void SetDestination(Vector3 origin, Vector3 destination, NavMeshAgent agent = null)
    {
        _trackedAgent = null; // stop tracking

        // Snap to NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(origin, out hit, 100f, NavMesh.AllAreas)) origin = hit.position;
        if (NavMesh.SamplePosition(destination, out hit, 100f, NavMesh.AllAreas)) destination = hit.position;

        Debug.Log($"<b>[PathAnimator]</b> Calculating path from {origin} to {destination}...");
        
        bool found = false;
        if (agent != null)
        {
            // Use agent-specific calculation to respect its radius (centers path in corridors)
            found = agent.CalculatePath(destination, _navPath);
        }
        else
        {
            found = NavMesh.CalculatePath(origin, destination, NavMesh.AllAreas, _navPath);
        }

        Vector3[] corners;
        if (!found || _navPath.status == NavMeshPathStatus.PathInvalid)
        {
            Debug.LogError("<b>[PathAnimator]</b> NAVMESH PATH FAILED! Falling back to straight line.");
            corners = new Vector3[] { origin, destination };
        }
        else
        {
            corners = _navPath.corners;
        }

        DrawPathCorners(corners);
    }

    private void DrawPathCorners(Vector3[] corners)
    {
        Vector3[] lifted = new Vector3[corners.Length];
        for (int i = 0; i < corners.Length; i++)
            lifted[i] = corners[i] + Vector3.up * yOffset;

        _line.positionCount = lifted.Length;
        _line.SetPositions(lifted);
        _line.enabled = true;

        if (showArrowHeads)
            BuildArrowHeads(lifted);
        else
            ClearArrowHeads();
    }

    /// <summary>
    /// Hides and resets the navigation path.
    /// </summary>
    public void ClearPath()
    {
        _line.positionCount = 0;
        _line.enabled       = false;
        _scrollOffset       = 0f;
        _trackedAgent       = null;
        ClearArrowHeads();

        // Re-enable the agent so it can be Warped to the destination on "Reached"
        NavMeshAgent agent = FindFirstObjectByType<NavMeshAgent>();
        if (agent != null)
        {
            agent.isStopped = false;
            agent.ResetPath();
        }
    }

    // ── Arrow head helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Spawn small ">" LineRenderer chevrons at regular intervals along the path.
    /// </summary>
    private void BuildArrowHeads(Vector3[] path)
    {
        ClearArrowHeads();

        if (path.Length < 2) return;

        float totalPathLength = 0f;
        float[] segmentLengths = new float[path.Length - 1];
        for (int i = 0; i < path.Length - 1; i++) 
        {
            segmentLengths[i] = Vector3.Distance(path[i], path[i+1]);
            totalPathLength += segmentLengths[i];
        }

        float accumulatedFromStart = arrowHeadSpacing * 0.5f;
        for (int i = 0; i < path.Length - 1; i++)
        {
            Vector3 segStart = path[i];
            Vector3 segEnd   = path[i + 1];
            float segLen     = segmentLengths[i];
            Vector3 segDir   = (segEnd - segStart).normalized;

            while (accumulatedFromStart <= segLen)
            {
                Vector3 pos = segStart + segDir * accumulatedFromStart;
                
                float distFromStart = 0;
                for(int j = 0; j < i; j++) distFromStart += segmentLengths[j];
                distFromStart += accumulatedFromStart;
                
                float distToDest = totalPathLength - distFromStart;

                SpawnArrowHead(pos, segDir, distToDest);
                accumulatedFromStart += arrowHeadSpacing;
            }
            accumulatedFromStart -= segLen;
        }
    }

    private void SpawnArrowHead(Vector3 center, Vector3 forward, float distToDest)
    {
        GameObject go = new GameObject("ArrowHead");
        go.transform.SetParent(transform);
        go.transform.position = center + Vector3.up * yOffset;

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace     = true;
        lr.positionCount     = 3;
        lr.startWidth        = arrowHeadSize * 0.5f;
        lr.endWidth          = arrowHeadSize * 0.5f;
        lr.sortingOrder      = sortingOrder + 1;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows    = false;
        lr.material          = navArrowMaterial;

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        float   half  = arrowHeadSize * 0.5f;

        Vector3 leftWing  = center - forward * half + right * half;
        Vector3 tip       = center + forward * half;
        Vector3 rightWing = center - forward * half - right * half;

        lr.SetPosition(0, leftWing  + Vector3.up * yOffset);
        lr.SetPosition(1, tip       + Vector3.up * yOffset);
        lr.SetPosition(2, rightWing + Vector3.up * yOffset);

        _arrowHeadsData.Add(new ArrowHeadData { lr = lr, distanceToDest = distToDest });
    }

    private void ClearArrowHeads()
    {
        foreach (var ah in _arrowHeadsData)
        {
            if (ah.lr != null) Destroy(ah.lr.gameObject);
        }
        _arrowHeadsData.Clear();
    }

    private void SyncArrowHeadMaterials()
    {
        foreach (var ah in _arrowHeadsData)
        {
            if (ah.lr != null && ah.lr.enabled)
                ah.lr.material.SetFloat(OffsetProp, _scrollOffset);
        }
    }
}

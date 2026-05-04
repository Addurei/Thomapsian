using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

/// <summary>
/// EnvironmentManager — global scene enforcer for Thomapsian.
///
/// Attach to any persistent GameObject in the scene (e.g. "NavigationSystem").
/// Runs automatically on Awake before any other script logic.
///
/// Responsibilities:
///   1. Canvas Scaler: Ensures CampusCanvas is set to ScaleWithScreenSize @ 1920x1080
///   2. NavMeshObstacles: Injects NavMeshObstacle (Carve) on all buildings
///   3. Collider Audit: Ensures buildings and walls have Static BoxColliders
/// </summary>
public class EnvironmentManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static EnvironmentManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Canvas Settings")]
    [Tooltip("Name of the root Canvas GameObject in the scene.")]
    public string canvasName = "CampusCanvas";

    public Vector2 referenceResolution = new Vector2(1920, 1080);

    [Header("Obstacle Settings")]
    [Tooltip("Tags to search for when injecting NavMeshObstacles.")]
    public string[] buildingTags = { "Building" };

    [Tooltip("Name fragments to search for when tags aren't set.")]
    public string[] buildingNameFragments = { "Building", "Wall", "Block" };

    [Tooltip("NavMeshObstacle carving move threshold (0 = always carve).")]
    public float carveThreshold = 0.01f;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        EnforceCanvasScaler();
        InjectNavMeshObstacles();

        // Auto-add the NavigationFlowManager to handle the "Where are you?" popup
        if (GetComponent<NavigationFlowManager>() == null)
        {
            gameObject.AddComponent<NavigationFlowManager>();
        }
    }

    // ── Canvas Scaler ─────────────────────────────────────────────────────────

    private void EnforceCanvasScaler()
    {
        // Find canvas by name first; fall back to FindFirstObjectByType
        GameObject canvasGO = GameObject.Find(canvasName);
        Canvas canvas = canvasGO != null
            ? canvasGO.GetComponent<Canvas>()
            : FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            Debug.LogWarning("[EnvironmentManager] No Canvas found in scene — skipping Canvas Scaler fix.");
            return;
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            Debug.Log("[EnvironmentManager] Added missing CanvasScaler to Canvas.");
        }

        bool changed = false;

        if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            changed = true;
        }

        if (scaler.referenceResolution != referenceResolution)
        {
            scaler.referenceResolution = referenceResolution;
            changed = true;
        }

        // Use Screen Match Mode: Match Width Or Height, balanced at 0.5
        if (scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
        {
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            changed = true;
        }

        if (changed)
            Debug.Log($"[EnvironmentManager] Canvas Scaler corrected → ScaleWithScreenSize @ {referenceResolution.x}×{referenceResolution.y}");
        else
            Debug.Log($"[EnvironmentManager] Canvas Scaler OK — {referenceResolution.x}×{referenceResolution.y}");
    }

    // ── NavMeshObstacle injection ─────────────────────────────────────────────

    private void InjectNavMeshObstacles()
    {
        int injected  = 0;
        int colliders = 0;

        // Collect all candidate GameObjects
        var candidates = new System.Collections.Generic.HashSet<GameObject>();

        // By tag
        foreach (string tag in buildingTags)
        {
            try
            {
                foreach (GameObject go in GameObject.FindGameObjectsWithTag(tag))
                    candidates.Add(go);
            }
            catch (UnityException)
            {
                // Tag doesn't exist in project — skip silently
            }
        }

        // By name fragment (catches objects without tags)
        foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            foreach (string fragment in buildingNameFragments)
            {
                if (go.name.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    candidates.Add(go);
                    break;
                }
            }
        }

        foreach (GameObject go in candidates)
        {
            // ── Collider ──────────────────────────────────────────────────────
            Collider col = go.GetComponent<Collider>();
            BoxCollider box = col as BoxCollider;

            if (col == null)
            {
                box = go.AddComponent<BoxCollider>();
                colliders++;
            }

            // Ensure BoxCollider accurately encapsulates all child renderers
            if (box != null)
            {
                Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                    {
                        bounds.Encapsulate(renderers[i].bounds);
                    }

                    Vector3 scale = go.transform.lossyScale;
                    if (Mathf.Abs(scale.x) > 0.0001f && Mathf.Abs(scale.y) > 0.0001f && Mathf.Abs(scale.z) > 0.0001f)
                    {
                        box.center = go.transform.InverseTransformPoint(bounds.center);
                        box.size = new Vector3(bounds.size.x / scale.x, bounds.size.y / scale.y, bounds.size.z / scale.z);
                    }
                }
            }

            // Force static so physics broadphase treats it as immovable
            if (!go.isStatic)
                go.isStatic = true;

            // ── NavMeshObstacle ───────────────────────────────────────────────
            NavMeshObstacle obstacle = go.GetComponent<NavMeshObstacle>();
            if (obstacle == null)
            {
                obstacle = go.AddComponent<NavMeshObstacle>();
                injected++;
            }

            obstacle.carving              = true;
            obstacle.carveOnlyStationary  = false;
            obstacle.carvingMoveThreshold = carveThreshold;

            // Match obstacle size to existing BoxCollider for accuracy
            if (box != null)
            {
                obstacle.shape  = NavMeshObstacleShape.Box;
                obstacle.size   = box.size;
                obstacle.center = box.center;
            }
        }

        Debug.Log($"[EnvironmentManager] NavMeshObstacle pass complete — " +
                  $"Injected: {injected} obstacles, Fixed: {colliders} missing colliders " +
                  $"across {candidates.Count} objects.");
    }
}

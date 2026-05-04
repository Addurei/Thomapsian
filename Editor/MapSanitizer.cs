using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections.Generic;

// Suppress obsolete warnings for NavigationStatic — still needed for legacy baked nav
#pragma warning disable CS0618

public class MapSanitizer : EditorWindow
{
    [MenuItem("Tools/Campus/Run Map Sanitizer")]
    public static void SanitizeMap()
    {
        GameObject root = GameObject.Find("Campus_Map_Blockout");
        if (root == null)
        {
            Debug.LogError("Could not find 'Campus_Map_Blockout' root object. " +
                           "Please ensure it exists in the active scene.");
            return;
        }

        int scaledBuildings = 0;
        int closedGaps      = 0;
        int fixedColliders  = 0;
        int obstacles       = 0;

        var buildings = new List<Transform>();
        var pathways  = new List<Transform>();
        var walls     = new List<Transform>();

        // ── 1. Collect objects ────────────────────────────────────────────────
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child == root.transform) continue;

            if (child.CompareTag("Building") || child.name.IndexOf("Building", System.StringComparison.OrdinalIgnoreCase) >= 0)
                buildings.Add(child);
            else if (child.CompareTag("Pathway") || child.name.IndexOf("Pathway", System.StringComparison.OrdinalIgnoreCase) >= 0)
                pathways.Add(child);
            else if (child.CompareTag("Wall") || child.name.IndexOf("Wall", System.StringComparison.OrdinalIgnoreCase) >= 0)
                walls.Add(child);
        }

        // ── 2. Fix Building Scaling ───────────────────────────────────────────
        foreach (Transform b in buildings)
        {
            Vector3 s    = b.localScale;
            float   newX = Mathf.Round(s.x * 2f) / 2f;
            float   newZ = Mathf.Round(s.z * 2f) / 2f;
            int     floors = Mathf.Max(1, Mathf.RoundToInt(s.y / 4f));
            float   newY = floors * 4f;

            if (Mathf.Abs(s.x - newX) > 0.01f ||
                Mathf.Abs(s.y - newY) > 0.01f ||
                Mathf.Abs(s.z - newZ) > 0.01f)
            {
                Undo.RecordObject(b, "Scale Building");
                b.localScale = new Vector3(newX, newY, newZ);
                scaledBuildings++;
                EditorUtility.SetDirty(b.gameObject);
            }
        }

        // ── 3. Colliders + Static + NavMeshObstacle (Buildings & Walls) ───────
        var collisionObjects = new List<Transform>();
        collisionObjects.AddRange(buildings);
        collisionObjects.AddRange(walls);

        foreach (Transform obj in collisionObjects)
        {
            // Ensure BoxCollider
            BoxCollider box = obj.GetComponent<BoxCollider>();
            if (box == null)
            {
                box = Undo.AddComponent<BoxCollider>(obj.gameObject);
                fixedColliders++;
                EditorUtility.SetDirty(obj.gameObject);
            }

            // Always fix BoxCollider bounds to encapsulate child renderers
            if (box != null)
            {
                Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                    {
                        bounds.Encapsulate(renderers[i].bounds);
                    }

                    Vector3 scale = obj.lossyScale;
                    if (Mathf.Abs(scale.x) > 0.0001f && Mathf.Abs(scale.y) > 0.0001f && Mathf.Abs(scale.z) > 0.0001f)
                    {
                        Vector3 newCenter = obj.InverseTransformPoint(bounds.center);
                        Vector3 newSize = new Vector3(bounds.size.x / scale.x, bounds.size.y / scale.y, bounds.size.z / scale.z);

                        if (box.center != newCenter || box.size != newSize)
                        {
                            Undo.RecordObject(box, "Recalculate BoxCollider Size");
                            box.center = newCenter;
                            box.size = newSize;
                            EditorUtility.SetDirty(box);
                        }
                    }
                }
            }

            // Force static
            if (!obj.gameObject.isStatic)
            {
                Undo.RecordObject(obj.gameObject, "Set Static");
                obj.gameObject.isStatic = true;
                EditorUtility.SetDirty(obj.gameObject);
            }

            // NavMeshObstacle with Carve (runtime anti-ghosting)
            NavMeshObstacle obs = obj.GetComponent<NavMeshObstacle>();
            if (obs == null)
            {
                obs = Undo.AddComponent<NavMeshObstacle>(obj.gameObject);
                obstacles++;
                EditorUtility.SetDirty(obj.gameObject);
            }
            obs.carving              = true;
            obs.carveOnlyStationary  = false;
            obs.carvingMoveThreshold = 0.01f;

            if (box != null)
            {
                obs.shape  = NavMeshObstacleShape.Box;
                obs.size   = box.size;
                obs.center = box.center;
            }
        }

        // ── 4. Close Pathway Gaps ─────────────────────────────────────────────
        for (int i = 0; i < pathways.Count; i++)
        {
            Collider c1 = pathways[i].GetComponent<Collider>();
            if (c1 == null)
            {
                Undo.AddComponent<BoxCollider>(pathways[i].gameObject);
                c1 = pathways[i].GetComponent<Collider>();
            }

            for (int j = i + 1; j < pathways.Count; j++)
            {
                Collider c2 = pathways[j].GetComponent<Collider>();
                if (c2 == null) continue;

                Bounds b1 = c1.bounds;
                Bounds b2 = c2.bounds;

                if (!b1.Intersects(b2))
                {
                    b1.Expand(0.2f);
                    if (b1.Intersects(b2))
                    {
                        Undo.RecordObject(pathways[i], "Close Pathway Gap");
                        Vector3 ns = pathways[i].localScale;
                        ns.x += 0.1f;
                        ns.z += 0.1f;
                        pathways[i].localScale = ns;
                        closedGaps++;
                        EditorUtility.SetDirty(pathways[i].gameObject);
                        c1 = pathways[i].GetComponent<Collider>();
                    }
                }
            }
        }

        // ── 5. NavMesh flags on Pathways ──────────────────────────────────────
        foreach (Transform pathway in pathways)
        {
            Undo.RecordObject(pathway.gameObject, "Set Pathway NavMesh Static");
            GameObjectUtility.SetStaticEditorFlags(pathway.gameObject,
                GameObjectUtility.GetStaticEditorFlags(pathway.gameObject) |
                StaticEditorFlags.NavigationStatic);

            NavMeshModifier mod = pathway.GetComponent<NavMeshModifier>();
            if (mod == null)
                mod = Undo.AddComponent<NavMeshModifier>(pathway.gameObject);

            mod.overrideArea = true;
            mod.area         = 0; // Walkable
            EditorUtility.SetDirty(pathway.gameObject);
        }

        // ── 6. NavMesh flags on Buildings ─────────────────────────────────────
        foreach (Transform building in buildings)
        {
            Undo.RecordObject(building.gameObject, "Set Building NavMesh Static");
            GameObjectUtility.SetStaticEditorFlags(building.gameObject,
                GameObjectUtility.GetStaticEditorFlags(building.gameObject) |
                StaticEditorFlags.NavigationStatic);

            NavMeshModifier mod = building.GetComponent<NavMeshModifier>();
            if (mod == null)
                mod = Undo.AddComponent<NavMeshModifier>(building.gameObject);

            mod.overrideArea = true;
            mod.area         = 1; // Not Walkable
            EditorUtility.SetDirty(building.gameObject);
        }

        // ── 7. Bake NavMesh via NavMeshSurface ────────────────────────────────
        // Uses the modern Unity AI Navigation package (NavMeshSurface).
        // If a NavMeshSurface exists in the scene, bake it; otherwise log instructions.
        NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();
        if (surface != null)
        {
            surface.BuildNavMesh();
            Debug.Log("[MapSanitizer] NavMesh baked via NavMeshSurface.");
        }
        else
        {
            Debug.LogWarning("[MapSanitizer] No NavMeshSurface found — skipping bake. " +
                             "Add a NavMeshSurface component to your scene and run again, " +
                             "or bake manually via Window > AI > Navigation.");
        }

        Debug.Log($"<b>Map Sanitization Complete!</b>\n" +
                  $"  Scaled Buildings : {scaledBuildings}\n" +
                  $"  Closed Gaps      : {closedGaps}\n" +
                  $"  Fixed Colliders  : {fixedColliders}\n" +
                  $"  NavMesh Obstacles: {obstacles}");
    }
}

#pragma warning restore CS0618

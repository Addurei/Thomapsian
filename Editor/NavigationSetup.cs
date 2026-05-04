using UnityEngine;
using UnityEditor;
using UnityEngine.AI;

/// <summary>
/// NavigationSetup — one-click scene wiring for the Thomapsian navigation system.
/// Run via: Tools > Campus > Setup Navigation System
/// </summary>
public static class NavigationSetup
{
    [MenuItem("Tools/Campus/Setup Navigation System")]
    public static void Setup()
    {
        // ── 1. Remove stale NavigationSystem if it exists ────────────────────
        GameObject existing = GameObject.Find("NavigationSystem");
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
            Debug.Log("[NavigationSetup] Removed old NavigationSystem.");
        }

        // ── 2. Create NavigationSystem root ───────────────────────────────────
        GameObject navSystem = new GameObject("NavigationSystem");
        Undo.RegisterCreatedObjectUndo(navSystem, "Create NavigationSystem");

        // ── 3. Add LineRenderer (required by PathAnimator) ────────────────────
        Undo.AddComponent<LineRenderer>(navSystem);

        // ── 4. Add PathAnimator ───────────────────────────────────────────────
        PathAnimator pathAnimator = Undo.AddComponent<PathAnimator>(navSystem);

        // ── 5. Add EnvironmentManager ─────────────────────────────────────────
        Undo.AddComponent<EnvironmentManager>(navSystem);

        // ── 6. Wire PathAnimator into CampusManager ───────────────────────────
        CampusManager campusManager = Object.FindFirstObjectByType<CampusManager>();
        if (campusManager != null)
        {
            Undo.RecordObject(campusManager, "Wire PathAnimator");
            campusManager.pathAnimator = pathAnimator;
            EditorUtility.SetDirty(campusManager);
            Debug.Log("[NavigationSetup] Wired PathAnimator into CampusManager.");
        }
        else
        {
            Debug.LogWarning("[NavigationSetup] CampusManager not found in scene. " +
                             "Run 'Tools > Campus > Generate UI' first, then re-run Setup.");
        }

        // ── 7. Select the new object so user can see it ───────────────────────
        Selection.activeGameObject = navSystem;

        Debug.Log("[NavigationSetup] Done! NavigationSystem created with PathAnimator + EnvironmentManager.");
    }
}

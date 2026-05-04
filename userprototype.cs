using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class userprototype : MonoBehaviour
{
    public Camera cam;
    public NavMeshAgent agent;

    private List<Vector3> path = new List<Vector3>();
    private LineRenderer lineRenderer;
    private float lineWidth = 0.1f;

    private string selectedDestination = "";
    private UnityEngine.UI.Image lastSelectedImage;
    private Color originalImageColor;

    void Start()
    {
        // ── DE-CONFLICT ──
        // If CampusManager is present, it handles the UI. Disable this legacy script's UI hooking.
        if (FindFirstObjectByType<CampusManager>() != null)
        {
            Debug.Log("<b>[userprototype]</b> CampusManager detected. Disabling legacy UI hooks to prevent conflicts.");
            return;
        }

        // Tighten NavMeshAgent steering to strictly follow the arrow's exact straight lines
        if (agent != null)
        {
            agent.acceleration = 9999f; // Prevent drifting/sliding on corners
            agent.angularSpeed = 9999f; // Prevent wide curving turns
            agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.NoObstacleAvoidance; // Stop swerving around dynamic objects
        }

        // --- AUTO-WIRE GUI BUTTONS & HIDE CUBES ---
        // Find all images in the scene (since UIGenerator makes panels using Image)
        UnityEngine.UI.Image[] allImages = Resources.FindObjectsOfTypeAll<UnityEngine.UI.Image>();
        foreach (var img in allImages)
        {
            // Skip prefabs, we only want scene objects
            if (img.gameObject.scene.rootCount == 0) continue;

            string objName = img.gameObject.name;

            // If this is a location button panel
            if (objName.EndsWith("_Btn"))
            {
                string locationName = objName.Replace("_Btn", "");

                // 1. Hide the corresponding destination cube
                GameObject locCube = GameObject.Find(locationName);
                if (locCube == null && locationName == "Cashier") locCube = GameObject.Find("Casier"); // Typo fallback
                
                if (locCube != null)
                {
                    MeshRenderer renderer = locCube.GetComponent<MeshRenderer>();
                    if (renderer != null) renderer.enabled = false;
                }

                // 2. Fix missing Button component from UIGenerator
                UnityEngine.UI.Button btn = img.GetComponent<UnityEngine.UI.Button>();
                if (btn == null) btn = img.gameObject.AddComponent<UnityEngine.UI.Button>();
                
                // 3. Link the click event dynamically
                btn.onClick.RemoveAllListeners(); // Prevent duplicates
                btn.onClick.AddListener(() => SelectLocation(locationName, btn));
            }
        }

        // Find the "Show Path" button
        GameObject showBtnObj = GameObject.Find("ShowPathBtn");
        if (showBtnObj != null)
        {
            UnityEngine.UI.Button btn = showBtnObj.GetComponent<UnityEngine.UI.Button>();
            if (btn == null) btn = showBtnObj.AddComponent<UnityEngine.UI.Button>();
            
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(ShowPath);
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Check if clicking on UI
            bool isPointerOverUI = UnityEngine.EventSystems.EventSystem.current != null && 
                                   UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
                                   
            if (Input.touchCount > 0 && UnityEngine.EventSystems.EventSystem.current != null)
            {
                isPointerOverUI = UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            }

            if (!isPointerOverUI)
            {
                HandleMouseClick();
            }
        }
    }

    // Step 1: Call this from the "Cashier" GUI Button
    public void SelectCashier()
    {
        SelectLocation("Cashier", null);
    }

    // Step 1 Internal: Selects a location and highlights the button
    public void SelectLocation(string locationName, UnityEngine.UI.Button clickedButton)
    {
        selectedDestination = locationName;
        Debug.Log($"<b>[userprototype]</b> Selected {locationName} as destination. Waiting for Show Path...");

        // Remove highlight from previous button
        if (lastSelectedImage != null)
        {
            lastSelectedImage.color = originalImageColor;
        }

        // Add highlight to new button
        if (clickedButton != null)
        {
            UnityEngine.UI.Image img = clickedButton.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                lastSelectedImage = img;
                originalImageColor = img.color;
                
                // Highlight color: Semi-transparent Gold/Yellow to match the theme, or Green
                img.color = new Color(1f, 0.72f, 0f, 0.6f); 
            }
        }
    }

    // Step 2: Call this from the "Show Path" GUI Button
    public void ShowPath()
    {
        if (string.IsNullOrEmpty(selectedDestination))
        {
            Debug.LogWarning("<b>[userprototype]</b> No destination selected! Please select a location first.");
            return;
        }
        
        GoToLocation(selectedDestination);
    }

    // Generic method to navigate to any named GameObject
    public void GoToLocation(string locationName)
    {
        GameObject loc = GameObject.Find(locationName);
        
        // Fallback for typo mentioned by user
        if (loc == null && locationName == "Cashier") 
        {
            loc = GameObject.Find("Casier");
        }

        if (loc != null)
        {
            Vector3 targetPos = loc.transform.position;
            if (PathAnimator.Instance != null)
            {
                Debug.Log($"<b>[userprototype]</b> UI Requesting path to {locationName} at {targetPos}");
                PathAnimator.Instance.TrackAgent(agent, targetPos);
            }
            else
            {
                agent.SetDestination(targetPos);
            }
        }
        else
        {
            Debug.LogWarning($"<b>[userprototype]</b> Could not find an object named '{locationName}' in the scene.");
        }
    }

    void HandleMouseClick()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // ── AESTHETIC FIX: Use the Luxury Navigation System ──
            if (PathAnimator.Instance != null)
            {
                Debug.Log($"<b>[userprototype]</b> Requesting path to {hit.point}");
                PathAnimator.Instance.TrackAgent(agent, hit.point);
            }
            else
            {
                agent.SetDestination(hit.point); // Fallback if no animator
            }
        }
    }
}
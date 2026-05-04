using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class CampusManager : MonoBehaviour
{
    private Camera mainCamera;

    [Header("Camera Settings")]
    public float orthoZoomSpeed = 10f;
    public float minOrthoSize   = 10f;
    public float maxOrthoSize   = 100f;

    [Header("Exploration (Free Fly) Settings")]
    public float moveSpeed     = 20f;
    public float fastMoveSpeed = 50f;
    public float lookSpeed     = 2f;

    [Header("Navigation")]
    [Tooltip("Reference to the PathAnimator in the scene.")]
    public PathAnimator pathAnimator;

    private bool  isExplorationMode = false;
    private float yaw   = 0f;
    private float pitch = 0f;

    private Vector3 dragOrigin;
    private bool isDragging = false;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("CampusManager: Main Camera not found!");
            return;
        }

        // Auto-bind UI buttons if they exist in the hierarchy under this Canvas
        UnityEngine.UI.Button perspectiveBtn = transform.Find("CenterOverlay/PerspectiveToggle")?.GetComponent<UnityEngine.UI.Button>();
        if (perspectiveBtn != null) perspectiveBtn.onClick.AddListener(TogglePerspective);

        UnityEngine.UI.Button zoomInBtn = transform.Find("CenterOverlay/ZoomIn")?.GetComponent<UnityEngine.UI.Button>();
        if (zoomInBtn != null) zoomInBtn.onClick.AddListener(ZoomIn);

        UnityEngine.UI.Button zoomOutBtn = transform.Find("CenterOverlay/ZoomOut")?.GetComponent<UnityEngine.UI.Button>();
        if (zoomOutBtn != null) zoomOutBtn.onClick.AddListener(ZoomOut);

        // Initialize camera to Orthographic looking down
        mainCamera.orthographic = true;
        mainCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        mainCamera.orthographicSize = maxOrthoSize * 0.8f; // Start zoomed out like Google Maps
        yaw = mainCamera.transform.eulerAngles.y;
        pitch = mainCamera.transform.eulerAngles.x;

        AutoHookExistingUI();
    }

    void Update()
    {
        if (mainCamera == null) return;

        if (isExplorationMode)
        {
            HandleFreeFlyCamera();
        }
        else
        {
            HandleOrthoCamera();
        }
    }

    private void HandleOrthoCamera()
    {
        // Pinch to zoom and touch drag
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float currentMagnitude = (touchZero.position - touchOne.position).magnitude;

            float difference = currentMagnitude - prevMagnitude;

            ZoomOrthoCamera(difference * 0.05f * orthoZoomSpeed, (touchZero.position + touchOne.position) / 2f);
            isDragging = false; // Cancel drag if pinching
        }
        else
        {
            // Mouse Scroll Zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0.0f && !EventSystem.current.IsPointerOverGameObject())
            {
                ZoomOrthoCamera(scroll * orthoZoomSpeed, Input.mousePosition);
            }

            // Pan Camera (Left click or Touch drag)
            if (Input.GetMouseButtonDown(0))
            {
                bool isPointerOverUI = EventSystem.current.IsPointerOverGameObject();
                if (Input.touchCount > 0)
                {
                    isPointerOverUI = EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
                }

                if (!isPointerOverUI)
                {
                    Vector3 screenPos = Input.mousePosition;
                    screenPos.z = mainCamera.nearClipPlane;
                    dragOrigin = mainCamera.ScreenToWorldPoint(screenPos);
                    isDragging = true;
                }
            }

            if (Input.GetMouseButton(0) && isDragging)
            {
                Vector3 screenPos = Input.mousePosition;
                screenPos.z = mainCamera.nearClipPlane;
                Vector3 currentPos = mainCamera.ScreenToWorldPoint(screenPos);
                Vector3 difference = dragOrigin - currentPos;
                mainCamera.transform.position += difference;
            }

            if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }
        }
    }

    private void ZoomOrthoCamera(float increment, Vector2 screenPos)
    {
        if (increment == 0) return;

        // Use nearClipPlane to ensure Z is valid for ScreenToWorldPoint
        Vector3 screenPosWithZ = new Vector3(screenPos.x, screenPos.y, mainCamera.nearClipPlane);

        Vector3 posBeforeZoom = mainCamera.ScreenToWorldPoint(screenPosWithZ);
        mainCamera.orthographicSize -= increment;
        mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize, minOrthoSize, maxOrthoSize);
        Vector3 posAfterZoom = mainCamera.ScreenToWorldPoint(screenPosWithZ);

        Vector3 diff = posBeforeZoom - posAfterZoom;
        mainCamera.transform.position += diff;
    }

    private void HandleFreeFlyCamera()
    {
        // Right click to look around
        if (Input.GetMouseButton(1))
        {
            yaw += lookSpeed * Input.GetAxis("Mouse X");
            pitch -= lookSpeed * Input.GetAxis("Mouse Y");
            pitch = Mathf.Clamp(pitch, -90f, 90f);
            
            mainCamera.transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);
        }

        // WASD movement
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? fastMoveSpeed : moveSpeed;
        Vector3 moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        
        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;
        
        mainCamera.transform.position += (forward * moveDirection.z + right * moveDirection.x) * currentSpeed * Time.deltaTime;
        
        // Up/Down with Q/E
        if (Input.GetKey(KeyCode.E))
            mainCamera.transform.position += Vector3.up * currentSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Q))
            mainCamera.transform.position -= Vector3.up * currentSpeed * Time.deltaTime;
    }

    // UI Hook: Toggle Perspective
    public void TogglePerspective()
    {
        if (mainCamera == null) return;

        isExplorationMode = !isExplorationMode;

        if (isExplorationMode)
        {
            // Switch to Perspective (free-fly exploration)
            mainCamera.orthographic = false;

            // Pitch down to 45 degrees if coming straight down
            if (Mathf.Approximately(mainCamera.transform.eulerAngles.x, 90f)
                || mainCamera.transform.eulerAngles.x > 80f)
            {
                pitch = 45f;
                yaw   = mainCamera.transform.eulerAngles.y;
                mainCamera.transform.eulerAngles = new Vector3(pitch, yaw, 0f);
            }

            // Hide navigation path in exploration mode — it's irrelevant when flying free
            pathAnimator?.ClearPath();
        }
        else
        {
            // Switch back to Orthographic Birds-Eye view
            mainCamera.orthographic = true;
            pitch = 90f;
            mainCamera.transform.eulerAngles = new Vector3(pitch, yaw, 0f);
        }
    }

    // UI Hook: Zoom In Button
    public void ZoomIn()
    {
        if (mainCamera != null && mainCamera.orthographic)
        {
            ZoomOrthoCamera(5f, new Vector2(Screen.width / 2f, Screen.height / 2f));
        }
    }

    // UI Hook: Zoom Out Button
    public void ZoomOut()
    {
        if (mainCamera != null && mainCamera.orthographic)
        {
            ZoomOrthoCamera(-5f, new Vector2(Screen.width / 2f, Screen.height / 2f));
        }
    }

    // ── Navigation Flow & Existing GUI Hook ───────────────────────────────────

    private Vector3 currentStartPos;
    private Vector3 currentDestPos;
    private bool isStartLocationSet = false;
    private GameObject reachedConfirmationPanel;
    
    private UnityEngine.UI.Image lastSelectedImage;
    private Color originalImageColor;

    private void AutoHookExistingUI()
    {
        // --- ROBUST UI DISCOVERY (Adopted from userprototype) ---
        UnityEngine.UI.Image[] allImages = Resources.FindObjectsOfTypeAll<UnityEngine.UI.Image>();
        foreach (var img in allImages)
        {
            // Skip prefabs
            if (img.gameObject.scene.rootCount == 0) continue;

            string objName = img.gameObject.name;

            // If this is a location button panel
            if (objName.EndsWith("_Btn"))
            {
                string locationName = objName.Replace("_Btn", "");

                // 1. Link the click event dynamically
                UnityEngine.UI.Button btn = img.GetComponent<UnityEngine.UI.Button>();
                if (btn == null) btn = img.gameObject.AddComponent<UnityEngine.UI.Button>();
                
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnLocationSelected(locationName, img));
                
                // 2. Hide the corresponding destination cube if requested (optional aesthetic)
                GameObject locCube = GameObject.Find(locationName);
                if (locCube == null && locationName == "Cashier") locCube = GameObject.Find("Casier"); 
                if (locCube != null)
                {
                    MeshRenderer renderer = locCube.GetComponent<MeshRenderer>();
                    if (renderer != null) renderer.enabled = false;
                }
            }
        }

        // Show Path Button
        UnityEngine.UI.Button showPathBtn = null;
        GameObject spObj = GameObject.Find("ShowPathBtn");
        if (spObj != null) showPathBtn = spObj.GetComponent<UnityEngine.UI.Button>();
        
        if (showPathBtn != null)
        {
            showPathBtn.onClick.RemoveAllListeners();
            showPathBtn.onClick.AddListener(ShowPath);

            // Create a proper Confirmation Popup for "Reached Destination"
            if (reachedConfirmationPanel == null)
            {
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    reachedConfirmationPanel = new GameObject("ReachedConfirmationPanel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                    reachedConfirmationPanel.transform.SetParent(canvas.transform, false);
                    
                    RectTransform rt = reachedConfirmationPanel.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(1f, 0f); // Bottom Right
                    rt.anchorMax = new Vector2(1f, 0f);
                    rt.pivot = new Vector2(1f, 0f);
                    rt.anchoredPosition = new Vector2(-40f, 40f); // Margin from corner
                    rt.sizeDelta = new Vector2(400f, 150f);
                    
                    reachedConfirmationPanel.GetComponent<UnityEngine.UI.Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

                    // Text: "Have you reached your destination?"
                    GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    textGo.transform.SetParent(rt, false);
                    RectTransform textRt = textGo.GetComponent<RectTransform>();
                    textRt.anchorMin = new Vector2(0, 0.6f); textRt.anchorMax = new Vector2(1, 1);
                    textRt.anchoredPosition = Vector2.zero;
                    textRt.sizeDelta = Vector2.zero;
                    
                    TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
                    tmp.text = "Arrived at destination?";
                    tmp.fontSize = 20;
                    tmp.alignment = TextAlignmentOptions.Center;

                    // "Yes" Button
                    GameObject yesBtnGo = Instantiate(spObj, rt);
                    yesBtnGo.name = "YesButton";
                    RectTransform yesRt = yesBtnGo.GetComponent<RectTransform>();
                    yesRt.anchorMin = new Vector2(0.5f, 0); yesRt.anchorMax = new Vector2(0.5f, 0);
                    yesRt.pivot = new Vector2(0.5f, 0);
                    yesRt.anchoredPosition = new Vector2(0, 15f);
                    yesRt.sizeDelta = new Vector2(180f, 45f);

                    yesBtnGo.GetComponentInChildren<TextMeshProUGUI>().text = "YES";
                    yesBtnGo.GetComponent<UnityEngine.UI.Button>().onClick.RemoveAllListeners();
                    yesBtnGo.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(ReachedDestination);

                    reachedConfirmationPanel.SetActive(false); // Hide until path is showing
                }
            }
        }

        // Destination SearchBar hook
        GameObject sbObj = GameObject.Find("SearchBar");
        if (sbObj != null)
        {
            TMPro.TMP_InputField searchBar = sbObj.GetComponent<TMPro.TMP_InputField>();
            if (searchBar != null)
            {
                searchBar.onEndEdit.RemoveAllListeners();
                searchBar.onEndEdit.AddListener(OnSearchSubmitted);
            }
        }

        // --- SPAWN MISSING LOCATION BUTTONS ---
        GameObject cashierBtnGo = GameObject.Find("Cashier_Btn");
        if (cashierBtnGo != null)
        {
            // Names as they appear on the buttons
            string[] neededLocations = { "Dome", "Daragang hall", "Daragang Magayon Hall" };
            foreach (string loc in neededLocations)
            {
                // Check if button already exists
                if (GameObject.Find(loc + "_Btn") == null)
                {
                    GameObject newBtnGo = Instantiate(cashierBtnGo, cashierBtnGo.transform.parent);
                    newBtnGo.name = loc + "_Btn";
                    
                    // Set the correct text
                    TMPro.TextMeshProUGUI tmp = newBtnGo.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (tmp != null) tmp.text = loc;

                    UnityEngine.UI.Button btn = newBtnGo.GetComponent<UnityEngine.UI.Button>();
                    btn.onClick.RemoveAllListeners();
                    UnityEngine.UI.Image img = newBtnGo.GetComponent<UnityEngine.UI.Image>();
                    string capturedLoc = loc;
                    btn.onClick.AddListener(() => OnLocationSelected(capturedLoc, img));
                    
                    // Offset it so they don't overlap (find last button's Y)
                    // This is crude but works for this UI layout
                    RectTransform rt = newBtnGo.GetComponent<RectTransform>();
                    rt.anchoredPosition += new Vector2(0, -70f * (System.Array.IndexOf(neededLocations, loc) + 4)); 
                }
            }
        }
    }

    public void OnLocationSelected(string locationName, UnityEngine.UI.Image clickedImg = null)
    {
        GameObject locGo = GameObject.Find(locationName);
        if (locGo == null)
        {
            // Try robust fallback: check if any object name is a substring of locationName (e.g. "CEAFA Building" in "CEAFA Building(Location)")
            // or if locationName is a substring of the object name.
            string cleanName = locationName.Replace("(Location)", "").Trim();
            
            foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name == cleanName || go.name.Contains(cleanName) || cleanName.Contains(go.name))
                {
                    if (go.name.EndsWith("_Btn")) continue; // Skip UI buttons
                    locGo = go;
                    break;
                }
            }

            if (locGo == null)
            {
                Debug.LogWarning($"[CampusManager] Location '{locationName}' not found in scene!");
                return;
            }
        }

        Vector3 pos = locGo.transform.position;
        // Snap to valid NavMesh so user isn't stuck inside the building obstacle
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(pos, out hit, 100f, UnityEngine.AI.NavMesh.AllAreas))
        {
            pos = hit.position;
        }

        currentDestPos = pos;
        Debug.Log($"[CampusManager] Destination set to {locGo.name}. Click Show Path.");

        // --- HIGHLIGHTING ---
        if (lastSelectedImage != null) lastSelectedImage.color = originalImageColor;

        if (clickedImg != null)
        {
            lastSelectedImage = clickedImg;
            originalImageColor = clickedImg.color;
            clickedImg.color = new Color(1f, 0.72f, 0f, 0.6f); // Highlight Gold
        }
    }

    public void SetExplicitStartLocation(Vector3 pos)
    {
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(pos, out hit, 100f, UnityEngine.AI.NavMesh.AllAreas))
        {
            pos = hit.position;
        }

        currentStartPos = pos;
        isStartLocationSet = true;
        
        TeleportTo(currentStartPos);
        TeleportCharacter(currentStartPos);
        Debug.Log("[CampusManager] Start location confirmed! Now use your SearchBar or Buttons to select a Destination.");
    }

    // UI Hook: Search Bar (Called On End Edit)
    public void OnSearchSubmitted(string query)
    {
        if (string.IsNullOrEmpty(query)) return;
        Debug.Log($"<b>[CampusManager]</b> Search submitted: '{query}'");
        
        // Assume search bar is used for picking destination (or start if not set)
        OnLocationSelected(query);
    }

    // UI Hook: Show Path Button
    public void ShowPath()
    {
        // 1. Ensure PathAnimator is assigned
        if (pathAnimator == null) pathAnimator = PathAnimator.Instance;
        if (pathAnimator == null) pathAnimator = FindFirstObjectByType<PathAnimator>();

        if (pathAnimator == null)
        {
            Debug.LogError("[CampusManager] PathAnimator not found in scene! Make sure it's present.");
            return;
        }

        if (!isStartLocationSet)
        {
            Debug.LogWarning("[CampusManager] Please select a start location first.");
            return;
        }

        if (currentDestPos == Vector3.zero)
        {
            Debug.LogWarning("[CampusManager] No destination selected. Click a location button or search.");
            return;
        }

        Debug.Log($"[CampusManager] Showing path from {currentStartPos} to {currentDestPos}");
        
        // ── CAMERA: Switch to Orthographic to frame the path ──
        if (!mainCamera.orthographic)
        {
            TogglePerspective(); // Switch back to Birds-Eye Ortho
        }

        UnityEngine.AI.NavMeshAgent agent = FindFirstObjectByType<UnityEngine.AI.NavMeshAgent>();
        pathAnimator.SetDestination(currentStartPos, currentDestPos, agent);
        FramePoints(currentStartPos, currentDestPos);
        
        if (reachedConfirmationPanel != null)
        {
            reachedConfirmationPanel.SetActive(true);
            reachedConfirmationPanel.transform.SetAsLastSibling();
        }
    }

    // UI Hook: Reached Destination Button (Confirm Yes)
    public void ReachedDestination()
    {
        if (pathAnimator != null) pathAnimator.ClearPath();
        
        TeleportTo(currentDestPos);
        TeleportCharacter(currentDestPos);
        
        isStartLocationSet = false;
        if (reachedConfirmationPanel != null) reachedConfirmationPanel.SetActive(false);
        Debug.Log("[CampusManager] Reached destination. Teleported user.");
        
        // Bring back the Where Are You panel
        if (NavigationFlowManager.Instance != null)
        {
            NavigationFlowManager.Instance.ShowPanel(true);
        }
    }

    private void TeleportCharacter(Vector3 position)
    {
        UnityEngine.AI.NavMeshAgent agent = FindFirstObjectByType<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.Warp(position);
            // Do NOT call SetDestination here — that would make the agent walk
            return;
        }

        GameObject player = GameObject.Find("user");
        if (player == null) player = GameObject.Find("User");
        if (player == null) player = GameObject.Find("Player");
        if (player == null)
        {
            try { player = GameObject.FindGameObjectWithTag("Player"); } catch {}
        }

        if (player != null)
        {
            // If they have a Rigidbody, we must reset velocity so they don't carry momentum through the teleport
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = position;
            }
            player.transform.position = position;
        }
    }

    // ── Navigation Flow Camera Controls ───────────────────────────────────────

    private Coroutine cameraPanCoroutine;

    /// <summary>
    /// Instantly teleports the camera to look at the target position.
    /// Used when the user selects their starting location.
    /// </summary>
    public void TeleportTo(Vector3 position)
    {
        if (cameraPanCoroutine != null)
        {
            StopCoroutine(cameraPanCoroutine);
            cameraPanCoroutine = null;
        }

        if (mainCamera == null) return;
        
        Vector3 newPos = position;
        
        // If we are in Top-Down Orthographic mode, keep the original height but center X and Z
        if (mainCamera.orthographic)
        {
            newPos.y = mainCamera.transform.position.y;
            mainCamera.transform.position = newPos;
            mainCamera.orthographicSize = 25f; // Zoom in a bit
        }
        else
        {
            // If in perspective, just move near the target and look at it
            newPos += new Vector3(0, 15f, -15f);
            mainCamera.transform.position = newPos;
            mainCamera.transform.LookAt(position);
        }
    }

    /// <summary>
    /// Smoothly animates the camera to frame both the start and end points on the screen.
    /// </summary>
    public void FramePoints(Vector3 p1, Vector3 p2)
    {
        if (mainCamera == null || !mainCamera.orthographic) return;

        if (cameraPanCoroutine != null)
            StopCoroutine(cameraPanCoroutine);

        cameraPanCoroutine = StartCoroutine(FramePointsRoutine(p1, p2));
    }

    private System.Collections.IEnumerator FramePointsRoutine(Vector3 p1, Vector3 p2)
    {
        // Calculate center
        Vector3 center = (p1 + p2) / 2f;
        center.y = mainCamera.transform.position.y;

        // Calculate required orthographic size to fit both points with padding
        // Accounting for Aspect Ratio (orthoSize is vertical, so we check both)
        float dx = Mathf.Abs(p1.x - p2.x);
        float dz = Mathf.Abs(p1.z - p2.z);
        
        float aspect = mainCamera.aspect;
        float requiredSizeByHeight = dz * 0.5f;
        float requiredSizeByWidth  = (dx * 0.5f) / aspect;
        
        float targetSize = Mathf.Max(requiredSizeByHeight, requiredSizeByWidth) + 10f; // Add padding
        targetSize = Mathf.Clamp(targetSize, minOrthoSize, maxOrthoSize);

        Vector3 startPos = mainCamera.transform.position;
        float startSize = mainCamera.orthographicSize;

        float duration = 1.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            mainCamera.transform.position = Vector3.Lerp(startPos, center, t);
            mainCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);

            yield return null;
        }

        mainCamera.transform.position = center;
        mainCamera.orthographicSize = targetSize;
    }
}

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class UIGenerator : EditorWindow
{
    [MenuItem("Tools/Campus/Generate UI")]
    public static void GenerateUI()
    {
        // 1. Create Canvas
        GameObject canvasObj = new GameObject("CampusCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // 2. Create EventSystem if not exists
        EventSystem existingES = GameObject.FindFirstObjectByType<EventSystem>();
        if (existingES == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
        }

        // Add CampusManager to Canvas
        CampusManager manager = canvasObj.AddComponent<CampusManager>();

        // Load Textures
        Texture2D searchTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/UI/search_icon.png");
        Texture2D locationTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/UI/location_pin.png");
        Texture2D perspectiveTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/UI/perspective_toggle.png");

        Sprite searchSprite = searchTex ? Sprite.Create(searchTex, new Rect(0,0,searchTex.width,searchTex.height), new Vector2(0.5f, 0.5f)) : null;
        Sprite locationSprite = locationTex ? Sprite.Create(locationTex, new Rect(0,0,locationTex.width,locationTex.height), new Vector2(0.5f, 0.5f)) : null;
        Sprite perspectiveSprite = perspectiveTex ? Sprite.Create(perspectiveTex, new Rect(0,0,perspectiveTex.width,perspectiveTex.height), new Vector2(0.5f, 0.5f)) : null;

        // 3. Top Header
        ColorUtility.TryParseHtmlString("#FFB800", out Color headerColor);
        GameObject header = CreatePanel(canvasObj.transform, "TopHeader", headerColor);
        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(0.5f, 1);
        headerRect.sizeDelta = new Vector2(0, 80);
        headerRect.anchoredPosition = Vector2.zero;

        GameObject title = CreateText(header.transform, "Title", "THOMAPSIAN", 36, true);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.sizeDelta = Vector2.zero;
        titleRect.anchoredPosition = Vector2.zero;
        
        TextMeshProUGUI titleText = title.GetComponent<TextMeshProUGUI>();
        titleText.enableWordWrapping = false;
        titleText.overflowMode = TextOverflowModes.Overflow;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.characterSpacing = 5f; 
        titleText.color = Color.white;
        titleText.margin = Vector4.zero;
        titleText.characterSpacing = 5f; // Add some premium letter spacing

        // 4. Left Sidebar
        GameObject leftSidebar = CreatePanel(canvasObj.transform, "LeftSidebar", new Color(0, 0, 0, 0.8f));
        RectTransform leftRect = leftSidebar.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 0);
        leftRect.anchorMax = new Vector2(0, 1);
        leftRect.pivot = new Vector2(0, 0.5f);
        leftRect.sizeDelta = new Vector2(350, -80); // width 350, height matches canvas minus header
        leftRect.anchoredPosition = new Vector2(0, -40); // Shift down half header height

        // Search Bar Placeholder
        GameObject searchBg = CreatePanel(leftSidebar.transform, "SearchBar", new Color(1, 1, 1, 0.2f));
        RectTransform searchRect = searchBg.GetComponent<RectTransform>();
        searchRect.anchorMin = new Vector2(0, 1);
        searchRect.anchorMax = new Vector2(1, 1);
        searchRect.pivot = new Vector2(0.5f, 1);
        searchRect.sizeDelta = new Vector2(-40, 50); // width minus padding
        searchRect.anchoredPosition = new Vector2(0, -20);
        
        GameObject searchText = CreateText(searchBg.transform, "Placeholder", "Search Locations...", 24, false);
        searchText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
        RectTransform searchTextRect = searchText.GetComponent<RectTransform>();
        searchTextRect.anchorMin = new Vector2(0, 0);
        searchTextRect.anchorMax = new Vector2(1, 1);
        searchTextRect.offsetMin = new Vector2(50, 0);
        searchTextRect.offsetMax = new Vector2(0, 0);

        if (searchSprite)
        {
            GameObject searchIcon = CreateImage(searchBg.transform, "Icon", searchSprite);
            RectTransform iconRect = searchIcon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.5f);
            iconRect.anchorMax = new Vector2(0, 0.5f);
            iconRect.pivot = new Vector2(0, 0.5f);
            iconRect.sizeDelta = new Vector2(30, 30);
            iconRect.anchoredPosition = new Vector2(10, 0);
        }

        // Navigation Buttons (Dummy)
        CreateNavButton(leftSidebar.transform, "T-Lobby", -100);
        CreateNavButton(leftSidebar.transform, "Cashier", -170);
        CreateNavButton(leftSidebar.transform, "Registrar", -240);
        CreateNavButton(leftSidebar.transform, "CEAAF Building", -310);

        // Action Button
        GameObject pathBtnObj = CreateButton(leftSidebar.transform, "ShowPathBtn", null);
        ColorUtility.TryParseHtmlString("#FFB800", out Color btnColor);
        pathBtnObj.GetComponent<Image>().color = btnColor;
        pathBtnObj.GetComponentInChildren<TextMeshProUGUI>().text = "Show Path";
        pathBtnObj.GetComponentInChildren<TextMeshProUGUI>().color = Color.black;
        RectTransform pathBtnRect = pathBtnObj.GetComponent<RectTransform>();
        pathBtnRect.anchorMin = new Vector2(0.5f, 0);
        pathBtnRect.anchorMax = new Vector2(0.5f, 0);
        pathBtnRect.pivot = new Vector2(0.5f, 0);
        pathBtnRect.sizeDelta = new Vector2(310, 50);
        pathBtnRect.anchoredPosition = new Vector2(0, 20);

        // 5. Right Sidebar
        GameObject rightSidebar = CreatePanel(canvasObj.transform, "RightSidebar", new Color(0, 0, 0, 0.8f));
        RectTransform rightRect = rightSidebar.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(1, 0);
        rightRect.anchorMax = new Vector2(1, 1);
        rightRect.pivot = new Vector2(1, 0.5f);
        rightRect.sizeDelta = new Vector2(300, -80);
        rightRect.anchoredPosition = new Vector2(0, -40);

        GameObject infoTitle = CreateText(rightSidebar.transform, "InfoTitle", "Location Information", 24, true);
        RectTransform infoTitleRect = infoTitle.GetComponent<RectTransform>();
        infoTitleRect.anchorMin = new Vector2(0.5f, 1);
        infoTitleRect.anchorMax = new Vector2(0.5f, 1);
        infoTitleRect.anchoredPosition = new Vector2(0, -40);

        GameObject infoText = CreateText(rightSidebar.transform, "InfoContent", "Selected: None", 20, false);
        infoText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;
        RectTransform infoContentRect = infoText.GetComponent<RectTransform>();
        infoContentRect.anchorMin = new Vector2(0, 0);
        infoContentRect.anchorMax = new Vector2(1, 1);
        infoContentRect.offsetMin = new Vector2(20, 20);
        infoContentRect.offsetMax = new Vector2(-20, -100);

        // Location Pin Dummy
        if (locationSprite)
        {
            GameObject pinObj = CreateImage(rightSidebar.transform, "LocationPin", locationSprite);
            RectTransform pinRect = pinObj.GetComponent<RectTransform>();
            pinRect.anchorMin = new Vector2(0.5f, 0.5f);
            pinRect.anchorMax = new Vector2(0.5f, 0.5f);
            pinRect.sizeDelta = new Vector2(100, 100);
            pinRect.anchoredPosition = new Vector2(0, 50);
        }

        // 6. Center Overlay (Perspective & Zoom)
        GameObject centerOverlay = new GameObject("CenterOverlay");
        centerOverlay.transform.SetParent(canvasObj.transform, false);
        RectTransform centerRect = centerOverlay.AddComponent<RectTransform>();
        centerRect.anchorMin = new Vector2(0.5f, 0);
        centerRect.anchorMax = new Vector2(0.5f, 0);
        centerRect.pivot = new Vector2(0.5f, 0);
        centerRect.sizeDelta = new Vector2(300, 100);
        centerRect.anchoredPosition = new Vector2(0, 50);

        // Perspective Button
        GameObject perspectiveBtn = CreateButton(centerOverlay.transform, "PerspectiveToggle", perspectiveSprite);
        perspectiveBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
        perspectiveBtn.GetComponent<Image>().color = new Color(0,0,0,0.8f);
        if (perspectiveSprite == null) 
        {
            perspectiveBtn.GetComponentInChildren<TextMeshProUGUI>().text = "3D";
            perspectiveBtn.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
        }

        // Zoom Buttons
        GameObject zoomInBtn = CreateButton(centerOverlay.transform, "ZoomIn", null);
        zoomInBtn.GetComponent<Image>().color = new Color(0,0,0,0.8f);
        zoomInBtn.GetComponentInChildren<TextMeshProUGUI>().text = "+";
        zoomInBtn.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
        zoomInBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-80, 0);

        GameObject zoomOutBtn = CreateButton(centerOverlay.transform, "ZoomOut", null);
        zoomOutBtn.GetComponent<Image>().color = new Color(0,0,0,0.8f);
        zoomOutBtn.GetComponentInChildren<TextMeshProUGUI>().text = "-";
        zoomOutBtn.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
        zoomOutBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(80, 0);

        // Set undo
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create UI Canvas");
        
        Debug.Log("UI Successfully Generated! Press Play to test.");
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    private static GameObject CreateText(Transform parent, string name, string text, int fontSize, bool isBold)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = isBold ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return go;
    }

    private static GameObject CreateImage(Transform parent, string name, Sprite sprite)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        if (sprite) img.sprite = sprite;
        return go;
    }

    private static GameObject CreateButton(Transform parent, string name, Sprite sprite)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        if (sprite) img.sprite = sprite;
        go.AddComponent<Button>();
        
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(60, 60);

        if (sprite == null)
        {
            CreateText(go.transform, "Text", "Btn", 36, true);
        }

        return go;
    }

    private static void CreateNavButton(Transform parent, string text, float yPos)
    {
        GameObject bg = CreatePanel(parent, text + "_Btn", new Color(1,1,1,0.1f));
        RectTransform rect = bg.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.sizeDelta = new Vector2(-40, 50);
        rect.anchoredPosition = new Vector2(0, yPos);

        GameObject txt = CreateText(bg.transform, "Text", text, 20, false);
        txt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
        RectTransform tRect = txt.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0,0);
        tRect.anchorMax = new Vector2(1,1);
        tRect.offsetMin = new Vector2(20,0);
        tRect.offsetMax = new Vector2(0,0);
    }
}

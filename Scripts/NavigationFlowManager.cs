using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NavigationFlowManager : MonoBehaviour
{
    public static NavigationFlowManager Instance { get; private set; }

    [Header("Configuration")]
    public List<string> locationNames = new List<string> {
        "CEAFA Building(Location)",
        "Dome",
        "Daragang Magayon Hall",
        "Cashier",
        "Registrar",
        "T-Lobby"
    };

    private Dictionary<string, Vector3> availableLocations = new Dictionary<string, Vector3>();
    private List<string> dropdownOptions = new List<string>();
    private int selectedLocationIndex = 0;

    // UI refs
    private GameObject panelGo;
    private TextMeshProUGUI dropdownText;
    private GameObject dropdownListPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        DiscoverLocations();
        CreateUI();
        ShowPanel(true);
    }

    private void DiscoverLocations()
    {
        foreach (string locName in locationNames)
        {
            GameObject go = GameObject.Find(locName);
            if (go == null) go = GameObject.Find(locName.Replace("(Location)", "")); // try without suffix
            
            if (go != null)
            {
                availableLocations[locName] = go.transform.position;
                dropdownOptions.Add(locName);
            }
        }
        
        if (dropdownOptions.Count == 0) dropdownOptions.Add("No Locations Found");
    }

    private void CreateUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        panelGo = new GameObject("StartLocationPanel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(canvas.transform, false);
        RectTransform panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(600f, 250f);
        panelGo.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // Title
        GameObject titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(panelRt, false);
        RectTransform titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f); titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0, -20f);
        titleRt.sizeDelta = new Vector2(-20f, 50f);
        TextMeshProUGUI titleText = titleGo.GetComponent<TextMeshProUGUI>();
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 32;
        titleText.text = "Where are you right now?";

        // Main Dropdown Button
        GameObject dropBtnGo = new GameObject("DropdownBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        dropBtnGo.transform.SetParent(panelRt, false);
        RectTransform dropBtnRt = dropBtnGo.GetComponent<RectTransform>();
        dropBtnRt.anchorMin = new Vector2(0.5f, 0.5f); dropBtnRt.anchorMax = new Vector2(0.5f, 0.5f);
        dropBtnRt.anchoredPosition = new Vector2(0, 10f);
        dropBtnRt.sizeDelta = new Vector2(500f, 60f);
        dropBtnGo.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
        Button mainDropdownBtn = dropBtnGo.GetComponent<Button>();
        mainDropdownBtn.onClick.AddListener(ToggleDropdownList);

        GameObject dropTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        dropTextGo.transform.SetParent(dropBtnRt, false);
        RectTransform dropTextRt = dropTextGo.GetComponent<RectTransform>();
        dropTextRt.anchorMin = Vector2.zero; dropTextRt.anchorMax = Vector2.one;
        dropTextRt.sizeDelta = Vector2.zero;
        dropdownText = dropTextGo.GetComponent<TextMeshProUGUI>();
        dropdownText.alignment = TextAlignmentOptions.Center;
        dropdownText.fontSize = 26;
        dropdownText.text = dropdownOptions.Count > 0 ? dropdownOptions[0] : "No locations";

        // Action Button
        GameObject actBtnGo = new GameObject("ActionBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        actBtnGo.transform.SetParent(panelRt, false);
        RectTransform actBtnRt = actBtnGo.GetComponent<RectTransform>();
        actBtnRt.anchorMin = new Vector2(0.5f, 0f); actBtnRt.anchorMax = new Vector2(0.5f, 0f);
        actBtnRt.pivot = new Vector2(0.5f, 0f);
        actBtnRt.anchoredPosition = new Vector2(0, 20f);
        actBtnRt.sizeDelta = new Vector2(400f, 60f);
        actBtnGo.GetComponent<Image>().color = new Color(0.1f, 0.6f, 0.1f, 1f);
        Button actionBtn = actBtnGo.GetComponent<Button>();
        actionBtn.onClick.AddListener(OnConfirmStartLocation);

        GameObject actTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        actTextGo.transform.SetParent(actBtnRt, false);
        RectTransform actTextRt = actTextGo.GetComponent<RectTransform>();
        actTextRt.anchorMin = Vector2.zero; actTextRt.anchorMax = Vector2.one;
        actTextRt.sizeDelta = Vector2.zero;
        TextMeshProUGUI actionBtnText = actTextGo.GetComponent<TextMeshProUGUI>();
        actionBtnText.alignment = TextAlignmentOptions.Center;
        actionBtnText.fontSize = 26;
        actionBtnText.text = "Confirm Start Location";
        actionBtnText.color = Color.white;

        // Dropdown List Panel
        dropdownListPanel = new GameObject("DropdownList", typeof(RectTransform), typeof(Image));
        dropdownListPanel.transform.SetParent(panelRt, false);
        RectTransform listRt = dropdownListPanel.GetComponent<RectTransform>();
        listRt.anchorMin = new Vector2(0.5f, 0f); listRt.anchorMax = new Vector2(0.5f, 0f);
        listRt.pivot = new Vector2(0.5f, 1f);
        listRt.anchoredPosition = new Vector2(0, -10f); // just below panel
        float listHeight = Mathf.Min(dropdownOptions.Count * 60f, 400f);
        listRt.sizeDelta = new Vector2(500f, listHeight);
        dropdownListPanel.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);
        dropdownListPanel.SetActive(false);

        // Use a ScrollRect setup manually is tedious, so we just clip it or let it run
        for (int i = 0; i < dropdownOptions.Count; i++)
        {
            int index = i;
            GameObject itemBtnGo = new GameObject("Item", typeof(RectTransform), typeof(Image), typeof(Button));
            itemBtnGo.transform.SetParent(listRt, false);
            RectTransform itemRt = itemBtnGo.GetComponent<RectTransform>();
            itemRt.anchorMin = new Vector2(0f, 1f); itemRt.anchorMax = new Vector2(1f, 1f);
            itemRt.pivot = new Vector2(0.5f, 1f);
            itemRt.anchoredPosition = new Vector2(0, -index * 60f);
            itemRt.sizeDelta = new Vector2(0, 60f);
            itemBtnGo.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 1f);

            Button itemBtn = itemBtnGo.GetComponent<Button>();
            itemBtn.onClick.AddListener(() => { SelectOption(index); });

            GameObject itemTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            itemTextGo.transform.SetParent(itemRt, false);
            RectTransform itemTextRt = itemTextGo.GetComponent<RectTransform>();
            itemTextRt.anchorMin = Vector2.zero; itemTextRt.anchorMax = Vector2.one;
            itemTextRt.sizeDelta = Vector2.zero;
            TextMeshProUGUI itemText = itemTextGo.GetComponent<TextMeshProUGUI>();
            itemText.alignment = TextAlignmentOptions.Center;
            itemText.fontSize = 24;
            itemText.text = dropdownOptions[i];
        }
    }

    private void ToggleDropdownList()
    {
        if (dropdownListPanel != null)
        {
            dropdownListPanel.SetActive(!dropdownListPanel.activeSelf);
            if (dropdownListPanel.activeSelf) dropdownListPanel.transform.SetAsLastSibling();
        }
    }

    private void SelectOption(int index)
    {
        selectedLocationIndex = index;
        dropdownText.text = dropdownOptions[index];
        ToggleDropdownList();
    }

    public void ShowPanel(bool show)
    {
        if (panelGo != null)
        {
            panelGo.SetActive(show);
            if (show)
            {
                panelGo.transform.SetAsLastSibling();
                if (dropdownListPanel != null) dropdownListPanel.SetActive(false);
            }
        }
    }

    private void OnConfirmStartLocation()
    {
        if (dropdownOptions.Count == 0 || dropdownOptions[0] == "No Locations Found") return;
        
        string selectedName = dropdownOptions[selectedLocationIndex];
        if (availableLocations.TryGetValue(selectedName, out Vector3 startPos))
        {
            CampusManager campusManager = FindFirstObjectByType<CampusManager>();
            if (campusManager != null)
            {
                // Tell CampusManager this is the start location
                campusManager.SetExplicitStartLocation(startPos);
            }
            ShowPanel(false);
        }
    }
}

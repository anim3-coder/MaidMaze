using LightReflectiveMirror;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class UIPlayMenu : MonoBehaviour
{
    [SerializeField] private PanelRenderer doc;
    private string[] tabNames = { "Main", "Graphics", "Controls" };
    private VisualElement root;
    private Button[] tabButtons;
    private VisualElement[] tabContents;
    public bool IsEnable
    {
        get
        {
            if(root == null) return false;
            return root.style.display == DisplayStyle.Flex;
        }
    }
    public void SetEnable(bool value)
    {
        root.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OnEnable()
    {
        doc.RegisterUIReloadCallback(OnUIReload);
    }

    private void OnDisable()
    {
        doc.UnregisterUIReloadCallback(OnUIReload);
    }

    private const float serverListRefreshInterval = 5;
    private float _lastServerListRequestTime = 0;
    private void Update()
    {
        if (serverListRefreshInterval > 0f && IsEnable) 
        {
            if (math.abs(Time.time - _lastServerListRequestTime) > serverListRefreshInterval)
            {
                RequestServerList();
                Debug.Log(NetGame.LRM.relayServerList.Count);
            }
        }
    }

    void RequestServerList()
    {
        NetGame.LRM.RequestServerList();
        _lastServerListRequestTime = Time.time;
    }

    private void OnUIReload(PanelRenderer renderer, VisualElement rootElement, int v)
    {
        root = rootElement.Q<VisualElement>("PlayMenu");
        listView = root.Q<ListView>("RoomList");
        var scrollView = listView.Q<ScrollView>();
        scrollView.AddToClassList("tab-scroll");
        if(NetGame.IsInitLRM) NetGame.LRM.serverListUpdated.AddListener(ConfigureListView);
        ConfigureListView();

        var createTab = root.Q<VisualElement>("Tab_Graphics");
        serverName = createTab.Q<TextField>();
        serverPublicToggle = createTab.Q<Toggle>();
        createTab.Q<Button>("Create").clicked += CreateHost;

        var enterTab = root.Q<VisualElement>("Tab_Controls");
        enterField = enterTab.Q<TextField>();
        enterField.RegisterValueChangedCallback(EnterField_Handle);
        enterTab.Q<Button>().clicked += EnterButton_Connect;

        #region Buttons
        tabButtons = new Button[tabNames.Length];
        tabContents = new VisualElement[tabNames.Length];
        for (int i = 0; i < tabNames.Length; i++)
        {
            string name = tabNames[i];
            tabButtons[i] = root.Q<Button>($"TabButton_{name}");
            tabContents[i] = root.Q<VisualElement>($"Tab_{name}");
            int index = i;
            tabButtons[i].clicked += () => SwitchToTab(index);
        }
        root.Q<Button>($"TabButton_Exit").clicked += () => SetEnable(false);
        
        #endregion

        SetEnable(false);
    }
    private void SwitchToTab(int index)
    {
        for (int i = 0; i < tabButtons.Length; i++)
        {
            bool isActive = (i == index);
            tabButtons[i].EnableInClassList("pagination-button--active", isActive);
            tabContents[i].EnableInClassList("tab-active", isActive);
        }
    }


    #region CreateTab
    private TextField serverName;
    private Toggle serverPublicToggle;
    void ApplyServerSettingsToLRM()
    {
        if (NetGame.LRM != null)
        {
            NetGame.LRM.serverName = serverName.value;
            NetGame.LRM.extraServerData = "...";
            NetGame.LRM.maxServerPlayers = 4;
            NetGame.LRM.isPublicServer = serverPublicToggle.value;
        }
    }
    private void CreateHost()
    {
        ApplyServerSettingsToLRM();
        NetGame.singleton.StartHost();
    }
    #endregion

    #region EnterTab
    private TextField enterField;

    private void EnterField_Handle(ChangeEvent<string> change)
    {
        string filtered = new string((change.newValue ?? string.Empty)
            .Where(c => !char.IsDigit(c))   // только ASCII и не цифры
            .ToArray()).ToUpper();
        enterField.value = filtered;
    }

    private void EnterButton_Connect()
    {
        Connect(enterField.value.ToUpper());
    }
    #endregion

    #region ListView
    private ListView listView;
    private List<Room> m_Items => NetGame.LRM.relayServerList;
    private void ConfigureListView()
    {
        listView.itemsSource = m_Items;
        listView.bindItem = BindItem;
        listView.fixedItemHeight = 64;
        listView.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
        listView.selectionType = SelectionType.None;
        listView.Rebuild();
    }

    private void BindItem(VisualElement element, int index)
    {
        var data = NetGame.LRM.relayServerList[index];
        var button = element.Q<Button>("server-button");
        if (button == null) return;

        element.Q<Label>("server-name").text = data.serverName;
        element.Q<Label>("player-count").text = $"{data.currentPlayers}/{data.maxPlayers}";

        // Отписываемся от предыдущего обработчика, если он был
        if (button.userData is Action previous)
            button.clicked -= previous;

        // Создаём новый обработчик с захваченными данными
        Action handler = () => OnServerClicked(data);
        button.userData = handler;
        button.clicked += handler;
    }

    private void OnServerClicked(Room data)
    {
        Connect(data.serverId);
    }

    private void Connect(string v)
    {
        Debug.Log($"Connecting to {v}");
        NetGame.singleton.networkAddress = v;
        NetGame.singleton.StartClient();
    }

    #endregion
}

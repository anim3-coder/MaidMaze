using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class LobbyHUD : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private float refreshInterval = 0.25f;

    private Label _playersCountLabel;
    private Label _lobbyCodeValueLabel;
    private Label _copyHintLabel;
    private Button _lobbyCodeButton;

    private IVisualElementScheduledItem _hideHintSchedule;
    private float _nextRefreshTime;

    private int _cachedCurrentPlayers = -1;
    private int _cachedMaxPlayers = -1;
    private string _cachedLobbyCode = string.Empty;

    private void Awake()
    {
        if (document == null)
            document = GetComponent<UIDocument>();

        var root = document.rootVisualElement;
        _playersCountLabel = root.Q<Label>("players-count-label");
        _lobbyCodeValueLabel = root.Q<Label>("lobby-code-value");
        _copyHintLabel = root.Q<Label>("copy-hint-label");
        _lobbyCodeButton = root.Q<Button>("lobby-code-button");

        if (_lobbyCodeButton != null)
            _lobbyCodeButton.clicked += OnCopyLobbyCodeClicked;
    }

    private void OnEnable()
    {
        ForceRefresh();
    }

    private void OnDestroy()
    {
        if (_lobbyCodeButton != null)
            _lobbyCodeButton.clicked -= OnCopyLobbyCodeClicked;
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextRefreshTime)
            return;

        _nextRefreshTime = Time.unscaledTime + refreshInterval;
        RefreshUI();
    }

    public void ForceRefresh()
    {
        _cachedCurrentPlayers = -1;
        _cachedMaxPlayers = -1;
        _cachedLobbyCode = null;
        RefreshUI();
    }

    private void RefreshUI()
    {
        int currentPlayers = GetCurrentPlayersCount();
        int maxPlayers = GetMaxPlayersCount();
        string lobbyCode = ResolveLobbyCode();

        if (currentPlayers != _cachedCurrentPlayers || maxPlayers != _cachedMaxPlayers)
        {
            _cachedCurrentPlayers = currentPlayers;
            _cachedMaxPlayers = maxPlayers;

            if (_playersCountLabel != null)
                _playersCountLabel.text = $"Игроки: {currentPlayers}/{maxPlayers}";
        }

        if (lobbyCode != _cachedLobbyCode)
        {
            _cachedLobbyCode = lobbyCode;

            if (_lobbyCodeValueLabel != null)
                _lobbyCodeValueLabel.text = lobbyCode;
        }
    }

    private int GetCurrentPlayersCount()
    {
        if (NetworkServer.active)
            return NetworkServer.connections.Count;

        if (NetworkClient.active)
        {
            int spawnedPlayers = NetworkClient.spawned.Values
                .Count(identity => identity != null && identity.GetComponent<Player>() != null);

            return Mathf.Max(1, spawnedPlayers);
        }

        return 0;
    }

    private int GetMaxPlayersCount()
    {
        if (NetGame.singleton != null)
            return NetGame.singleton.maxConnections;

        if (NetGame.IsInitLRM && NetGame.LRM.maxServerPlayers > 0)
            return NetGame.LRM.maxServerPlayers;

        return 4;
    }

    private string ResolveLobbyCode()
    {
        if (NetGame.IsInitLRM && !string.IsNullOrWhiteSpace(NetGame.LRM.serverId))
            return NetGame.LRM.serverId.ToUpperInvariant();

        if (NetGame.singleton != null && !string.IsNullOrWhiteSpace(NetGame.singleton.networkAddress))
            return NetGame.singleton.networkAddress.ToUpperInvariant();

        return "-----";
    }

    private void OnCopyLobbyCodeClicked()
    {
        if (string.IsNullOrWhiteSpace(_cachedLobbyCode) || _cachedLobbyCode == "-----")
            return;

        GUIUtility.systemCopyBuffer = _cachedLobbyCode;

        if (_copyHintLabel == null)
            return;

        _copyHintLabel.AddToClassList("copy-hint--visible");
        _hideHintSchedule?.Pause();
        _hideHintSchedule = _copyHintLabel.schedule
            .Execute(() => _copyHintLabel.RemoveFromClassList("copy-hint--visible"))
            .StartingIn(1500);
    }
}
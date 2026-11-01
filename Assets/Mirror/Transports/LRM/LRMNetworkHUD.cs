using System.Collections.Generic;
using LightReflectiveMirror; // Пространство имён LRM
using Mirror;
using UnityEngine;

namespace Mirror
{
    /// <summary>
    /// HUD для NetworkManager, адаптированный под Light Reflective Mirror.
    /// Позволяет хостить, подключаться по Server ID/URI и просматривать список серверов LRM.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Network/LRM Network Manager HUD")]
    [RequireComponent(typeof(NetworkManager))]
    [HelpURL("https://github.com/Speidy674/Light-Reflective-Mirror")]
    public class LRMNetworkManagerHUD : MonoBehaviour
    {
        [Header("UI Настройки")]
        public int offsetX;
        public int offsetY;
        public int width = 320;

        [Header("Список серверов")]
        [Tooltip("Автоматически запрашивать список серверов при запуске клиента.")]
        public bool autoRequestServerList = true;
        [Tooltip("Интервал автообновления списка серверов (в секундах). 0 = отключено.")]
        public float serverListRefreshInterval = 5f;

        private NetworkManager _manager;
        private LightReflectiveMirrorTransport _lrmTransport;

        // Поля для ввода Server ID / URI
        private string _connectAddress = "";

        // Поля для настройки сервера (отображаются при хостинге)
        private string _serverName = "My LRM Server";
        private string _extraServerData = "";
        private int _maxPlayers = 8;
        private bool _isPublicServer = true;

        // Для списка серверов
        private float _lastServerListRequestTime;
        private Vector2 _serverListScrollPosition;

        // Кэш для отображения Server ID хоста
        private string _hostServerId = "";

        void Awake()
        {
            _manager = GetComponent<NetworkManager>();

            // Получаем ссылку на транспорт LRM, если он установлен
            if (_manager.transport is LightReflectiveMirrorTransport lrm)
            {
                _lrmTransport = lrm;
                _connectAddress = _lrmTransport.serverId; // Попытка получить текущий ID (если есть)
            }
            else
            {
                Debug.LogError("[LRM HUD] NetworkManager не использует LightReflectiveMirrorTransport! HUD может работать некорректно.");
            }
        }

        void Update()
        {
            // Автоматическое обновление списка серверов
            if (_lrmTransport != null && autoRequestServerList && serverListRefreshInterval > 0f)
            {
                if (Time.time - _lastServerListRequestTime > serverListRefreshInterval)
                {
                    RequestServerList();
                }
            }

            // Получаем Server ID хоста, если мы хостим
            if (NetworkServer.active && _lrmTransport != null)
            {
                // В разных версиях LRM поле может называться serverId или ServerID.
                // Если возникнет ошибка компиляции, проверьте имя поля в исходниках LRM.
                _hostServerId = _lrmTransport.serverId;
            }
            else
            {
                _hostServerId = "";
            }
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10 + offsetX, 40 + offsetY, width, 9999));

            if (!NetworkClient.isConnected && !NetworkServer.active)
                DrawStartButtons();
            else
                DrawStatusLabels();

            // Кнопка "Client Ready" появляется, если клиент подключён, но ещё не готов
            if (NetworkClient.isConnected && !NetworkClient.ready)
            {
                if (GUILayout.Button("Client Ready"))
                {
                    NetworkClient.Ready();
                    if (NetworkClient.localPlayer == null)
                        NetworkClient.AddPlayer();
                }
            }

            DrawStopButtons();

            // Список серверов (показываем только когда мы не в игре)
            if (!NetworkClient.isConnected && !NetworkServer.active)
            {
                DrawServerList();
            }

            GUILayout.EndArea();
        }

        // --- Кнопки запуска ---
        void DrawStartButtons()
        {
            if (!NetworkClient.active)
            {
#if UNITY_WEBGL
                if (GUILayout.Button("Single Player"))
                {
                    NetworkServer.listen = false;
                    _manager.StartHost();
                }
#else
                // Хостинг (сервер + клиент)
                if (GUILayout.Button("Host (Server + Client)"))
                    StartHostWithLRMSettings();
#endif

                // Подключение клиента
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Connect to Server"))
                {
                    if (!string.IsNullOrWhiteSpace(_connectAddress))
                    {
                        _manager.networkAddress = _connectAddress.Trim();
                        _manager.StartClient();
                    }
                    else
                    {
                        Debug.LogWarning("[LRM HUD] Введите Server ID или URI для подключения.");
                    }
                }

                _connectAddress = GUILayout.TextField(_connectAddress, GUILayout.Width(180));

                GUILayout.EndHorizontal();

                // Подсказка
                GUILayout.Label("<i>Введите Server ID (например, ABC123) или URI (lrm://...)</i>");

                // Выделенный сервер
#if !UNITY_WEBGL
                if (GUILayout.Button("Server Only"))
                    StartServerWithLRMSettings();
#endif

                // Настройки сервера (показываем перед запуском)
                GUILayout.Space(5);
                GUILayout.Label("<b>Настройки сервера:</b>");
                GUILayout.BeginHorizontal();
                GUILayout.Label("Name:", GUILayout.Width(50));
                _serverName = GUILayout.TextField(_serverName);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Extra:", GUILayout.Width(50));
                _extraServerData = GUILayout.TextField(_extraServerData);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Max Players:", GUILayout.Width(80));
                string maxPlayersStr = GUILayout.TextField(_maxPlayers.ToString(), GUILayout.Width(40));
                if (int.TryParse(maxPlayersStr, out int max))
                    _maxPlayers = Mathf.Max(1, max);
                _isPublicServer = GUILayout.Toggle(_isPublicServer, "Public");
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label($"Подключение к {_manager.networkAddress}...");
                if (GUILayout.Button("Отменить попытку подключения"))
                    _manager.StopClient();
            }
        }

        // --- Статус ---
        void DrawStatusLabels()
        {
            if (NetworkServer.active && NetworkClient.active)
            {
                GUILayout.Label($"<b>Хост</b> через {Transport.active}");
                if (!string.IsNullOrEmpty(_hostServerId))
                {
                    GUILayout.Label($"Server ID: <b>{_hostServerId}</b>");
                    if (GUILayout.Button("Копировать Server ID"))
                        GUIUtility.systemCopyBuffer = _hostServerId;
                }
            }
            else if (NetworkServer.active)
            {
                GUILayout.Label($"<b>Сервер</b> через {Transport.active}");
                if (!string.IsNullOrEmpty(_hostServerId))
                {
                    GUILayout.Label($"Server ID: <b>{_hostServerId}</b>");
                    if (GUILayout.Button("Копировать Server ID"))
                        GUIUtility.systemCopyBuffer = _hostServerId;
                }
            }
            else if (NetworkClient.isConnected)
            {
                GUILayout.Label($"<b>Клиент</b>: подключён к {_manager.networkAddress} через {Transport.active}");
            }
        }

        // --- Кнопки остановки ---
        void DrawStopButtons()
        {
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                GUILayout.BeginHorizontal();
#if UNITY_WEBGL
                if (GUILayout.Button("Stop Single Player"))
                    _manager.StopHost();
#else
                if (GUILayout.Button("Stop Host"))
                    _manager.StopHost();
                if (GUILayout.Button("Stop Client"))
                    _manager.StopClient();
#endif
                GUILayout.EndHorizontal();
            }
            else if (NetworkClient.isConnected)
            {
                if (GUILayout.Button("Stop Client"))
                    _manager.StopClient();
            }
            else if (NetworkServer.active)
            {
                if (GUILayout.Button("Stop Server"))
                    _manager.StopServer();
            }
        }

        // --- Список серверов LRM ---
        void DrawServerList()
        {
            if (_lrmTransport == null) return;

            GUILayout.Space(10);
            GUILayout.Label("<b>Список серверов LRM:</b>");

            if (GUILayout.Button("Обновить список"))
                RequestServerList();

            // Проверяем, есть ли данные
            var serverList = _lrmTransport.relayServerList;
            if (serverList == null || serverList.Count == 0)
            {
                GUILayout.Label("<i>Список пуст или ещё не загружен.</i>");
                return;
            }

            // Отображаем список в скролле
            _serverListScrollPosition = GUILayout.BeginScrollView(_serverListScrollPosition, GUILayout.Height(200));
            foreach (var server in serverList)
            {
                // Предполагаем, что у элемента есть поля: serverId, serverName, players, maxPlayers, extraData.
                // Если имена полей отличаются, проверьте исходники LRM (RelayServerInfo).
                string displayText = $"{server.serverName} ({server.currentPlayers}/{server.maxPlayers})";
                if (!string.IsNullOrEmpty(server.serverData))
                    displayText += $" - {server.serverData}";

                if (GUILayout.Button(displayText))
                {
                    // Подключаемся к выбранному серверу по его Server ID
                    _connectAddress = server.serverId;
                    _manager.networkAddress = server.serverId;
                    _manager.StartClient();
                }
            }
            GUILayout.EndScrollView();
        }

        // --- Вспомогательные методы ---
        void RequestServerList()
        {
            if (_lrmTransport != null)
            {
                _lrmTransport.RequestServerList();
                _lastServerListRequestTime = Time.time;
            }
        }

        void StartHostWithLRMSettings()
        {
            ApplyServerSettingsToLRM();
            _manager.StartHost();
        }

        void StartServerWithLRMSettings()
        {
            ApplyServerSettingsToLRM();
            _manager.StartServer();
        }

        void ApplyServerSettingsToLRM()
        {
            if (_lrmTransport != null)
            {
                _lrmTransport.serverName = _serverName;
                _lrmTransport.extraServerData = _extraServerData;
                _lrmTransport.maxServerPlayers = _maxPlayers;
                _lrmTransport.isPublicServer = _isPublicServer;
            }
        }
    }
}
using LightReflectiveMirror;
using Mirror;
using Mirror.Examples.Pong;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
public class NetGame : NetworkManager
{
    [Header("LRM Reconnect")]
    [Tooltip("Задержка перед попыткой переподключения (в секундах).")]
    public float reconnectDelay = 1.5f;

    [Tooltip("Максимальное количество попыток переподключения подряд.")]
    public int maxReconnectAttempts = 10;

    [Tooltip("Включить автоматическое переподключение при дисконнекте.")]
    public bool autoReconnectOnDisconnect = true;

    // Счётчик попыток
    private int _reconnectAttempts;
    private Coroutine _reconnectCoroutine;

    // Флаг, чтобы отличать запланированный StopClient от неожиданного дисконнекта
    private bool _intentionalDisconnect;

    public static bool IsInitLRM
    {
        get
        {
            if (singleton != null && singleton.transport is LightReflectiveMirrorTransport)
                return true;
            return false;
        }
    }

    public static LightReflectiveMirrorTransport LRM
    {
        get
        {
            if (singleton != null && singleton.transport is LightReflectiveMirrorTransport)
                return singleton.transport as LightReflectiveMirrorTransport;
            return null;
        }
    }

    public override void Awake()
    {
        base.Awake();
        SceneManager.LoadScene(offlineScene);
    }

    private void OnEnable()
    {
        if (LRM != null) LRM.disconnectedFromRelay.AddListener(TryReconnectToRelay);
    }

    private void OnDisable()
    {
        if(LRM != null) LRM.disconnectedFromRelay.RemoveListener(TryReconnectToRelay);
    }

    public override void OnDestroy()
    {
        Debug.Log("[NetGame] был уничтожен");
    }

    private void TryReconnectToRelay()
    {
        Debug.LogWarning("[NetGame] был зафиксирован дисконект от релея.");
        if (LRM != null) LRM.ConnectToRelay();
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        Debug.Log($"[NetGame] Подключился к {conn.address}");
        base.OnServerConnect(conn);
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        GameObject player = Instantiate(playerPrefab);
        NetworkServer.AddPlayerForConnection(conn, player);
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        Debug.Log($"[NetGame] Отключился от {conn.address}");
        base.OnServerDisconnect(conn);
    }
}
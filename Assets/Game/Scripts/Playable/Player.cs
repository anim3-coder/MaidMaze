using Mirror;
using System;
using System.Collections;
using System.Security.Principal;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [SerializeField] private GameObject prefab;
    public static Player current { get; private set; }

    [SerializeField] private Body body;
    public static event Action<Player> OnStartLocalPlayerEvent;

    public override void OnStartLocalPlayer()
    {
        current = this;
        CreateBody();
        SetName(Setup.profile.PlayerName);
        OnStartLocalPlayerEvent?.Invoke(this);

        var ui = FindAnyObjectByType<ChatUI>();
        if (ui != null)
        {
            ui.OnLocalSubmit += SendChat;          // UI → сеть
            OnChatMessage += ui.ReceiveMessage;  // сеть → UI
        }
    }

    #region Chat
    public static event Action<string, string> OnChatMessage;

    /// <summary>Клиентская точка входа: вызывается из ChatUI.</summary>
    public void SendChat(string message)
    {
        if (!isOwned) return;    // Command разрешён только владельцу
        CmdSendChat(message);
    }

    [Command]
    private void CmdSendChat(string message)
    {
        // Валидация на сервере — нельзя доверять клиенту
        if (string.IsNullOrWhiteSpace(message)) return;

        string sender = string.IsNullOrEmpty(_name) ? "Unknown" : _name;

        RpcReceiveChat(sender, message);
    }

    [ClientRpc]
    private void RpcReceiveChat(string sender, string message)
    {
        OnChatMessage?.Invoke(sender, message);
    }

    #endregion

    #region Name
    [SyncVar(hook = nameof(OnNameChanged))]
    private string _name;
    public string Name => _name;

    private void OnNameChanged(string oldValue, string newValue)
    {
        if (body != null)
            body.SetPlayerName(newValue);
        else
            StartCoroutine(IEOnNameChanged(newValue));
    }

    private IEnumerator IEOnNameChanged(string v)
    {
        while (body == null)
            yield return null;
        body.SetPlayerName(v);
    }

    [Command]
    public void SetName(string newName)
    {
        _name = newName;
    }
    #endregion

    #region CreateBody
    public static event Action<Body> OnCreateLocalBody;
    [Command]
    public void CreateBody()
    {
        if (body != null) return;

        GameObject go = Instantiate(prefab);
        NetworkServer.Spawn(go, connectionToClient); // виден только владельцу

        body = go.GetComponent<Body>();
        body.playerNetId = netId;
        TargetSetBody(body.netId); // уходит ТОЛЬКО владельцу
    }

    [TargetRpc]
    private void TargetSetBody(uint netId)
    {
        if (NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity identity))
        {
            body = identity.GetComponent<Body>();
            OnCreateLocalBody?.Invoke(body);
        }
        else
        {
            StartCoroutine(WaitForBody(netId));
        }
    }

    private System.Collections.IEnumerator WaitForBody(uint netId)
    {
        NetworkIdentity identity;
        while (!NetworkClient.spawned.TryGetValue(netId, out identity))
            yield return null;

        body = identity.GetComponent<Body>();
        OnCreateLocalBody?.Invoke(body);
    }
    #endregion
}
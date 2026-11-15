using Mirror;
using System;
using System.Security.Principal;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [SerializeField] private GameObject prefab;
    public static Player current { get; private set; }

    [SerializeField] public Body body;
    public static event Action<Player> OnStartLocalPlayerEvent;
    public override void OnStartLocalPlayer()
    {
        current = this;
        CreateBody();
        OnStartLocalPlayerEvent?.Invoke(this);
    }

    #region CreateBody
    public static event Action<Body> OnCreateLocalBody;
    [Command]
    public void CreateBody()
    {
        if (body != null) return;

        GameObject go = Instantiate(prefab);
        NetworkServer.Spawn(go, connectionToClient); // виден только владельцу

        body = go.GetComponent<Body>();
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
            // На случай гонки сообщений — ждём
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
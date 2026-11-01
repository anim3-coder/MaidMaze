using Mirror;
using UnityEngine;

/// <summary>
/// Пустой обьект, указывает на наличие игрока.
/// </summary>
public class Player : NetworkBehaviour
{
    [SerializeField] private GameObject prefab;
    public static Player current { get; private set; }
    public Body body;
    public override void OnStartLocalPlayer()
    {
        current = this;
        CreateBody();
    }

    [Command]
    public void CreateBody()
    {
        if (body == null)
        {
            GameObject go = Instantiate(prefab);
            NetworkServer.Spawn(go, connectionToClient);
            body = go.GetComponent<Body>();
        }
    }
}

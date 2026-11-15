using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Mathematics;
/// <summary>
/// Тело, То чем управляет Player.
/// </summary>
public class Body : NetworkBehaviour
{
    [SerializeField] private Rigidbody2D rig;
    [SerializeField] private float speed;
    [SerializeField] private SpriteRenderer renderer;
    [SerializeField] private Animator animator;
    [SerializeField] private UI_PlayerName m_playerName;
    private InputAction move;
    private InputActionAsset asset => InputSystem.actions;
    public void SetPlayerName(string v) => m_playerName.SetName(v);
    #region linearVelocity
    [SerializeField] private float smoothing = 0.15f;
    private Vector2 lastPosition;
    private Vector2 estimatedVelocity;
    public Vector2 linearVelocity
    {
        get
        {
            if (isOwned) return rig.linearVelocity;
            return estimatedVelocity;
        }
    }
    private void UpdateLV()
    {
        Vector2 current = transform.position;
        Vector2 delta = current - lastPosition;
        Vector2 instantVelocity = delta / Time.deltaTime;
        estimatedVelocity = Vector2.Lerp(instantVelocity, estimatedVelocity, smoothing);
        lastPosition = current;
    }
    #endregion

    public override void OnStartAuthority()
    {
        rig.bodyType = RigidbodyType2D.Dynamic;
        move = asset.FindAction("Move");
    }

    private void Update()
    {
        if (isOwned)
            UpdateOwned();
        else
            UpdateOther();

        if (math.abs(linearVelocity.x) > 0)
        {
            animator.SetBool("IsWalk", true);
            renderer.flipX = linearVelocity.x > 0 ? false : true;
        }
        else
        {
            animator.SetBool("IsWalk", false);
        }
    }
    /// <summary>
    /// Обновление только для владельца тела
    /// </summary>
    private void UpdateOwned()
    {
        rig.linearVelocity = new Vector2(move.ReadValue<Vector2>().x * speed, rig.linearVelocity.y);
    }
    /// <summary>
    /// Обновление для всех кто не владелец
    /// </summary>
    private void UpdateOther()
    {
        UpdateLV();
    }
}
using UnityEngine;
using Unity.Netcode;

public class NetworkPlayerController : NetworkBehaviour
{
    [Header("Movement & Dash")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float dashSpeed = 15f;
    [SerializeField] float dashDuration = 0.2f;
    [SerializeField] float dashCooldown = 1.5f;

    private CharacterController controller;
    private NetworkPlayerShooter shooter;
    
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        shooter = GetComponent<NetworkPlayerShooter>();
    }

    void Update()
    {
        if (!IsOwner) return;

        if (GameManager.Instance != null && GameManager.Instance.matchState.Value != 2) return;

        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");
        Vector2 inputDirection = new Vector2(horizontalInput, verticalInput);
        bool dashRequested = Input.GetKeyDown(KeyCode.Space);

        if (IsServer) MovePlayer(inputDirection, dashRequested);
        else MovePlayerRPC(inputDirection, dashRequested);
    }

    [Rpc(SendTo.Server)]
    private void MovePlayerRPC(Vector2 movementInput, bool dashRequested)
    {
        MovePlayer(movementInput, dashRequested);
    }

    private void MovePlayer(Vector2 movementInput, bool dashRequested)
    {
        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;
        if (dashTimer > 0) dashTimer -= Time.deltaTime;

        if (dashRequested && dashCooldownTimer <= 0)
        {
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
        }

        // MOVEMENT: Now cleanly moves forward/back/left/right relative to your spine!
        Vector3 moveDirection = (transform.forward * movementInput.y) + (transform.right * movementInput.x);
        if (moveDirection.magnitude > 1) moveDirection.Normalize();

        float currentSpeed = moveSpeed;
        if (dashTimer > 0) currentSpeed = dashSpeed; 
        else if (shooter != null && shooter.isCharging) currentSpeed *= 0.5f; 

        controller.Move((moveDirection * currentSpeed + Vector3.down * 5f) * Time.deltaTime);
    }
}
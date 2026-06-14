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
        bool dashRequested = Input.GetKeyDown(KeyCode.Space);

        // Calculate absolute world-space movement locally using the perfectly smooth local Camera!
        // This makes your client completely immune to network delay when calculating movement angles.
        Vector3 camForward = Camera.main.transform.forward;
        camForward.y = 0; 
        camForward.Normalize();

        Vector3 camRight = Camera.main.transform.right;
        camRight.y = 0;
        camRight.Normalize();

        // Calculate the absolute world direction you want to move in
        Vector3 absoluteMoveDirection = (camForward * verticalInput) + (camRight * horizontalInput);
        if (absoluteMoveDirection.magnitude > 1) absoluteMoveDirection.Normalize();

        // Grab exact mouse rotation from the camera
        float exactYaw = Camera.main.transform.eulerAngles.y;

        if (IsServer) MovePlayer(absoluteMoveDirection, exactYaw, dashRequested);
        else MovePlayerRPC(absoluteMoveDirection, exactYaw, dashRequested);
    }

    [Rpc(SendTo.Server)]
    private void MovePlayerRPC(Vector3 calculatedMoveDir, float clientYaw, bool dashRequested)
    {
        MovePlayer(calculatedMoveDir, clientYaw, dashRequested);
    }

    private void MovePlayer(Vector3 calculatedMoveDir, float clientYaw, bool dashRequested)
    {
        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;
        if (dashTimer > 0) dashTimer -= Time.deltaTime;

        if (dashRequested && dashCooldownTimer <= 0)
        {
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
        }

        // Snap the server's version of the model to match the client's camera
        transform.rotation = Quaternion.Euler(0f, clientYaw, 0f);

        float currentSpeed = moveSpeed;
        if (dashTimer > 0) currentSpeed = dashSpeed; 
        else if (shooter != null && shooter.isCharging) currentSpeed *= 0.5f; 

        // MOVEMENT: Now cleanly moves along the exact world-space trajectory the client requested!
        controller.Move((calculatedMoveDir * currentSpeed + Vector3.down * 5f) * Time.deltaTime);
    }
}
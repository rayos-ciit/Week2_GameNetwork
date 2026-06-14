using UnityEngine;
using Unity.Netcode;

public class NetworkPlayerController : NetworkBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float gravity = -9.8f;
    [SerializeField] float groundedGravity = -2f;
    [SerializeField] float jumpHeight = 2f;
    [SerializeField] KeyCode jumpKey = KeyCode.Space;

    private CharacterController controller;
    private NetworkPlayerShooter shooter;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        shooter = GetComponent<NetworkPlayerShooter>();
    }

    void Update()
    {
        if (!IsOwner) return;

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        Vector2 inputDirection = new Vector2(horizontalInput, verticalInput);
        bool jumpRequestedThisFrame = Input.GetKeyDown(jumpKey);

        if (IsServer) MovePlayer(inputDirection, jumpRequestedThisFrame);
        else MovePlayerRPC(inputDirection, jumpRequestedThisFrame);
    }

    [Rpc(SendTo.Server)]
    private void MovePlayerRPC(Vector2 movementInput, bool jumpRequested)
    {
        MovePlayer(movementInput, jumpRequested);
    }

    private void MovePlayer(Vector2 movementInput, bool jumpRequested)
    {
        if (Camera.main != null)
        {
            float cameraRotationY = Camera.main.transform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0, cameraRotationY, 0);
        }

        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f) verticalVelocity = groundedGravity;
            if (jumpRequested) verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 moveDirection = (transform.forward * movementInput.y) + (transform.right * movementInput.x);
        if (moveDirection.magnitude > 1) moveDirection.Normalize();

        // Slow down movement if charging the bow
        float currentSpeed = moveSpeed;
        if (shooter != null && shooter.isCharging)
        {
            currentSpeed *= 0.5f; 
        }

        controller.Move((moveDirection * currentSpeed + Vector3.up * verticalVelocity) * Time.deltaTime);
    }
}
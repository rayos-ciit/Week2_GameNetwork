using UnityEngine;
using Unity.Netcode;

public class PlayerCamera : NetworkBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float distanceFromPlayer = 5f;
    [SerializeField] private float heightOffset = 1.5f; // Looks slightly over the shoulder
    [SerializeField] private Vector2 pitchMinMax = new Vector2(-40f, 60f);

    private float pitch = 0f;
    private float yaw = 0f;
    private Camera mainCam;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            mainCam = Camera.main;
            mainCam.transform.SetParent(null);
            yaw = transform.eulerAngles.y; // Start facing the spawn direction
        }
    }

    void Update()
    {
        if (!IsOwner || mainCam == null) return;

        // Free the mouse if the match isn't actively playing (Menus/Countdown/Game Over)
        if (GameManager.Instance != null && GameManager.Instance.matchState.Value != 2)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        yaw += Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, pitchMinMax.x, pitchMinMax.y);

        // Rotate the actual player model horizontally so WASD always matches your aim
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    void LateUpdate()
    {
        if (!IsOwner || mainCam == null) return;

        Vector3 targetPosition = transform.position + Vector3.up * heightOffset;
        Quaternion camRotation = Quaternion.Euler(pitch, yaw, 0f);
        
        mainCam.transform.position = targetPosition - (camRotation * Vector3.forward * distanceFromPlayer);
        mainCam.transform.rotation = camRotation;
    }
}
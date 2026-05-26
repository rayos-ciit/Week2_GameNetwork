using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private TMP_Text textMesh;
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float lifetime = 0.8f;

    public void Initialize(int damageAmount)
    {
        if (textMesh != null)
        {
            textMesh.text = damageAmount.ToString();
        }
        
        // Auto-destroys the visual object after its lifespan expires
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Make the text float upwards over time
        transform.Translate(Vector3.up * moveSpeed * Time.deltaTime, Space.World);

        // Billboarding effect: Forces the text to always face the local player's camera
        if (Camera.main != null)
        {
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
        }
    }
}
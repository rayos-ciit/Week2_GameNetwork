using UnityEngine;

public class FloatingText : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float destroyTime = 1f;

    void Start()
    {
        Destroy(gameObject, destroyTime);
    }

    void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;
    }
}
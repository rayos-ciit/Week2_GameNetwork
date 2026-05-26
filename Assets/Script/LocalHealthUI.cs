using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class LocalHealthUI : NetworkBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private NetworkPlayerHealth healthScript;

    public override void OnNetworkSpawn()
    {
        // Hide the slider for everyone except the owner
        if (!IsOwner)
        {
            if (healthSlider != null) healthSlider.gameObject.SetActive(false);
            return;
        }

        // Subscribe to changes to update the bar
        healthScript.CurrentHealth.OnValueChanged += UpdateHealthBar;
        UpdateHealthBar(0, healthScript.CurrentHealth.Value);
    }

    private void UpdateHealthBar(int oldVal, int newVal)
    {
        // Assuming health is 0-100
        healthSlider.value = (float)newVal / 100f; 
    }
}
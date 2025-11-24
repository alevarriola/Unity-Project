using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vida")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI")]
    public Slider healthSlider;              // Asignar un Slider UI
    public TextMeshProUGUI healthText;       // texto "HP: X/Y"

    [Header("Feedback")]
    public GameObject deathVfx;              
    public bool destroyOnDeath = false;      

    public System.Action OnDeath;

    void Awake()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    public void TakeDamage(float dmg)
    {
        currentHealth -= dmg;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void UpdateUI()
    {
        if (healthSlider)
        {
            healthSlider.value = currentHealth / maxHealth;
        }

        if (healthText)
        {
            healthText.text = $"HP: {Mathf.RoundToInt(currentHealth)} / {Mathf.RoundToInt(maxHealth)}";
        }
    }

    void Die()
    {
        OnDeath?.Invoke();

        if (deathVfx)
        {
            Instantiate(deathVfx, transform.position, Quaternion.identity);
        }

        if (destroyOnDeath)
            Destroy(gameObject);
        else
            Debug.Log("Jugador muerto");
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        UpdateUI();
    }
}
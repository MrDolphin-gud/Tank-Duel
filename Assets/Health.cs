using UnityEngine;
using UnityEngine.UI; 

public class Health : MonoBehaviour
{
    [Header("Can Ayarları")]
    public float maxHealth = 100f;
    public float currentHealth;
    
    [Header("Durum")]
    public bool isShielded = false; 

    [Header("UI Bağlantısı")]
    public Slider healthBar; 

    void Start()
    {
        currentHealth = maxHealth;

        // Eğer bir Slider bağlandıysa, onun maksimum değerini canımıza eşitle
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }
    }

    public void TakeDamage(float amount)
    {
        if (isShielded)
        {
            Debug.Log("Kalkan hasarı engelledi");
            return;
        }

        currentHealth -= amount;

        // Can barını güncelle
        if (healthBar != null)
        {
            healthBar.value = currentHealth;
        }

        Debug.Log(gameObject.name + " hasar aldı. Kalan: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " elendi");
        gameObject.SetActive(false);
    }
}
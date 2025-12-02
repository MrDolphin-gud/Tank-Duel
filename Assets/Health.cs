using UnityEngine;
using UnityEngine.UI; 

public class Health : MonoBehaviour
{
    // Maksimum can değerini belirler
    public float maxHealth = 100f;
    // Mevcut can değerini tutar
    public float currentHealth;
    
    // Kalkan aktif mi kontrol eder
    public bool isShielded = false; 

    // Can çubuğu slider bileşeni
    public Slider healthBar; 

    void Start()
    {
        // Başlangıçta canı maksimum değere eşitler
        currentHealth = maxHealth;

        // Can çubuğu varsa ayarlarını yapar
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }
    }

    public void TakeDamage(float amount)
    {
        // Kalkan aktifse hasarı engeller
        if (isShielded)
        {
            Debug.Log("Kalkan hasarı engelledi");
            return;
        }

        // Canı azaltır
        currentHealth -= amount;

        // Can çubuğunu günceller
        if (healthBar != null)
        {
            healthBar.value = currentHealth;
        }

        // Konsola hasar bilgisini yazar
        Debug.Log(gameObject.name + " hasar aldı. Kalan: " + currentHealth);

        // Can sıfır veya altındaysa ölüm fonksiyonunu çağırır
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // Konsola ölüm mesajını yazar
        Debug.Log(gameObject.name + " elendi");
        // Objeyi devre dışı bırakır
        gameObject.SetActive(false);
    }
}
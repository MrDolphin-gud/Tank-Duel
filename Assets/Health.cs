using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.SceneManagement; 

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

    [Header("Eğitim Modu")]
    public bool isInvincible = false; // Sonsuz can (eğitim için)
    
    public void TakeDamage(float amount, GameObject damageSource = null)
    {
        // OYUNCU SONSUZ CAN KONTROLÜ - En başta kontrol et
        if (isInvincible && gameObject.CompareTag("Player"))
        {
            // Sonsuz can modunda hasar almaz, canı da düşürmez
            // Sadece log yaz ve çık
            Debug.Log($"OYUNCU SONSUZ CAN! Hasar engellendi: {amount}");
            
            // Canı minimum %10'da tut (görsel olarak)
            if (currentHealth < maxHealth * 0.1f)
            {
                currentHealth = maxHealth * 0.1f;
                if (healthBar != null) healthBar.value = currentHealth;
            }
            return; // Fonksiyondan tamamen çık
        }
        
        // Q-Learning Agent referansı
        QLearningAgent qlAgent = GetComponent<QLearningAgent>();
        
        // Kalkan aktifse hasarı engeller ve ödül ver
        if (isShielded)
        {
            Debug.Log("Kalkan hasarı engelledi");
            
            // Kalkan ile hasar engelleme ödülü (Q-Learning)
            if (qlAgent != null)
            {
                qlAgent.AddShieldBlockReward(3f); // Kalkan ile hasar engelleme = +3 puan (0-10 arası)
                Debug.Log($"{gameObject.name} kalkan ile hasarı engelledi! +3 Reward");
            }
            
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
        
        // --- OYUNCUDAN HASAR ALMA CEZASI (AI tankları için) ---
        if (damageSource != null && qlAgent != null)
        {
            bool isFromPlayer = damageSource.CompareTag("Player");
            
            if (isFromPlayer)
            {
                // Oyuncudan hasar alındı - AI tankına ceza ver (0-10 arası)
                float penalty = Mathf.Clamp(amount * 0.2f, 1f, 5f);
                qlAgent.Punish(penalty);
                Debug.Log($"{gameObject.name} oyuncudan {amount} hasar aldı! -{penalty:F1} Ceza");
                
                // Hint: Kalkan aktifleştir veya geri git
                if (qlAgent.GetComponent<EnemyController>()?.IsShieldReady() == true)
                {
                    qlAgent.TeachCorrectAction(9, 2f); // ActivateShield
                    Debug.Log("HINT: Hasar aldın, kalkan kullan!");
                }
                else
                {
                    qlAgent.TeachCorrectAction(1, 1f); // MoveBackward - kaç
                    Debug.Log("HINT: Hasar aldın, geri çekil!");
                }
            }
        }

        // Can sıfır veya altındaysa öl fonksiyonunu çağırır
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // Konsola ölüm mesajını yazar
        Debug.Log(gameObject.name + " elendi");
        
        // Enemy tank öldüyse Q-Learning modelini kaydet
        if (gameObject.CompareTag("Enemy"))
        {
            QLearningBrain brain = GetComponent<QLearningBrain>();
            if (brain != null)
            {
                brain.SaveModelManually();
                Debug.Log($"[QLearning] {gameObject.name} öldü, model kaydedildi");
            }
        }
        
        // Oyuncu veya düşman öldüğünde ana menüye dön
        if (gameObject.CompareTag("Player") || gameObject.CompareTag("Enemy"))
        {
            Debug.Log($"{gameObject.name} öldü! Ana menüye yönlendiriliyor...");
            
            // Kontrol scriptlerini devre dışı bırak
            TankController tankController = GetComponent<TankController>();
            if (tankController != null)
            {
                tankController.enabled = false;
            }
            
            QLearningAgent qlAgent = GetComponent<QLearningAgent>();
            if (qlAgent != null)
            {
                qlAgent.enabled = false;
            }
            
            EnemyController enemyController = GetComponent<EnemyController>();
            if (enemyController != null)
            {
                enemyController.enabled = false;
            }
            
            // Rigidbody'yi durdur
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            // Kısa bir gecikme ekle (ölüm animasyonu/efekti için)
            StartCoroutine(LoadMainMenuAfterDelay(2f));
        }
        else
        {
            // Diğer nesneler için normal yok etme
            Destroy(gameObject);
        }
    }
    

    System.Collections.IEnumerator LoadMainMenuAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Time.timeScale'i normale döndür 
        Time.timeScale = 1f;
        
        // Ana menü sahnesine geçiş yap
        SceneManager.LoadScene("MainMenu");
    }
}
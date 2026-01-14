using UnityEngine;

public class Mine : MonoBehaviour
{
    [Header("Ayarlar")]
    public float damage = 30f;           // Mayının vereceği hasar
    public AudioClip explosionSound;    // Patlama sesi
    public GameObject explosionEffect;  // Varsa patlama efekti 

    // Mayın sahibi bilgisi
    [HideInInspector] public string ownerTag;
    
    // Q-Learning Agent referansı (ödül/ceza için)
    [HideInInspector] public QLearningAgent ownerQLearningAgent;

    private void OnTriggerEnter(Collider other)
    {
        // Kendi sahibine çarpmayı engelle
        if (ownerQLearningAgent != null && other.gameObject == ownerQLearningAgent.gameObject)
        {
            return;
        }
        
        // Mayına çarpan nesnenin can sistemi (Health) var mı?
        Health targetHealth = other.GetComponent<Health>();

        if (targetHealth != null)
        {
            bool isPlayer = other.CompareTag("Player");
            bool isEnemy = other.CompareTag("Enemy");
            
            // Kalkan kontrolü
            bool damageBlocked = targetHealth.isShielded;
            
            if (!damageBlocked)
            {
                // Hasar ver - kaynağı belirt
                GameObject damageSource = null;
                if (ownerQLearningAgent != null)
                {
                    damageSource = ownerQLearningAgent.gameObject;
                }
                else if (!string.IsNullOrEmpty(ownerTag))
                {
                    GameObject owner = GameObject.FindGameObjectWithTag(ownerTag);
                    if (owner != null)
                    {
                        damageSource = owner;
                    }
                }
                targetHealth.TakeDamage(damage, damageSource);
            }
            
            
            if (ownerQLearningAgent != null)
            {
                // Kalkana vurma cezası
                if (damageBlocked && isPlayer)
                {
                    ownerQLearningAgent.Punish(3f); // Kalkana mayın ile vurma = -3 
                    Debug.Log($"{ownerQLearningAgent.name} oyuncunun kalkanına mayın ile vurdu! -3 Ceza");
                }
                else if (!damageBlocked)
                {
                    if (isPlayer)
                    {
                        // OYUNCUYA MAYIN İLE HASAR VERME: Ödül
                        ownerQLearningAgent.AddMineHitReward(5f); // Mayın ile oyuncuya vurma = +5 
                        ownerQLearningAgent.AddPlayerDamageDealt(damage);
                        
                        if (targetHealth.currentHealth <= 0)
                        {
                            ownerQLearningAgent.AddKillReward(1f); // Oyuncuyu mayın ile öldürme = +10
                            Debug.Log($"{ownerQLearningAgent.name} OYUNCUYU MAYIN ILE ÖLDÜRDÜ! +10 Reward");
                        }
                        else
                        {
                            Debug.Log($"{ownerQLearningAgent.name} oyuncuyu mayın ile vurdu! +5 Reward");
                        }
                    }
                    else if (isEnemy)
                    {
                        // ENEMY'YE MAYIN İLE HASAR VERME: Ceza
                        ownerQLearningAgent.Punish(5f); // Mayın ile enemy'ye vurma = -5 
                        
                        if (targetHealth.currentHealth <= 0)
                        {
                            ownerQLearningAgent.AddEnemyKillPenalty(1f); // Enemy'yi mayın ile öldürme = -10
                            Debug.Log($"{ownerQLearningAgent.name} ENEMY'Yİ MAYIN İLE ÖLDÜRDÜ! -10 Ceza");
                        }
                        else
                        {
                            Debug.Log($"{ownerQLearningAgent.name} enemy'yi mayın ile vurdu! -5 Ceza");
                        }
                    }
                }
            }
            
            // Patlama sesini çal
            if (explosionSound != null)
            {
                AudioSource.PlayClipAtPoint(explosionSound, transform.position, 1.0f);
            }

            // Patlama efekti
            if (explosionEffect != null)
            {
                Instantiate(explosionEffect, transform.position, Quaternion.identity);
            }

            // Mayını yok et
            Destroy(gameObject); 
        }
    }
}

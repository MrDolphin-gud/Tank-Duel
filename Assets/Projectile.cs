using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Ayarlar")]
    public float damage = 10f;
    
    // Merminin hasar verebileceği ve puan kazandıracağı etiketler
    public string[] targetTags = { "Player", "Enemy" };
    
    // Q-Learning Agent referansı (ödül/ceza için)
    [HideInInspector] public QLearningAgent shooterAgent;

    // Mermiyi atan GameObject 
    [HideInInspector] public GameObject shooterGameObject;
    
    // İsabet kontrolü
    private bool hasHitTarget = false;

    private void OnCollisionEnter(Collision collision)
    {
        // Çarptığımız objenin tag'ini kontrol ediyoruz
        bool hitValidTarget = false;
        
        foreach (string tag in targetTags)
        {
            if (collision.gameObject.CompareTag(tag))
            {
                // Kendi kendimizi vurmamak için kontrol 
                if (shooterAgent != null && collision.gameObject == shooterAgent.gameObject)
                    continue;

                // Çarptığımız objede Health scripti var mı?
                Health targetHealth = collision.gameObject.GetComponent<Health>();
                
                if (targetHealth != null)
                {
                    hitValidTarget = true;
                    hasHitTarget = true;
                    
                    // Kalkan kontrolü
                    bool damageBlocked = targetHealth.isShielded;
                    
                    if (!damageBlocked)
                    {
                        // Hasar ver - kaynağı belirt 
                        GameObject damageSource = null;
                        if (shooterAgent != null)
                        {
                            damageSource = shooterAgent.gameObject;
                        }
                        else if (shooterGameObject != null)
                        {
                            damageSource = shooterGameObject;
                        }
                        targetHealth.TakeDamage(damage, damageSource);
                    }
                    
                    // Oyuncuya vurduysa ödül Enemy'ye vurduysa ceza
                    bool isPlayer = collision.gameObject.CompareTag("Player");
                    bool isEnemy = collision.gameObject.CompareTag("Enemy");
                    
                    
                    if (shooterAgent != null)
                    {
                        // Kalkana vurma cezası
                        if (damageBlocked && isPlayer)
                        {
                            shooterAgent.Punish(3f); // Kalkana vurma = -3 
                            Debug.Log($"{shooterAgent.name} oyuncunun kalkanına vurdu -3 Ceza");
                        }
                        else if (!damageBlocked)
                        {
                            if (isPlayer)
                            {
                                if (targetHealth.currentHealth <= 0)
                                {
                                    shooterAgent.AddKillReward(1f); 
                                    Debug.Log($"{shooterAgent.name} OYUNCUYU ÖLDÜRDÜ +10 Reward");
                                }
                                else
                                {
                                    shooterAgent.AddHitReward(5f); 
                                    shooterAgent.AddPlayerDamageDealt(damage);
                                    Debug.Log($"{shooterAgent.name} oyuncuyu vurdu +5 Reward");
                                }
                            }
                            else if (isEnemy)
                            {
                                if (targetHealth.currentHealth <= 0)
                                {
                                    shooterAgent.AddEnemyKillPenalty(1f); 
                                    Debug.Log($"{shooterAgent.name} ENEMY'Yİ ÖLDÜRDÜ! -10 Ceza");
                                }
                                else
                                {
                                    shooterAgent.Punish(5f); 
                                    Debug.Log($"{shooterAgent.name} enemy'ye vurdu! -5 Ceza");
                                }
                            }
                        }
                    }
                }
                
                break;
            }
        }
        
        // Hedef dışı bir şeye çarptıysa (duvar, zemin vb.) - ıskalama cezası
        if (!hitValidTarget && shooterAgent != null && !hasHitTarget)
        {
            // Iskalama cezası (0-10 arası)
            shooterAgent.Punish(1f);
            
            // Oyuncuyu bul ve hint ver
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && shooterAgent != null)
            {
                Transform shooterTransform = shooterAgent.transform;
                EnemyController controller = shooterAgent.GetComponent<EnemyController>();
                
                Vector3 toPlayer = (player.transform.position - shooterTransform.position).normalized;
                
                // Taret varsa taret açısını kontrol et
                if (controller != null && controller.turretTransform != null)
                {
                    Vector3 turretForward = controller.turretTransform.forward;
                    float turretAngle = Vector3.Angle(turretForward, toPlayer);
                    Vector3 turretCross = Vector3.Cross(turretForward, toPlayer);
                    
                    if (turretAngle > 10f) // 10 dereceden fazla sapma varsa
                    {
                        if (turretCross.y > 0)
                        {
                            // Taret sağa dönmeli
                            shooterAgent.TeachCorrectActionWithAngle(5, turretAngle, "sağa", "Taret", "oyuncuya", 1f);
                        }
                        else
                        {
                            // Taret sola dönmeli
                            shooterAgent.TeachCorrectActionWithAngle(4, turretAngle, "sola", "Taret", "oyuncuya", 1f);
                        }
                    }
                }
                else
                {
                    // Taret yoksa gövde açısını kontrol et
                    float bodyAngle = Vector3.Angle(shooterTransform.forward, toPlayer);
                    Vector3 bodyCross = Vector3.Cross(shooterTransform.forward, toPlayer);
                    
                    if (bodyAngle > 10f)
                    {
                        if (bodyCross.y > 0)
                        {
                            shooterAgent.TeachCorrectActionWithAngle(3, bodyAngle, "sağa", "Gövde", "oyuncuya", 1f);
                        }
                        else
                        {
                            shooterAgent.TeachCorrectActionWithAngle(2, bodyAngle, "sola", "Gövde", "oyuncuya", 1f);
                        }
                    }
                }
            }
            
            Debug.Log($"{shooterAgent?.name} ıskaladı! -1 Ceza");
        }

        // Mermi yok olmalı
        Destroy(gameObject);
    }
}

using System.Collections.Generic;
using UnityEngine;

public class QLearningAgent : MonoBehaviour
{
    [Header("Q-Learning")]
    private QLearningBrain brain;
    private EnemyController controller;
    private Health health;

    [Header("Hedefleme")]
    private Transform currentTarget;
    public string[] targetTags = { "Enemy", "Player" };

    [Header("State Tracking")]
    private float decisionInterval = 0.2f; // Her 0.2 saniyede bir karar ver 
    private float lastDecisionTime = 0f;

    // Son karar verilen action ve state
    private int lastActionIndex = -1;
    private string lastStateString = "";
    
    // Sürekli aynı action seçilmesini engelle
    private int actionRepeatCount = 0;
    private int maxActionRepeats = 3; // Aynı action'ı maksimum 3 kez üst üste seçebilir

    void Awake()
    {
        // Awake'te referansları al ve action'ları register et
        brain = GetComponent<QLearningBrain>();
        controller = GetComponent<EnemyController>();
        health = GetComponent<Health>();

        if (brain == null)
        {
            Debug.LogError("QLearningBrain component bulunamadı!");
            return;
        }

        if (controller == null)
        {
            Debug.LogError("EnemyController component bulunamadı!");
            return;
        }

        // Actions'ları register et 
        RegisterActions();
        Debug.Log("[QLearningAgent] 11 action kaydedildi (Awake)");
    }

    void RegisterActions()
    {
        // Action 0: Move Forward
        brain.RegisterAction("MoveForward", p => MoveForward((float)p[0]), 1);
        
        // Action 1: Move Backward
        brain.RegisterAction("MoveBackward", p => MoveBackward((float)p[0]), 1);
        
        // Action 2: Rotate Body Left
        brain.RegisterAction("RotateLeft", p => RotateBody(-1f), 0);
        
        // Action 3: Rotate Body Right
        brain.RegisterAction("RotateRight", p => RotateBody(1f), 0);
        
        // Action 4: Rotate Turret Left
        brain.RegisterAction("RotateTurretLeft", p => RotateTurret(-1f), 0);
        
        // Action 5: Rotate Turret Right
        brain.RegisterAction("RotateTurretRight", p => RotateTurret(1f), 0);
        
        // Action 6: Fire
        brain.RegisterAction("Fire", p => Fire(), 0);
        
        // Action 7: Dash
        brain.RegisterAction("Dash", p => Dash(), 0);
        
        // Action 8: Place Mine
        brain.RegisterAction("PlaceMine", p => PlaceMine(), 0);
        
        // Action 9: Activate Shield
        brain.RegisterAction("ActivateShield", p => ActivateShield(), 0);
        
        // Action 10: Do Nothing (hareket durdur)
        brain.RegisterAction("DoNothing", p => DoNothing(), 0);
    }

    [Header("Reward Tracking")]
    private float lastAlignmentCheckTime = 0f;
    private float alignmentCheckInterval = 0.2f; // Her 0.2 saniyede bir kontrol et 
    
    // Dönük kalma süreleri 
    private float bodyAlignedTime = 0f; // Gövde ne kadar süredir dönük
    private float turretAlignedTime = 0f; // Taret ne kadar süredir dönük
    private float alignmentRewardThreshold = 2f; // 2 saniye dönük kalınca ödül ver
    private float lastBodyRewardTime = 0f; // Son gövde ödülü zamanı
    private float lastTurretRewardTime = 0f; // Son taret ödülü zamanı
    private float alignmentRewardCooldown = 3f; // Ödüller arası bekleme süresi

    void FixedUpdate()
    {
        if (brain == null || controller == null || health == null) return;
        if (health.currentHealth <= 0) return; // Ölüyse çalışma

        // Belirli aralıklarla karar ver
        if (Time.time - lastDecisionTime >= decisionInterval)
        {
            MakeDecision();
            lastDecisionTime = Time.time;
        }

        // Decay sadece DoNothing action'ı seçildiğinde uygulanır 
        if (lastActionWasDoNothing)
        {
            // DoNothing seçildiyse yavaşça dur
            currentMoveInput *= movementDecayRate;
            if (Mathf.Abs(currentMoveInput) < 0.01f) currentMoveInput = 0f;
            
            currentRotateInput *= movementDecayRate;
            if (Mathf.Abs(currentRotateInput) < 0.01f) currentRotateInput = 0f;
            
            currentTurretRotateInput *= movementDecayRate;
            if (Mathf.Abs(currentTurretRotateInput) < 0.01f) currentTurretRotateInput = 0f;
        }
        
        // Sürekli hareket ve dönüş uygula 
        controller.Move(currentMoveInput, currentRotateInput);
        controller.RotateTurretManual(currentTurretRotateInput);

        // Oyuncuya dönük olma ödülü kontrolü
        if (Time.time - lastAlignmentCheckTime >= alignmentCheckInterval)
        {
            CheckPlayerAlignmentReward();
            lastAlignmentCheckTime = Time.time;
        }
    }

    void MakeDecision()
    {
        // State'i oluştur
        List<float> inputs = BuildStateInputs();
        brain.SetInputs(inputs);
        
        // State string'ini kaydet 
        lastStateString = brain.EncodeState(inputs);
        
        int actionIndex = -1;
        if (ShouldFireAtPlayer())
        {
            if (Random.value < 0.7f) //Öğrenme açısından ateş etmesi random
            {
                actionIndex = 6; // Fire action
                string currentState = brain.EncodeState(inputs);
                brain.SetLastAction(actionIndex, currentState);
                Debug.Log("Oyuncuya dönük - Ateş ediliyor!");
            }
        }
        
        // Eğer ateş kararı alınmadıysa normal Q-learning kararı al
        if (actionIndex == -1)
        {
            actionIndex = brain.DecideAction(); //Kendisi ateş etmeyi öğrenecek mi?
        }
        
        // Sürekli aynı action seçilmesini engelle 
        if (actionIndex == lastActionIndex && actionIndex != 6)
        {
            actionRepeatCount++;
            if (actionRepeatCount >= maxActionRepeats)
            {
                // Aynı action'ı çok fazla tekrar seçiyor DoNothing yap veya rastgele action seç
                if (Random.value < 0.5f)
                {
                    actionIndex = 10; // DoNothing
                }
                else
                {
                    // Rastgele farklı bir action seç (hareket/dönüş action'ları hariç)
                    int[] nonMovementActions = { 6, 7, 8, 9, 10 }; // Fire, Dash, Mine, Shield, DoNothing
                    actionIndex = nonMovementActions[Random.Range(0, nonMovementActions.Length)];
                }
                actionRepeatCount = 0;
                
                // Action değiştirildiyse SetLastAction'ı tekrar çağır (doğru action için reward verilsin)
                string currentState = brain.EncodeState(inputs);
                brain.SetLastAction(actionIndex, currentState);
            }
        }
        else
        {
            actionRepeatCount = 0;
        }
        
        lastActionIndex = actionIndex;

        // Action'ı execute et
        ExecuteAction(actionIndex);
        
        // DoNothing action'ı seçildi mi kontrol et
        lastActionWasDoNothing = (actionIndex == 10);
    }
    
    // Oyuncuya dönük mü ve ateş etmeli mi kontrol et
    bool ShouldFireAtPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return false;
        
        Vector3 toPlayer = (player.transform.position - transform.position).normalized;
        
        // Taret kontrolü (varsa)
        if (controller.turretTransform != null)
        {
            Vector3 turretForward = controller.turretTransform.forward;
            float turretAngle = Vector3.Angle(turretForward, toPlayer);
            if (turretAngle < 15f) // 15 derece içindeyse - iyi nişan
            {
                return true;
            }
        }
        
        // Gövde kontrolü
        float bodyAngle = Vector3.Angle(transform.forward, toPlayer);
        if (bodyAngle < 15f) // 15 derece içindeyse - iyi nişan
        {
            return true;
        }
        
        return false;
    }

    List<float> BuildStateInputs()
    {
        List<float> inputs = new List<float>();

        // Hedef bul (sadece mesafe ve hedef tipi için)
        FindClosestTarget();

        if (currentTarget != null)
        {
            // Hedefe mesafe (normalize: 0-50m -> 0-1)
            float distance = Vector3.Distance(transform.position, currentTarget.position);
            inputs.Add(Mathf.Clamp01(distance / 50f));

            // Hedef oyuncu mu? (1 = evet, 0 = hayır)
            inputs.Add(currentTarget.CompareTag("Player") ? 1f : 0f);
        }
        else
        {
            // Hedef yok
            inputs.Add(1f); // Maksimum mesafe
            inputs.Add(0f); // Oyuncu değil
        }

        // Kendi pozisyon bilgisi (AI'nın kendi konumunu öğrenmesi için)
        // Normalize edilmiş pozisyon (örnek: -50 ile 50 arası -> 0 ile 1 arası)
        inputs.Add(Mathf.Clamp01((transform.position.x + 50f) / 100f));
        inputs.Add(Mathf.Clamp01((transform.position.z + 50f) / 100f));

        // Oyuncuya göre açı bilgisi (AI oyuncuya dönük olmayı öğrensin)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 toPlayer = (player.transform.position - transform.position).normalized;
            
            // Gövde oyuncuya göre açısı (0-1 arası: 0 = tam dönük, 1 = tam ters)
            float bodyAngleToPlayer = Vector3.Angle(transform.forward, toPlayer);
            inputs.Add(bodyAngleToPlayer / 180f); // 0-180 derece -> 0-1
            
            // Gövde hangi yöne dönmeli? (sol: -1, sağ: +1, normalize: 0-1)
            Vector3 cross = Vector3.Cross(transform.forward, toPlayer);
            float turnDirection = cross.y > 0 ? 1f : 0f; // 1 = sağa dön, 0 = sola dön
            inputs.Add(turnDirection);
            
            // Taret oyuncuya göre açısı (
            if (controller.turretTransform != null)
            {
                Vector3 turretForward = controller.turretTransform.forward;
                float turretAngleToPlayer = Vector3.Angle(turretForward, toPlayer);
                inputs.Add(turretAngleToPlayer / 180f); // 0-180 derece -> 0-1
                
                // Taret hangi yöne dönmeli?
                Vector3 turretCross = Vector3.Cross(turretForward, toPlayer);
                float turretTurnDirection = turretCross.y > 0 ? 1f : 0f;
                inputs.Add(turretTurnDirection);
            }
            else
            {
                inputs.Add(bodyAngleToPlayer / 180f); 
                inputs.Add(turnDirection); 
            }
        }
        else
        {
            // Oyuncu yoksa maksimum açı ve rastgele yön
            inputs.Add(1f); // Maksimum açı
            inputs.Add(0.5f); // Belirsiz yön
            inputs.Add(1f); // Taret maksimum açı
            inputs.Add(0.5f); // Taret belirsiz yön
        }

        // Kendi can durumu (0-1 arası)
        inputs.Add(health.currentHealth / health.maxHealth);

        // Cooldown durumları (1 = hazır, 0 = hazır değil)
        inputs.Add(controller.IsDashReady() ? 1f : 0f);
        inputs.Add(controller.IsMineReady() ? 1f : 0f);
        inputs.Add(controller.IsShieldReady() ? 1f : 0f);

        // Kalkan aktif mi? (1 = evet, 0 = hayır)
        inputs.Add(health.isShielded ? 1f : 0f);

        // Hareket durumu (şu anda hareket ediyor mu?)
        inputs.Add(Mathf.Abs(currentMoveInput)); // 0-1 arası hareket hızı

        return inputs;
    }

    string EncodeState(List<float> inputs)
    {
        // QLearningBrain'in EncodeState metodunu kullanmak yerine burada da quantize ediyoruz
        List<string> quantized = new List<string>();
        foreach (float val in inputs)
        {
            float quantizedVal = Mathf.Round(val * 2f) / 2f; // 0.5 hassasiyetinde 
            quantized.Add(quantizedVal.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
        }
        return string.Join("_", quantized);
    }

    void ExecuteAction(int actionIndex)
    {
        switch (actionIndex)
        {
            case 0: // MoveForward
                brain.ExecuteAction(actionIndex, 1f);
                break;
            case 1: // MoveBackward
                brain.ExecuteAction(actionIndex, -1f);
                break;
            case 2: // RotateLeft
                brain.ExecuteAction(actionIndex);
                break;
            case 3: // RotateRight
                brain.ExecuteAction(actionIndex);
                break;
            case 4: // RotateTurretLeft
                brain.ExecuteAction(actionIndex);
                break;
            case 5: // RotateTurretRight
                brain.ExecuteAction(actionIndex);
                break;
            case 6: // Fire
                brain.ExecuteAction(actionIndex);
                break;
            case 7: // Dash
                brain.ExecuteAction(actionIndex);
                break;
            case 8: // PlaceMine
                brain.ExecuteAction(actionIndex);
                break;
            case 9: // ActivateShield
                brain.ExecuteAction(actionIndex);
                break;
            case 10: // DoNothing
                DoNothing(); // Hareket input'larını sıfırla
                brain.ExecuteAction(actionIndex);
                break;
        }
    }
    

    private float currentMoveInput = 0f;
    private float currentRotateInput = 0f;
    private float currentTurretRotateInput = 0f;
    
    // Hareket decay (hareket action'ı seçilmezse yavaşça dur)
    private float movementDecayRate = 0.99f; // Her frame %1 azal (daha yavaş decay)
    private bool lastActionWasDoNothing = false; // Son action DoNothing miydi?

    void MoveForward(float speed)
    {
        currentMoveInput = speed;
    }

    void MoveBackward(float speed)
    {
        currentMoveInput = speed;
    }

    void RotateBody(float direction)
    {
        currentRotateInput = direction;
    }

    void RotateTurret(float direction)
    {
        currentTurretRotateInput = direction;
    }
    
    void DoNothing()
    {
        // DoNothing action'ında tüm hareket ve dönüş input'larını sıfırla
        currentMoveInput = 0f;
        currentRotateInput = 0f;
        currentTurretRotateInput = 0f; // Taret dönüşünü de durdur
    }

    void Fire()
    {
        GameObject bullet = controller.FireManual();
        if (bullet != null)
        {
            // Ateş etme ödülü
            Reward(1f); // Her ateş etme = +1 puan 
            
            // Projectile'a referans ekle
            Projectile proj = bullet.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.shooterAgent = this;
            }
        }
        else
        {
            // Fire action başarısız 
            Punish(0.5f); // Fire kaçırma cezası 
        }
    }

    void Dash()
    {
        // Dash öncesi cooldown kontrolü
        bool dashWasReady = controller.IsDashReady();
        controller.DashManual();
        
        // Dash başarısız olduysa (cooldown'da) küçük ceza
        if (!dashWasReady)
        {
            Punish(0.5f); // Dash başarısız cezası 
        }
    }

    void PlaceMine()
    {
        // Mine öncesi cooldown kontrolü
        bool mineWasReady = controller.IsMineReady();
        controller.PlaceMineManual();
        
        // Mine yerleştirme başarısız olduysa (cooldown'ta) küçük ceza
        if (!mineWasReady)
        {
            Punish(0.5f); // Mine başarısız cezası 
        }
    }

    void ActivateShield()
    {
        // Shield öncesi cooldown kontrolü
        bool shieldWasReady = controller.IsShieldReady();
        controller.ActivateShieldManual();
        
        // Shield aktivasyonu başarısız olduysa (cooldown'da) küçük ceza
        if (!shieldWasReady)
        {
            Punish(0.5f); // Shield başarısız cezası 
        }
    }
    

    void FindClosestTarget()
    {
        currentTarget = null;
        float closestDistance = float.MaxValue;

        foreach (string tag in targetTags)
        {
            GameObject[] targets = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject target in targets)
            {
                if (target == gameObject) continue; // Kendini hedefleme

                Health targetHealth = target.GetComponent<Health>();
                if (targetHealth != null && targetHealth.currentHealth <= 0) continue; // Ölü hedefleme

                float distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    currentTarget = target.transform;
                }
            }
        }
    }

    // ===================================================================================
    // ALIGNMENT REWARD (Oyuncuya dönük olma)
    // ===================================================================================

    void CheckPlayerAlignmentReward()
    {
        // Oyuncuyu bul
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // Oyuncu yoksa timer'ları sıfırla
            bodyAlignedTime = 0f;
            turretAlignedTime = 0f;
            return;
        }

        Vector3 toPlayer = (player.transform.position - transform.position).normalized;

        // Gövde oyuncuya dönük mü?
        float bodyAngle = Vector3.Angle(transform.forward, toPlayer);
        if (bodyAngle < 30f) // 30 derece içindeyse
        {
            bodyAlignedTime += alignmentCheckInterval; // Süreyi artır
            
                // Belirli süre dönük kaldıysa ve cooldown dolmuşsa ödül ver
                if (bodyAlignedTime >= alignmentRewardThreshold && 
                    Time.time - lastBodyRewardTime >= alignmentRewardCooldown)
                {
                    float rewardAmount = 2f; // Gövde dönük kalma ödülü (0-10 arası ölçeklendirildi: 20 -> 2)
                    Reward(rewardAmount);
                    lastBodyRewardTime = Time.time;
                    bodyAlignedTime = 0f; // Timer'ı sıfırla (tekrar ödül için)
                    Debug.Log($"{gameObject.name} gövdesi {alignmentRewardThreshold} saniye oyuncuya dönük kaldı! +{rewardAmount} Reward");
                    
                    // Gövde dönükken ateş etmesi gerektiğine dair hint ver
                    TeachCorrectAction(6); // Fire action
                    Debug.Log("HINT: Gövde oyuncuya dönük, ateş et!");
                }
        }
        else
        {
            // Dönük değilse timer'ı sıfırla
            bodyAlignedTime = 0f;
        }

        // Taret oyuncuya dönük mü?
        if (controller.turretTransform != null)
        {
            Vector3 turretForward = controller.turretTransform.forward;
            float turretAngle = Vector3.Angle(turretForward, toPlayer);
            
            if (turretAngle < 30f) // 30 derece içindeyse
            {
                turretAlignedTime += alignmentCheckInterval; // Süreyi artır
                
                // Belirli süre dönük kaldıysa ve cooldown dolmuşsa ödül ver
                if (turretAlignedTime >= alignmentRewardThreshold && 
                    Time.time - lastTurretRewardTime >= alignmentRewardCooldown)
                {
                    float rewardAmount = 2f; // Taret dönük kalma ödülü (0-10 arası ölçeklendirildi: 20 -> 2)
                    Reward(rewardAmount);
                    lastTurretRewardTime = Time.time;
                    turretAlignedTime = 0f; // Timer'ı sıfırla (tekrar ödül için)
                    Debug.Log($"{gameObject.name} tareti {alignmentRewardThreshold} saniye oyuncuya dönük kaldı! +{rewardAmount} Reward");
                    
                    // Taret dönükken ateş etmesi gerektiğine dair hint ver
                    TeachCorrectAction(6); // Fire action
                    Debug.Log("HINT: Taret oyuncuya dönük, ateş et!");
                }
            }
            else
            {
                // Dönük değilse timer'ı sıfırla
                turretAlignedTime = 0f;
            }
        }
    }

    // ===================================================================================
    // REWARD METHODS (Diğer scriptlerden çağrılacak)
    // ===================================================================================

    public void Reward(float value)
    {
        if (brain != null)
        {
            // ÖNEMLİ: Reward uygulanmadan önce güncel state'i hesapla ve güncelle
            // Bu, Q-Learning'in doğru çalışması için kritik!
            List<float> currentInputs = BuildStateInputs();
            brain.SetInputs(currentInputs);
            
            brain.Reward(value);
        }
    }

    public void Punish(float value)
    {
        if (brain != null)
        {
            // ÖNEMLİ: Punish uygulanmadan önce güncel state'i hesapla ve güncelle
            // Bu, Q-Learning'in doğru çalışması için kritik!
            List<float> currentInputs = BuildStateInputs();
            brain.SetInputs(currentInputs);
            
            brain.Punish(value);
        }
    }
    
    /// <summary>
    /// Ceza aldığında doğru action'ı öğret (reward shaping)
    /// </summary>
    public void TeachCorrectAction(int correctActionIndex, float hintReward = 1f)
    {
        if (brain != null && lastStateString != null && lastStateString != "")
        {
            // Şu anki state'te doğru action'a küçük bir reward ver
            brain.TeachActionInState(lastStateString, correctActionIndex, hintReward);
        }
    }
    
    /// <summary>
    /// Ceza aldığında doğru action'ı açı ve yön bilgisiyle öğret (reward shaping)
    /// </summary>
    public void TeachCorrectActionWithAngle(int correctActionIndex, float angle, string direction, float baseReward = 1f)
    {
        if (brain != null && lastStateString != null && lastStateString != "")
        {
            // Şu anki state'te doğru action'a açıya göre ayarlanmış reward ver
            brain.TeachActionInStateWithAngle(lastStateString, correctActionIndex, angle, direction, baseReward);
        }
    }
    
    /// <summary>
    /// Ceza aldığında doğru action'ı açı, yön ve hedef bilgisiyle öğret (reward shaping)
    /// </summary>
    public void TeachCorrectActionWithAngle(int correctActionIndex, float angle, string direction, string part, string target, float baseReward = 1f)
    {
        if (brain != null && lastStateString != null && lastStateString != "")
        {
            // Şu anki state'te doğru action'a açıya göre ayarlanmış reward ver
            brain.TeachActionInStateWithAngle(lastStateString, correctActionIndex, angle, direction, baseReward, part, target);
        }
    }

    // Özel reward metodları (0-10 arası ölçeklendirildi)
    public void AddHitReward(float value) => Reward(value); // Zaten 0-10 arası olmalı
    public void AddKillReward(float multiplier) => Reward(Mathf.Clamp(10f * multiplier, 0f, 10f)); // 300 * multiplier -> 10 * multiplier (max 10)
    public void AddDamageDealt(float damage) => Reward(Mathf.Clamp(damage * 0.5f, 0f, 5f)); // damage * 5 -> damage * 0.5 (max 5)
    public void AddPlayerDamageDealt(float damage) => Reward(Mathf.Clamp(damage * 1f, 0f, 10f)); // damage * 10 -> damage * 1 (max 10)
    public void AddEnemyDamageDealt(float damage) => Punish(Mathf.Clamp(damage * 1.5f, 0f, 10f)); // damage * 15 -> damage * 1.5 (max 10)
    public void AddPlayerDamageTaken(float damage) => Punish(Mathf.Clamp(damage * 2f, 0f, 10f)); // damage * 20 -> damage * 2 (max 10)
    public void AddEnemyKillPenalty(float multiplier) => Punish(Mathf.Clamp(10f * multiplier, 0f, 10f)); // 500 * multiplier -> 10 * multiplier (max 10)
    public void AddMineHitReward(float value) => Reward(value); // Zaten 0-10 arası olmalı
    public void AddMineHitPenalty(float value) => Punish(Mathf.Abs(value)); // Zaten 0-10 arası olmalı
    public void AddShieldBlockReward(float value) => Reward(value); // Zaten 0-10 arası olmalı
}

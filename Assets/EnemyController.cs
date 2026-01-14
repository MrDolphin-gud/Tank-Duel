using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
   
    [Header("Hareket Ayarları")]
    public float moveSpeed = 5f;
    public float bodyRotateSpeed = 100f;
    public float dashForce = 20f;
    public float dashCooldown = 2f;

    [Header("Taret Ayarları")]
    public Transform turretTransform;
    public Transform barrelTransform;
    public float turretRotateSpeed = 100f;

    [Header("Savaş Ayarları")]
    public GameObject bulletPrefab;
    public GameObject minePrefab;
    public Transform firePoint;
    public Transform minePoint;
    public float bulletSpeed = 20f;
    public float fireRate = 1f;
    
    public float mineCooldown = 2f; 

    [Header("Efekt Ayarları")]
    public float recoilDistance = 0.2f;
    public float recoilRecoverySpeed = 5f;
    public float shieldDuration = 3f;
    public float shieldCooldown = 5f;

    [Header("Ses Ayarları")]
    public AudioClip engineSound;    
    public AudioClip turretSound;    
    public AudioClip fireSound;      
    public AudioClip shieldOnSound;  
    public AudioClip shieldOffSound; 
    public AudioSource engineSource; 
    public AudioSource turretSource; 
    public AudioSource effectsSource;

    
    private Rigidbody rb;
    private Health healthScript;
    private float nextDashTime = 0;
    private float nextShieldTime = 0;
    private float nextFireTime = 0;
    private float nextMineTime = 0; 
    private Vector3 originalBarrelPos;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        healthScript = GetComponent<Health>();

        if (barrelTransform != null) originalBarrelPos = barrelTransform.localPosition;

        if (engineSource != null && engineSound != null)
        {
            engineSource.clip = engineSound;
            engineSource.loop = true; 
        }

        if (turretSource != null && turretSound != null)
        {
            turretSource.clip = turretSound;
            turretSource.loop = true;
        }
    }

    

    public void Move(float moveInput, float rotateInput)
    {
        transform.Translate(Vector3.forward * moveInput * moveSpeed * Time.deltaTime);
        if (moveInput < -0.1f) rotateInput = -rotateInput;
        transform.Rotate(Vector3.up * rotateInput * bodyRotateSpeed * Time.deltaTime);
    }

    public void RotateTurretManual(float input)
    {
        if (turretTransform != null)
        {
            turretTransform.Rotate(Vector3.up * input * turretRotateSpeed * Time.deltaTime);
        }
    }

    public GameObject FireManual()
    {
        return FireManual(false);
    }
    
    public GameObject FireManual(bool aggressiveMode)
    {
        float currentFireRate = aggressiveMode ? 0.3f : fireRate;
        
        if (Time.time < nextFireTime) return null;

        if (bulletPrefab && firePoint)
        {
            if (effectsSource && fireSound) effectsSource.PlayOneShot(fireSound);
            
            // Mermi yönünü taretin yönüne göre ayarla
            // turretTransform varsa onun yönünü kullan, yoksa firePoint'in yönünü kullan
            Vector3 fireDirection;
            Quaternion fireRotation;
            
            if (turretTransform != null)
            {
                // Taretin yönünü kullan 
                fireDirection = turretTransform.forward;
                fireRotation = turretTransform.rotation;
            }
            else
            {
                // Fallback: firePoint'in yönünü kullan
                fireDirection = firePoint.forward;
                fireRotation = firePoint.rotation;
            }
            
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, fireRotation);
            Physics.IgnoreCollision(GetComponent<Collider>(), bullet.GetComponent<Collider>());
            bullet.GetComponent<Rigidbody>().linearVelocity = fireDirection * bulletSpeed;
            
            nextFireTime = Time.time + currentFireRate;
            Destroy(bullet, 3f);

            if (barrelTransform != null) { StopCoroutine("RecoilAnimation"); StartCoroutine("RecoilAnimation"); }
            return bullet;
        }
        return null;
    }

    public void DashManual()
    {
        if (Time.time < nextDashTime) return;
        
        // Rigidbody null kontrolü 
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null) return; 
        }
        
        rb.AddForce(transform.forward * dashForce, ForceMode.Impulse);
        nextDashTime = Time.time + dashCooldown;
    }

    public void PlaceMineManual()
    {
        // Kontrol 1: Cooldown süresi doldu mu?
        if (Time.time < nextMineTime) return;

        // Kontrol 2: Prefab ve Nokta atanmış mı? (Hata almamak için)
        if (minePrefab != null && minePoint != null)
        {
            GameObject newMine = Instantiate(minePrefab, minePoint.position, Quaternion.identity);
            
            // Mayın scriptini kontrol et
            Mine mineScript = newMine.GetComponent<Mine>();
            if (mineScript != null)
            {
                mineScript.ownerTag = gameObject.tag;
                
                // Mayını bırakan agent'ı kaydet (ödül/ceza için)
                QLearningAgent qlAgent = GetComponent<QLearningAgent>();
                if (qlAgent != null)
                {
                    mineScript.ownerQLearningAgent = qlAgent;
                }
            }
            
            // Çarpışmaları yoksay
            Collider[] tCols = GetComponentsInChildren<Collider>();
            Collider[] mCols = newMine.GetComponentsInChildren<Collider>();
            //foreach (var t in tCols) foreach (var m in mCols) Physics.IgnoreCollision(t, m);

            // Cooldown'ı başlat
            nextMineTime = Time.time + mineCooldown;
            
            // Test için Konsola yazı yazdıralım 
            // Debug.Log(gameObject.name + " mayın bıraktı!");
        }
    }

    public void ActivateShieldManual()
    {
        if (Time.time > nextShieldTime) StartCoroutine(ShieldRoutine());
    }

    private IEnumerator ShieldRoutine()
    {
        if (healthScript != null) healthScript.isShielded = true;
        if (effectsSource && shieldOnSound) effectsSource.PlayOneShot(shieldOnSound);
        GetComponent<Renderer>().material.color = Color.blue;

        yield return new WaitForSeconds(shieldDuration);

        if (healthScript != null) healthScript.isShielded = false;
        if (effectsSource && shieldOffSound) effectsSource.PlayOneShot(shieldOffSound);
        GetComponent<Renderer>().material.color = Color.red; 
        nextShieldTime = Time.time + shieldCooldown;
    }

    IEnumerator RecoilAnimation()
    {
        Vector3 recoilPos = originalBarrelPos - (Vector3.forward * recoilDistance);
        barrelTransform.localPosition = recoilPos;
        while (Vector3.Distance(barrelTransform.localPosition, originalBarrelPos) > 0.01f)
        {
            barrelTransform.localPosition = Vector3.Lerp(barrelTransform.localPosition, originalBarrelPos, Time.deltaTime * recoilRecoverySpeed);
            yield return null; 
        }
        barrelTransform.localPosition = originalBarrelPos;
    }

    public bool IsDashReady() => Time.time > nextDashTime;
    public bool IsShieldReady() => Time.time > nextShieldTime;
    public bool IsMineReady() => Time.time > nextMineTime;
    

    void OnCollisionEnter(Collision collision)
    {
        // Duvar ile çarpışma kontrolü 
        // "Wall" tag'li objelerle çarpışma cezası
        if (collision.gameObject.CompareTag("Wall"))
        {
            // Çarpışma normal vektörünü kontrol et - zemin mi duvar mı?
            Vector3 collisionNormal = collision.contacts[0].normal;
            
            // Eğer normal yukarı bakıyorsa (y > 0.7) bu zemindir, ceza verme
            if (Mathf.Abs(collisionNormal.y) > 0.7f)
            {
                return; // Zemin çarpışması - ceza verme
            }
            
            QLearningAgent agent = GetComponent<QLearningAgent>();
            if (agent != null)
            {
                // Küçük ceza ver (0-10 arası ölçeklendirildi)
                agent.Punish(2f);
                
                // Çarpışma yönünü hesapla ve hint ver
                Vector3 collisionPoint = collision.contacts[0].point;
                Vector3 toCollision = (collisionPoint - transform.position).normalized;
                
                // Tankın hangi tarafıyla çarptığını belirle
                float dotForward = Vector3.Dot(transform.forward, toCollision);
                float dotRight = Vector3.Dot(transform.right, toCollision);
                
                // Oyuncuyu bul ve oyuncuya dönüş açısını hesapla
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                
                if (Mathf.Abs(dotForward) > Mathf.Abs(dotRight))
                {
                    // Ön veya arka ile çarptı
                    if (dotForward > 0)
                    {
                        // Önden çarptı - geri git hint'i
                        agent.TeachCorrectAction(1, 1f); // MoveBackward
                    }
                    else
                    {
                        // Arkadan çarptı - ileri git hint'i
                        agent.TeachCorrectAction(0, 1f); // MoveForward
                    }
                }
                else
                {
                    // Sağ veya sol ile çarptı - oyuncuya doğru dönmesi için hint ver
                    if (player != null)
                    {
                        Vector3 toPlayer = (player.transform.position - transform.position).normalized;
                        Vector3 cross = Vector3.Cross(transform.forward, toPlayer);
                        float angleToPlayer = Vector3.Angle(transform.forward, toPlayer);
                        
                        if (cross.y > 0)
                        {
                            // Oyuncu sağda - sağa dön hint'i
                            agent.TeachCorrectActionWithAngle(3, angleToPlayer, "sağa", "Gövde", "oyuncuya", 1f);
                        }
                        else
                        {
                            // Oyuncu solda - sola dön hint'i
                            agent.TeachCorrectActionWithAngle(2, angleToPlayer, "sola", "Gövde", "oyuncuya", 1f);
                        }
                    }
                }
                
                Debug.Log($"{gameObject.name} duvara çarptı! -2 Ceza");
            }
        }
    }
}
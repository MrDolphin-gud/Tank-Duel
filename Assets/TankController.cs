using UnityEngine;
using System.Collections;

public class TankController : MonoBehaviour
{
    // Tankın AI kontrolünde mi yoksa oyuncu kontrolünde mi olduğunu belirler
    public bool isAI = false;

    // Tankın ileri geri hareket hızını belirler
    public float moveSpeed = 5f;
    // Tank gövdesinin dönüş hızını belirler
    public float bodyRotateSpeed = 100f;
    // Dash yaparken uygulanacak kuvvet miktarını belirler
    public float dashForce = 20f;
    // İki dash arasında geçmesi gereken süreyi belirler
    public float dashCooldown = 2f;

    // Taret dönüşünü kontrol eden transform bileşeni
    public Transform turretTransform;
    // Namlu geri tepme animasyonu için kullanılan transform bileşeni
    public Transform barrelTransform;
    // Taretin dönüş hızını belirler
    public float turretRotateSpeed = 100f;

    // Ateş edildiğinde oluşturulacak mermi prefab'ı
    public GameObject bulletPrefab;
    // Yerleştirilecek mayın prefab'ı
    public GameObject minePrefab;
    // Merminin çıkış noktasını belirler
    public Transform firePoint;
    // Mayının yerleştirileceği noktayı belirler
    public Transform minePoint;
    // Merminin uçuş hızını belirler
    public float bulletSpeed = 20f;

    // Namlunun geri tepme mesafesini belirler
    public float recoilDistance = 0.2f;
    // Namlunun geri tepme sonrası eski haline dönme hızını belirler
    public float recoilRecoverySpeed = 5f;

    // Kalkanın aktif kalacağı süreyi belirler
    public float shieldDuration = 3f;
    // İki kalkan aktivasyonu arasında geçmesi gereken süreyi belirler
    public float shieldCooldown = 5f;

    // Motor çalışırken çalacak ses dosyası
    public AudioClip engineSound;    
    // Taret dönerken çalacak ses dosyası
    public AudioClip turretSound;    
    // Ateş edildiğinde çalacak ses dosyası
    public AudioClip fireSound;      
    // Kalkan aktif edildiğinde çalacak ses dosyası
    public AudioClip shieldOnSound;  
    // Kalkan kapanırken çalacak ses dosyası
    public AudioClip shieldOffSound; 

    // Motor sesini çalacak audio source bileşeni
    public AudioSource engineSource; 
    // Taret sesini çalacak audio source bileşeni
    public AudioSource turretSource; 
    // Diğer efekt seslerini çalacak audio source bileşeni
    public AudioSource effectsSource;

    // Tankın fiziksel hareketini kontrol eden rigidbody bileşeni
    private Rigidbody rb;
    // Tankın can sistemini kontrol eden health scripti
    private Health healthScript;
    // Bir sonraki dash yapılabilecek zamanı tutar
    private float nextDashTime = 0;
    // Bir sonraki kalkan aktif edilebilecek zamanı tutar
    private float nextShieldTime = 0;
    // Namlunun başlangıç pozisyonunu saklar
    private Vector3 originalBarrelPos;

    void Start()
    {
        // Rigidbody bileşenini alır ve saklar
        rb = GetComponent<Rigidbody>();
        // Health scriptini alır ve saklar
        healthScript = GetComponent<Health>();

        // Namlunun başlangıç pozisyonunu kaydeder
        if (barrelTransform != null)
        {
            originalBarrelPos = barrelTransform.localPosition;
        }

        // Motor sesini ayarlar ve döngüye alır
        if (engineSource != null && engineSound != null)
        {
            engineSource.clip = engineSound;
            engineSource.loop = true; 
        }

        // Taret sesini ayarlar ve döngüye alır
        if (turretSource != null && turretSound != null)
        {
            turretSource.clip = turretSound;
            turretSource.loop = true;
        }
    }

    void Update()
    {
        // AI kontrolündeyse oyuncu girdilerini işlemez
        if (isAI)
        {
            return;
        } 

        // Dikey eksen girdisini alır (W/S tuşları)
        float moveInput = Input.GetAxis("Vertical");
        // Tankı ileri veya geri hareket ettirir
        transform.Translate(Vector3.forward * moveInput * moveSpeed * Time.deltaTime);

        // Motor sesini hareket durumuna göre kontrol eder
        if (engineSource != null)
        {
            // Tank hareket ediyorsa motor sesini çalar ve hıza göre pitch ayarlar
            if (Mathf.Abs(moveInput) > 0.01f)
            {
                if (!engineSource.isPlaying)
                {
                    engineSource.Play();
                }
                engineSource.pitch = 1.0f + (Mathf.Abs(moveInput) * 0.2f); 
            }
            // Tank duruyorsa motor sesini durdurur
            else
            {
                if (engineSource.isPlaying)
                {
                    engineSource.Stop();
                }
            }
        }


        // Yatay eksen girdisini alır (A/D tuşları)
        float rotateInput = Input.GetAxis("Horizontal");

        // Tank geriye gidiyorsa dönüş yönünü tersine çevirir
        if (moveInput < -0.1f)
        {
            rotateInput = -rotateInput;
        }

        // Tank gövdesini döndürür
        transform.Rotate(Vector3.up * rotateInput * bodyRotateSpeed * Time.deltaTime);


        // Taret kontrolünü yapar
        if (turretTransform != null)
        {
            float turretInput = 0f;
            // E tuşuna basılırsa taret sağa döner
            if (Input.GetKey(KeyCode.E))
            {
                turretInput = 1f;
            }
            // Q tuşuna basılırsa taret sola döner
            if (Input.GetKey(KeyCode.Q))
            {
                turretInput = -1f;
            }
            
            // Tareti döndürür
            turretTransform.Rotate(Vector3.up * turretInput * turretRotateSpeed * Time.deltaTime);

            // Taret dönerken taret sesini çalar, durunca durdurur
            if (turretSource != null)
            {
                if (turretInput != 0 && !turretSource.isPlaying)
                {
                    turretSource.Play();
                }
                else if (turretInput == 0 && turretSource.isPlaying)
                {
                    turretSource.Stop();
                }
            }
        }


        // Boşluk tuşuna basılırsa ateş eder
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Fire();
        }
        // Shift tuşuna basılırsa ve bekleme süresi dolmuşsa dash yapar
        if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time > nextDashTime)
        {
            Dash();
        }
        // F tuşuna basılırsa mayın yerleştirir
        if (Input.GetKeyDown(KeyCode.F))
        {
            PlaceMine();
        }
        // R tuşuna basılırsa ve bekleme süresi dolmuşsa kalkanı aktif eder
        if (Input.GetKeyDown(KeyCode.R) && Time.time > nextShieldTime)
        {
            StartCoroutine(ActivateShield());
        }
    }

    void Fire()
    {
        // Mermi prefab'ı ve ateş noktası varsa ateş eder
        if (bulletPrefab && firePoint)
        {
            // Ateş sesini çalar
            if (effectsSource && fireSound)
            {
                effectsSource.PlayOneShot(fireSound);
            }

            // Mermiyi ateş noktasında oluşturur
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            // Merminin tankla çarpışmasını engeller
            Physics.IgnoreCollision(GetComponent<Collider>(), bullet.GetComponent<Collider>());
            // Mermiyi ileri doğru fırlatır
            bullet.GetComponent<Rigidbody>().linearVelocity = firePoint.forward * bulletSpeed;
            // Mermiyi 3 saniye sonra yok eder
            Destroy(bullet, 3f);

            // Namlu geri tepme animasyonunu başlatır
            if (barrelTransform != null)
            {
                StopCoroutine("RecoilAnimation");
                StartCoroutine("RecoilAnimation");
            }
        }
    }

    void Dash()
    {
        // Tankı ileri doğru ani bir kuvvetle fırlatır
        rb.AddForce(transform.forward * dashForce, ForceMode.Impulse);
        
        // Dash sırasında motor sesini yüksek pitch ile çalar
        if(engineSource) 
        {
             if(!engineSource.isPlaying)
             {
                 engineSource.Play();
             }
             engineSource.pitch = 1.8f; 
        }

        // Bir sonraki dash için bekleme süresini ayarlar
        nextDashTime = Time.time + dashCooldown; 
    }

    void PlaceMine()
    {
        // Mayın prefab'ı ve yerleştirme noktası varsa mayını yerleştirir
        if (minePrefab && minePoint)
        {
            // Mayını oluşturur
            GameObject newMine = Instantiate(minePrefab, minePoint.position, Quaternion.identity);
            
            // Mayına sahibini bildirir
            Mine mineScript = newMine.GetComponent<Mine>();
            if (mineScript != null)
            {
                mineScript.ownerTag = gameObject.tag;
            }

            // Tankın üzerindeki ve altındaki tüm Collider'ları alır
            Collider[] tankColliders = GetComponentsInChildren<Collider>();
            
            // Mayının üzerindeki ve altındaki tüm Collider'ları alır
            Collider[] mineColliders = newMine.GetComponentsInChildren<Collider>();
            
            // Tankın her bir parçasını mayının her bir parçasıyla çarpışmayı yoksayır
            foreach (Collider tCol in tankColliders)
            {
                foreach (Collider mCol in mineColliders)
                {
                    Physics.IgnoreCollision(tCol, mCol);
                }
            }
        }
    }

    IEnumerator ActivateShield()
    {
        // Kalkanı aktif eder ve hasar almayı engeller
        if(healthScript != null)
        {
            healthScript.isShielded = true;
        }
        // Kalkan açılma sesini çalar
        if (effectsSource && shieldOnSound)
        {
            effectsSource.PlayOneShot(shieldOnSound);
        }
        
        // Tankı mavi renge boyar
        GetComponent<Renderer>().material.color = Color.blue;

        // Kalkan süresince bekler
        yield return new WaitForSeconds(shieldDuration); 

        // Kalkanı kapatır ve hasar almayı tekrar aktif eder
        if(healthScript != null)
        {
            healthScript.isShielded = false;
        }
        // Kalkan kapanma sesini çalar
        if (effectsSource && shieldOffSound)
        {
            effectsSource.PlayOneShot(shieldOffSound);
        }

        // Tankı orijinal rengine döndürür
        if (isAI)
        {
            GetComponent<Renderer>().material.color = Color.red;
        }
        else
        {
            GetComponent<Renderer>().material.color = Color.darkGreen;
        } 
        // Bir sonraki kalkan için bekleme süresini ayarlar
        nextShieldTime = Time.time + shieldCooldown;
    }
    
    IEnumerator RecoilAnimation()
    {
        // Namlunun geri tepme pozisyonunu hesaplar
        Vector3 recoilPos = originalBarrelPos - (Vector3.up * recoilDistance);
        // Namluyu geri tepme pozisyonuna alır
        barrelTransform.localPosition = recoilPos;
        // Namlu orijinal pozisyonuna dönene kadar bekler
        while (Vector3.Distance(barrelTransform.localPosition, originalBarrelPos) > 0.01f)
        {
            // Namluyu yumuşak bir şekilde orijinal pozisyonuna döndürür
            barrelTransform.localPosition = Vector3.Lerp(barrelTransform.localPosition, originalBarrelPos, Time.deltaTime * recoilRecoverySpeed);
            yield return null; 
        }
        // Namluyu tam olarak orijinal pozisyonuna yerleştirir
        barrelTransform.localPosition = originalBarrelPos;
    }
}
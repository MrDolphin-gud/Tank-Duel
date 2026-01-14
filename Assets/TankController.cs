using UnityEngine;
using System.Collections;

public class TankController : MonoBehaviour
{
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

    // Mayın bekleme süresini belirler
    public float mineCooldown = 5f;

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
    // Bir sonraki mayın yerleştirilebilecek zamanı tutar
    private float nextMineTime = 0;
    // Namlunun başlangıç pozisyonunu saklar
    private Vector3 originalBarrelPos;

    void Start()
    {
        // Rigidbody bileşenini alır 
        rb = GetComponent<Rigidbody>();
        // Health scriptini alır 
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
        // Dikey eksen girdisini alır 
        float moveInput = Input.GetAxis("Vertical");
        // Tankı ileri veya geri hareket ettirir
        transform.Translate(Vector3.forward * moveInput * moveSpeed * Time.deltaTime);

        // Motor sesini hareket durumuna göre kontrol eder
        if (engineSource != null)
        {
            if (Mathf.Abs(moveInput) > 0.01f)
            {
                if (!engineSource.isPlaying) engineSource.Play();
                engineSource.pitch = 1.0f + (Mathf.Abs(moveInput) * 0.2f); 
            }
            else
            {
                if (engineSource.isPlaying) engineSource.Stop();
            }
        }

        // Yatay eksen girdisini alır 
        float rotateInput = Input.GetAxis("Horizontal");

        // Tank geriye gidiyorsa dönüş yönünü tersine çevirir
        if (moveInput < -0.1f) rotateInput = -rotateInput;

        // Tank gövdesini döndürür
        transform.Rotate(Vector3.up * rotateInput * bodyRotateSpeed * Time.deltaTime);

        // Taret kontrolünü yapar
        if (turretTransform != null)
        {
            float turretInput = 0f;
            if (Input.GetKey(KeyCode.E)) turretInput = 1f;
            if (Input.GetKey(KeyCode.Q)) turretInput = -1f;
            
            turretTransform.Rotate(Vector3.up * turretInput * turretRotateSpeed * Time.deltaTime);

            if (turretSource != null)
            {
                if (turretInput != 0 && !turretSource.isPlaying) turretSource.Play();
                else if (turretInput == 0 && turretSource.isPlaying) turretSource.Stop();
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
        // F tuşuna basılırsa ve bekleme süresi dolmuşsa mayın yerleştirir
        if (Input.GetKeyDown(KeyCode.F) && Time.time > nextMineTime)
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
        if (bulletPrefab && firePoint)
        {
            if (effectsSource && fireSound) effectsSource.PlayOneShot(fireSound);

            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            Physics.IgnoreCollision(GetComponent<Collider>(), bullet.GetComponent<Collider>());
            bullet.GetComponent<Rigidbody>().linearVelocity = firePoint.forward * bulletSpeed;
            
            // Oyuncu mermisi için Projectile'a referans ekle (oyuncudan gelen hasarı takip etmek için)
            Projectile proj = bullet.GetComponent<Projectile>();
            if (proj != null && gameObject.CompareTag("Player"))
            {
                // Oyuncu mermisi için shooterAgent null kalacak ama damageSource'u oyuncu olarak ayarlayacağız
                proj.shooterGameObject = gameObject; // Yeni field ekleyeceğiz
            }
            
            Destroy(bullet, 3f);

            if (barrelTransform != null)
            {
                StopCoroutine("RecoilAnimation");
                StartCoroutine("RecoilAnimation");
            }
        }
    }

    void Dash()
    {
        rb.AddForce(transform.forward * dashForce, ForceMode.Impulse);
        if(engineSource) 
        {
             if(!engineSource.isPlaying) engineSource.Play();
             engineSource.pitch = 1.8f; 
        }
        nextDashTime = Time.time + dashCooldown; 
    }

    void PlaceMine()
    {
        if (minePrefab && minePoint)
        {
            GameObject newMine = Instantiate(minePrefab, minePoint.position, Quaternion.identity);
            
            Mine mineScript = newMine.GetComponent<Mine>();
            if (mineScript != null) mineScript.ownerTag = gameObject.tag;

            Collider[] tankColliders = GetComponentsInChildren<Collider>();
            Collider[] mineColliders = newMine.GetComponentsInChildren<Collider>();
            
            /*foreach (Collider tCol in tankColliders)
            {
                foreach (Collider mCol in mineColliders)
                {
                    Physics.IgnoreCollision(tCol, mCol);
                }
            }*/

            // Mayın yerleştirildikten sonra bekleme süresini başlat
            nextMineTime = Time.time + mineCooldown;
        }
    }

    IEnumerator ActivateShield()
    {
        if(healthScript != null) healthScript.isShielded = true;
        if (effectsSource && shieldOnSound) effectsSource.PlayOneShot(shieldOnSound);
        
        GetComponent<Renderer>().material.color = Color.blue;
        yield return new WaitForSeconds(shieldDuration); 

        if(healthScript != null) healthScript.isShielded = false;
        if (effectsSource && shieldOffSound) effectsSource.PlayOneShot(shieldOffSound);

        GetComponent<Renderer>().material.color = Color.darkGreen;
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
}
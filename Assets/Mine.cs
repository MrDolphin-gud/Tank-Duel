using UnityEngine;

public class Mine : MonoBehaviour
{
    // Mayının vereceği hasar miktarını belirler
    public float damage = 30f;
    // Mayın patladığında çalacak ses dosyası
    public AudioClip explosionSound;
    
    
    // Mayını yerleştiren tankın tag'ini tutar
    public string ownerTag;

    void OnTriggerEnter(Collider other)
    {
        // Sahibine çarptıysa patlamaz
        if (other.CompareTag(ownerTag))
        {
            return;
        }

        // Çarptığı objede Health scripti var mı kontrol eder
        Health targetHealth = other.GetComponent<Health>();

        // Health scripti varsa hasar verir
        if (targetHealth != null)
        {
            // Hedefe hasar verir
            targetHealth.TakeDamage(damage);
            
            // Patlama sesini olay yerinde çalar
            if (explosionSound != null)
            {
                AudioSource.PlayClipAtPoint(explosionSound, transform.position, 1.0f);
            }

            // Mayını yok eder
            Destroy(gameObject); 
        }
    }
}
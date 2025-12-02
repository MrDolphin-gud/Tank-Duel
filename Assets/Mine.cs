using UnityEngine;

public class Mine : MonoBehaviour
{
    public float damage = 30f;       // Mayının vereceği hasar
    public AudioClip explosionSound; // Patlama sesi dosyası

    void OnTriggerEnter(Collider other)
    {
        Health targetHealth = other.GetComponent<Health>();

        if (targetHealth != null)
        {
            // Hedefe hasar ver
            targetHealth.TakeDamage(damage);
            
            // Patlama sesini 3D olarak olay yerinde çal
            // Bu fonksiyon geçici bir obje oluşturur sesi bitene kadar çalar ve yok olur. 
            if (explosionSound != null)
            {
                AudioSource.PlayClipAtPoint(explosionSound, transform.position, 1.0f);
            }

            // Mayını sahneden sil
            Destroy(gameObject); 
        }
    }
}
using UnityEngine;

public class Projectile : MonoBehaviour
{
    // Merminin vereceği hasar miktarını belirler
    public float damage = 10f;

    // Mermi katı bir cisme çarptığında bu fonksiyon otomatik çalışır
    void OnCollisionEnter(Collision collision)
    {
        // Çarptığı objede Health scripti var mı kontrol eder
        Health targetHealth = collision.gameObject.GetComponent<Health>();

        // Health scripti varsa hasar verir
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage);
        }

        // Mermiyi yok eder
        Destroy(gameObject);
    }
}
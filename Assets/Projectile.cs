using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float damage = 10f; // Merminin vereceği hasar

    // Mermi katı bir cisme çarptığında bu fonksiyon otomatik çalışır
    void OnCollisionEnter(Collision collision)
    {
        // Çarptığımız objede "Health" scripti var mı diye bakıyoruz
        Health targetHealth = collision.gameObject.GetComponent<Health>();

        // Eğer varsa canını azaltıyoruz
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage);
        }

        // mermi yok olmalı
        Destroy(gameObject);
    }
}
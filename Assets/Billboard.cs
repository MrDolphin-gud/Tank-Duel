using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform camTransform;

    void Start()
    {
        // Ana kamerayı bul
        camTransform = Camera.main.transform;
    }

    // LateUpdate, tüm hareketler bittikten sonra çalışır 
    void LateUpdate()
    {
        // UI objesini kameranın baktığı yöne çevir
        transform.LookAt(transform.position + camTransform.forward);
    }
}
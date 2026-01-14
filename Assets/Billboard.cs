using UnityEngine;

public class Billboard : MonoBehaviour
{
    // Ana kameranın transform bileşenini tutar
    private Transform camTransform;

    void Start()
    {
        // Ana kameranın transform bileşenini alır
        camTransform = Camera.main.transform;
    }

    // tüm hareketler bittikten sonra çalışır
    void LateUpdate()
    {
        // Objeyi kameranın baktığı yöne çevirir
        transform.LookAt(transform.position + camTransform.forward);
    }
}
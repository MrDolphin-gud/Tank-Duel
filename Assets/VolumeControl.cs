using UnityEngine;

public class VolumeControl : MonoBehaviour
{
    // Ses tipini belirleyen enum
    public enum AudioType { Music, SFX }
    // Bu objenin müzik mi yoksa efekt sesi mi olduğunu belirler
    public AudioType audioType;
    // Ses çalacak audio source bileşeni
    private AudioSource audioSource;

    void Start()
    {
        // Audio source bileşenini alır
        audioSource = GetComponent<AudioSource>();

        // Audio source varsa ses seviyesini ayarlar
        if (audioSource != null)
        {
            float savedVolume = 1f;

            // Müzik sesi ise müzik ses seviyesini okur
            if (audioType == AudioType.Music)
            {
                savedVolume = PlayerPrefs.GetFloat("MusicVol", 1f);
            }
            // Efekt sesi ise efekt ses seviyesini okur
            else
            {
                savedVolume = PlayerPrefs.GetFloat("SFXVol", 1f);
            }

            // Ses seviyesini ayarlar
            audioSource.volume = savedVolume;
        }
    }
}
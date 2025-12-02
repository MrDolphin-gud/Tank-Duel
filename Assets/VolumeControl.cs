using UnityEngine;

public class VolumeControl : MonoBehaviour
{
    public enum AudioType { Music, SFX }
    public AudioType audioType;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            // Hafızadan kaydedilmiş değeri oku
            float savedVolume = 1f;

            if (audioType == AudioType.Music)
            {
                savedVolume = PlayerPrefs.GetFloat("MusicVol", 1f);
            }
            else // SFX
            {
                savedVolume = PlayerPrefs.GetFloat("SFXVol", 1f);
            }

            // Sesi ayarla
            audioSource.volume = savedVolume;
        }
    }
}
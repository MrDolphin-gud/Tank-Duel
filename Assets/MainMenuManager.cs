using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    // Müzik ses seviyesini kontrol eden slider bileşeni
    public Slider musicSlider;
    // Efekt ses seviyesini kontrol eden slider bileşeni
    public Slider sfxSlider;

    void Start()
    {
        // Menü açıldığında kaydedilmiş müzik ses ayarını geri yükler
        if (musicSlider != null)
        {
            musicSlider.value = PlayerPrefs.GetFloat("MusicVol", 1f);
        }

        // Menü açıldığında kaydedilmiş efekt ses ayarını geri yükler
        if (sfxSlider != null)
        {
            sfxSlider.value = PlayerPrefs.GetFloat("SFXVol", 1f);
        }
    }

    public void StartGame()
    {
        // Oyun sahnesine geçiş yapar
        SceneManager.LoadScene("GameScene");
    }

    public void SetMusicVolume(float volume)
    {
        // Müzik ses seviyesini kaydeder
        PlayerPrefs.SetFloat("MusicVol", volume);
        // Ayarları kalıcı olarak kaydeder
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        // Efekt ses seviyesini kaydeder
        PlayerPrefs.SetFloat("SFXVol", volume);
        // Ayarları kalıcı olarak kaydeder
        PlayerPrefs.Save();
    }

    public void QuitGame()
    {
        // Oyunu kapatır
        Application.Quit();
        // Konsola çıkış mesajı yazar
        Debug.Log("Oyundan Çıkıldı");
    }
}
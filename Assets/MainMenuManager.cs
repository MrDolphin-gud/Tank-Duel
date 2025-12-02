using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Slider Referansları")]
    public Slider musicSlider;
    public Slider sfxSlider;

    void Start()
    {
        // Menü açıldığında, daha önce kaydedilmiş ses ayarlarını geri yükle
        // Eğer kayıt yoksa varsayılan olarak Tam ses yap.
        if (musicSlider != null) 
            musicSlider.value = PlayerPrefs.GetFloat("MusicVol", 1f);

        if (sfxSlider != null) 
            sfxSlider.value = PlayerPrefs.GetFloat("SFXVol", 1f);
    }

    public void StartGame()
    {
        SceneManager.LoadScene("GameScene");//Oyunu Başlat
    }

    public void SetMusicVolume(float volume)
    {
        // Değeri kaydet
        PlayerPrefs.SetFloat("MusicVol", volume);
        PlayerPrefs.Save(); // Garantilemek için kaydet
    }

    public void SetSFXVolume(float volume)
    {
        // Değeri kaydet
        PlayerPrefs.SetFloat("SFXVol", volume);
        PlayerPrefs.Save();
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Oyundan Çıkıldı");
    }
}
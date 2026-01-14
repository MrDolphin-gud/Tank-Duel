using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;

public class MainMenuManager : MonoBehaviour
{
    // Müzik ses seviyesini kontrol eden slider bileşeni
    public Slider musicSlider;
    // Efekt ses seviyesini kontrol eden slider bileşeni
    public Slider sfxSlider;
    
    // Yapay Zeka yükleme durumunu gösteren UI elementi 
    public Text aiLoadStatusText;

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
        
        // Yapay Zeka yükleme durumunu göster
        UpdateAILoadStatus();
    }
    
    void UpdateAILoadStatus()
    {
        if (aiLoadStatusText != null)
        {
            bool aiLoaded = PlayerPrefs.GetInt("LoadAI", 1) == 1;
            
            if (aiLoaded)
            {
                // StreamingAssets veya Assets klasöründeki modeli kontrol et
                string streamingAssetsPath = Path.Combine(Application.streamingAssetsPath, "RLBrain.json");
                bool hasModel = File.Exists(streamingAssetsPath);
                
                #if UNITY_EDITOR
                if (!hasModel)
                {
                    // Editor'da Assets klasörünü de kontrol et
                    string assetsModelPath = Path.Combine(Application.dataPath, "RLBrain.json");
                    hasModel = File.Exists(assetsModelPath);
                }
                #endif
                
                if (hasModel)
                {
                    aiLoadStatusText.text = "Hazir AI Model Yuklenecek";
                    aiLoadStatusText.color = Color.green;
                }
                else
                {
                    aiLoadStatusText.text = "Yeni AI Model Olusturulacak";
                    aiLoadStatusText.color = Color.yellow;
                }
            }
            else
            {
                aiLoadStatusText.text = "Yapay Zeka Yuklenmeyecek";
                aiLoadStatusText.color = Color.yellow;
            }
        }
    }

    public void StartGame()
    {
        // Oyun sahnesine geçiş yapar
        SceneManager.LoadScene("GameScene");
    }

    public void ToggleLoadAI()
    {
        int currentValue = PlayerPrefs.GetInt("LoadAI", 1);
        
        if (currentValue == 0)
        {
            // Kapalıdan açığa geçiş
            PlayerPrefs.SetInt("LoadAI", 1);
            PlayerPrefs.Save();
            Debug.Log("Yapay Zeka yukleme acildi.");
        }
        else
        {
            // Açıktan kapalıya geçiş
            PlayerPrefs.SetInt("LoadAI", 0);
            PlayerPrefs.Save();
            Debug.Log("Yapay Zeka yukleme kapatildi.");
        }
        
        UpdateAILoadStatus();
    }

    public void EnableLoadAI()
    {
        PlayerPrefs.SetInt("LoadAI", 1);
        PlayerPrefs.Save();
        UpdateAILoadStatus();
    }

    public void DisableLoadAI()
    {
        PlayerPrefs.SetInt("LoadAI", 0);
        PlayerPrefs.Save();
        UpdateAILoadStatus();
    }
    
    public void SetMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat("MusicVol", volume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        PlayerPrefs.SetFloat("SFXVol", volume);
        PlayerPrefs.Save();
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Oyundan Cikildi");
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
            bool aiLoaded = PlayerPrefs.GetInt("LoadAI", 0) == 1;
            string customPath = PlayerPrefs.GetString("CustomAIModelPath", "");
            
            if (aiLoaded)
            {
                if (!string.IsNullOrEmpty(customPath) && File.Exists(customPath))
                {
                    aiLoadStatusText.text = "Ozel Model: " + Path.GetFileName(customPath);
                    aiLoadStatusText.color = Color.cyan;
                }
                else
                {
                    aiLoadStatusText.text = "Varsayilan Model Yuklenecek";
                    aiLoadStatusText.color = Color.green;
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
        int currentValue = PlayerPrefs.GetInt("LoadAI", 0);
        
        if (currentValue == 0)
        {
            // Kapalıdan açığa geçiş - dosya gezgini aç
            #if UNITY_EDITOR
            string path = EditorUtility.OpenFilePanel("RLBrain.json Sec", "", "json");
            if (!string.IsNullOrEmpty(path))
            {
                PlayerPrefs.SetString("CustomAIModelPath", path);
                PlayerPrefs.SetInt("LoadAI", 1);
                PlayerPrefs.Save();
                Debug.Log("Model secildi: " + path);
            }
            // Dosya seçilmezse toggle yapma
            #endif
        }
        else
        {
            // Açıktan kapalıya geçiş - sadece kapat
            PlayerPrefs.SetInt("LoadAI", 0);
            PlayerPrefs.SetString("CustomAIModelPath", "");
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

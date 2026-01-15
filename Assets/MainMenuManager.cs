using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using UnityEngine.Networking;
using System.Collections;

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
        
        // Yapay Zeka yükleme durumunu göster (WebGL için coroutine)
        #if UNITY_WEBGL && !UNITY_EDITOR
        StartCoroutine(UpdateAILoadStatusCoroutine());
        #else
        UpdateAILoadStatus();
        #endif
    }
    
    void UpdateAILoadStatus()
    {
        if (aiLoadStatusText != null)
        {
            bool aiLoaded = PlayerPrefs.GetInt("LoadAI", 1) == 1;
            
            if (aiLoaded)
            {
                bool hasModel = false;
                string foundPath = "";
                
                #if UNITY_EDITOR
                // Editor'da önce StreamingAssets'i kontrol et (öncelikli)
                // Sonra Assets klasörünü kontrol et
                string[] pathsToCheck = new string[]
                {
                    Path.Combine(Application.dataPath, "StreamingAssets", "RLBrain.json"), // Öncelik 1: StreamingAssets
                    Path.Combine(Application.streamingAssetsPath, "RLBrain.json"), // Öncelik 2: StreamingAssets 
                    Path.Combine(Application.dataPath, "RLBrain.json") // Öncelik 3: Assets 
                };
                
                Debug.Log($"[MainMenu] Model arama basladi. Application.dataPath: {Application.dataPath}");
                Debug.Log($"[MainMenu] Application.streamingAssetsPath: {Application.streamingAssetsPath}");
                
                foreach (string path in pathsToCheck)
                {
                    bool exists = File.Exists(path);
                    Debug.Log($"[MainMenu] Kontrol: {path} -> {(exists ? "VAR" : "YOK")}");
                    
                    if (exists && !hasModel)
                    {
                        hasModel = true;
                        foundPath = path;
                        Debug.Log($"[MainMenu] ✓ Model bulundu: {path}");
                        break;
                    }
                }
                #elif UNITY_WEBGL
                // WebGL'de StreamingAssets'e File.Exists() ile erişilemez
                // StreamingAssets build'e dahil edildiyse varsayılan olarak var kabul et
                // Gerçek kontrol QLearningBrain'de UnityWebRequest ile yapılacak
                // WebGL build'de StreamingAssets klasörü varsa dosya orada olmalı
                hasModel = true; // WebGL build'de StreamingAssets varsa dosya orada olmalı
                foundPath = Path.Combine(Application.streamingAssetsPath, "RLBrain.json");
                Debug.Log($"[MainMenu] WebGL: StreamingAssets'te model oldugu varsayiliyor (gercek kontrol QLearningBrain'de yapilacak): {foundPath}");
                #else
                // Diğer platformlar 
                string streamingAssetsPath = Path.Combine(Application.streamingAssetsPath, "RLBrain.json");
                hasModel = File.Exists(streamingAssetsPath);
                if (hasModel)
                {
                    foundPath = streamingAssetsPath;
                    Debug.Log($"[MainMenu] StreamingAssets'te model bulundu: {streamingAssetsPath}");
                }
                #endif
                
                if (hasModel)
                {
                    aiLoadStatusText.text = "Hazir AI Model Yuklenecek";
                    aiLoadStatusText.color = Color.green;
                    Debug.Log($"[MainMenu] UI guncellendi: Hazir AI Model Yuklenecek (Path: {foundPath})");
                }
                else
                {
                    aiLoadStatusText.text = "Yeni AI Model Olusturulacak";
                    aiLoadStatusText.color = Color.yellow;
                    #if UNITY_EDITOR
                    Debug.LogWarning($"[MainMenu] ✗ Model bulunamadi! Kontrol edilen yollar:");
                    foreach (string path in pathsToCheck)
                    {
                        Debug.LogWarning($"  - {path}");
                    }
                    #endif
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
        
        #if UNITY_WEBGL && !UNITY_EDITOR
        StartCoroutine(UpdateAILoadStatusCoroutine());
        #else
        UpdateAILoadStatus();
        #endif
    }
    
    private IEnumerator UpdateAILoadStatusCoroutine()
    {
        // WebGL'de StreamingAssets'i UnityWebRequest ile kontrol et
        if (aiLoadStatusText != null)
        {
            bool aiLoaded = PlayerPrefs.GetInt("LoadAI", 1) == 1;
            
            if (aiLoaded)
            {
                string streamingAssetsUrl = Path.Combine(Application.streamingAssetsPath, "RLBrain.json");
                Debug.Log($"[MainMenu] WebGL: StreamingAssets kontrol ediliyor: {streamingAssetsUrl}");
                
                using (UnityWebRequest www = UnityWebRequest.Head(streamingAssetsUrl))
                {
                    yield return www.SendWebRequest();
                    
                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        aiLoadStatusText.text = "Hazir AI Model Yuklenecek";
                        aiLoadStatusText.color = Color.green;
                        Debug.Log($"[MainMenu] WebGL: ✓ Model bulundu: {streamingAssetsUrl}");
                    }
                    else
                    {
                        aiLoadStatusText.text = "Yeni AI Model Olusturulacak";
                        aiLoadStatusText.color = Color.yellow;
                        Debug.LogWarning($"[MainMenu] WebGL: ✗ Model bulunamadi: {www.error}");
                    }
                }
            }
            else
            {
                aiLoadStatusText.text = "Yapay Zeka Yuklenmeyecek";
                aiLoadStatusText.color = Color.yellow;
            }
        }
    }

    public void EnableLoadAI()
    {
        PlayerPrefs.SetInt("LoadAI", 1);
        PlayerPrefs.Save();
        #if UNITY_WEBGL && !UNITY_EDITOR
        StartCoroutine(UpdateAILoadStatusCoroutine());
        #else
        UpdateAILoadStatus();
        #endif
    }

    public void DisableLoadAI()
    {
        PlayerPrefs.SetInt("LoadAI", 0);
        PlayerPrefs.Save();
        #if UNITY_WEBGL && !UNITY_EDITOR
        StartCoroutine(UpdateAILoadStatusCoroutine());
        #else
        UpdateAILoadStatus();
        #endif
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

using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    // Duraklatma menüsü panel objesi
    public GameObject pauseMenuUI;
    
    // Oyun şu an duraklatıldı mı kontrol eder
    public static bool GameIsPaused = false;

    void Update()
    {
        // ESC tuşuna basılınca duraklatma menüsünü açar veya kapatır
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        // Duraklatma menüsü panelini gizler
        pauseMenuUI.SetActive(false); 
        
        // Oyun zamanını QLearningSpawnManager'daki hıza döndür (varsa)
        QLearningSpawnManager spawnManager = FindFirstObjectByType<QLearningSpawnManager>();
        if (spawnManager != null)
        {
            Time.timeScale = spawnManager.GetGameSpeed();
        }
        else
        {
            Time.timeScale = 1f;
        }
        
        // Oyunun devam ettiğini işaretler
        GameIsPaused = false;
    }

    void Pause()
    {
        // Duraklatma menüsü panelini gösterir
        pauseMenuUI.SetActive(true);
        // Oyun zamanını dondurur
        Time.timeScale = 0f;
        // Oyunun duraklatıldığını işaretler
        GameIsPaused = true;
    }

    public void LoadMenu()
    {
        // Menüye dönerken zamanı tekrar başlatır
        Time.timeScale = 1f;
        // Oyunun devam ettiğini işaretler
        GameIsPaused = false;
        // Ana menü sahnesine geçiş yapar
        SceneManager.LoadScene("MainMenu");
    }
    
}
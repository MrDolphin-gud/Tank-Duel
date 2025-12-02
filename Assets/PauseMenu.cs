using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI Referansı")]
    public GameObject pauseMenuUI; // Açıp kapatacağımız Panel objesi
    
    // Oyun şu an duraklatıldı mı?
    public static bool GameIsPaused = false;

    void Update()
    {
        // ESC tuşuna basılınca
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameIsPaused)
            {
                Resume(); // Açıksa kapat 
            }
            else
            {
                Pause(); // Kapalıysa aç 
            }
        }
    }

    public void Resume()
    {
        // Paneli gizle
        pauseMenuUI.SetActive(false); 
        // Zamanı normal akışına döndür 
        Time.timeScale = 1f;
        GameIsPaused = false;
    }

    void Pause()
    {
        // Paneli göster
        pauseMenuUI.SetActive(true);
        // Zamanı dondur
        Time.timeScale = 0f;
        
        GameIsPaused = true;
    }

    public void LoadMenu()
    {
        // Menüye dönerken zamanı tekrar başlatmalıyız yoksa etkileşime geçemeyiz.
        Time.timeScale = 1f;
        GameIsPaused = false;
        SceneManager.LoadScene("MainMenu");
    }
    
}
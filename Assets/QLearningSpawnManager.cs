using UnityEngine;

public class QLearningSpawnManager : MonoBehaviour
{
    [Header("Spawn Ayarları")]
    public GameObject enemyPrefab;
    public Transform spawnPoint;
    
    [Header("Oyun Hızı")]
    public float gameSpeed = 10f; // 10x hız
    
    [Header("Mayın Temizleme")]
    public float mineCleanupInterval = 30f; // Her 30 saniyede bir mayınları temizle
    private float lastMineCleanupTime = 0f;
    
    // Mevcut düşman tank
    private GameObject currentEnemy;
    
    void Start()
    {
        // Oyun hızını ayarla
        Time.timeScale = gameSpeed;
        Debug.Log($"[QLearningSpawnManager] Oyun hızı {gameSpeed}x olarak ayarlandı");
        
        // İlk düşmanı spawn et
        SpawnEnemy();
    }
    
    void Update()
    {
        // Düşman öldüyse yenisini spawn et
        if (currentEnemy == null || !currentEnemy.activeInHierarchy)
        {
            // Mayınları temizle
            CleanupMines();
            
            // Yeni düşman spawn et
            SpawnEnemy();
        }
        
        // Periyodik mayın temizleme
        if (Time.time - lastMineCleanupTime >= mineCleanupInterval)
        {
            CleanupMines();
            lastMineCleanupTime = Time.time;
        }
    }
    
    void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("[QLearningSpawnManager] Enemy prefab atanmamış!");
            return;
        }
        
        // Spawn noktası yoksa varsayılan pozisyon kullan
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : new Vector3(0, 1, 10);
        
        // 180 derece döndürülmüş olarak spawn et 
        Quaternion spawnRotation = Quaternion.Euler(0, 180, 0);
        
        currentEnemy = Instantiate(enemyPrefab, spawnPosition, spawnRotation);
        Debug.Log($"[QLearningSpawnManager] Yeni düşman spawn edildi: {currentEnemy.name}");
    }
    
    void CleanupMines()
    {
        // Tüm mayınları bul ve yok et
        Mine[] mines = FindObjectsByType<Mine>(FindObjectsSortMode.None);
        int mineCount = mines.Length;
        
        foreach (Mine mine in mines)
        {
            if (mine != null && mine.gameObject != null)
            {
                Destroy(mine.gameObject);
            }
        }
        
        if (mineCount > 0)
        {
            Debug.Log($"[QLearningSpawnManager] {mineCount} mayın temizlendi");
        }
    }
    
    void OnDestroy()
    {
        // Oyun hızını normale döndür
        Time.timeScale = 1f;
    }
    
    // Pause menüsünden erişilebilir
    public float GetGameSpeed()
    {
        return gameSpeed;
    }
}

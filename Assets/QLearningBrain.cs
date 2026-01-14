using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

public class QLearningBrain : MonoBehaviour
{
    [Header("General Settings")]
    public float learningRate = 0.5f; 
    public float discount = 0.9f; 
    public float exploration = 0.1f; 

    [Header("File Settings")]
    public string saveFileName = "RLBrain.json";

    // --- Input / Action Storage ---
    private List<float> currentInputs = new();     // dynamic input vector
    private List<ActionDefinition> actions = new(); // action registry

    // Q-Table : key = state string, value = Q-values for actions
    private Dictionary<string, float[]> Q = new();

    private string savePath;



    [Serializable]
    public class ActionDefinition
    {
        public string actionName;
        public Action<object[]> method;
        public int parameterCount;

        public ActionDefinition(string name, Action<object[]> func, int paramCount)
        {
            actionName = name;
            method = func;
            parameterCount = paramCount;
        }
    }

    [Serializable]
    private class QTableEntry
    {
        public string state;
        public float[] qValues;
        
        public QTableEntry() { }
        
        public QTableEntry(string state, float[] qValues)
        {
            this.state = state;
            this.qValues = qValues;
        }
    }

    [Serializable]
    private class SaveModel
    {
        public int inputCount;
        public int actionCount;
        public List<QTableEntry> qTableEntries; 
        public List<string> learnedStateActions; 
    }



    void Awake()
    {
        // Assets klasörüne kaydet (development için daha kolay)
        #if UNITY_EDITOR
        savePath = Path.Combine(Application.dataPath, saveFileName);
        #else
        savePath = Path.Combine(Application.persistentDataPath, saveFileName);
        #endif
        
        Debug.Log($"[QLearningBrain] Save path: {savePath}");
    }

    void Start()
    {
        // QLearningAgent.Awake() actions'ları register etmiş olmalı
        Debug.Log($"[QLearningBrain] Start - Actions: {actions.Count}");
        
        if (actions.Count == 0)
        {
            Debug.LogError("[QLearningBrain] HATA: Actions register edilmemiş! QLearningAgent var mı?");
            return;
        }
        
        // Model'i yükle
        LoadOrCreateModel();
        lastSaveTime = Time.time;
        lastSavedStateCount = Q.Count;
        
        Debug.Log($"[QLearning] Başlangıç - States: {Q.Count}, Learned Actions: {learnedStateActions.Count}");
    }

    
    /// <summary>
    /// Register input values every frame.
    /// Called by enemy AI script before calling DecideAction().
    /// </summary>
    public void SetInputs(List<float> inputs)
    {
        currentInputs = inputs;
    }

    /// <summary>
    /// Register an available action the agent can take.
    /// </summary>
    public void RegisterAction(string name, Action<object[]> method, int parameterCount)
    {
        actions.Add(new ActionDefinition(name, method, parameterCount));
    }

    /// <summary>
    /// Call this every decision tick. It returns which action index to execute.
    /// </summary>
    public int DecideAction()
    {
        // Önce state'i encode et
        string state = EncodeState(currentInputs);
        EnsureStateExists(state);

        int actionIndex;
        bool isExploration = UnityEngine.Random.value < exploration;
        
        if (isExploration)
        {
            actionIndex = UnityEngine.Random.Range(0, actions.Count);
            // Debug: Exploration action seçildiğinde logla (her 50 action'da bir)
            if (UnityEngine.Random.value < 0.02f) // %2 şansla logla (spam'i azalt)
            {
                Debug.Log($"[QLearning] EXPLORATION: Rastgele action {actionIndex} seçildi (exploration rate: {exploration * 100:F1}%)");
            }
        }
        else
        {
            float[] qRow = Q[state];

            int bestIndex = 0;
            float bestVal = qRow[0];
            
            // Eğer tüm Q-değerleri 0 ise rastgele seç (exploration)
            bool allZero = true;
            for (int i = 0; i < qRow.Length; i++)
            {
                if (Mathf.Abs(qRow[i]) > 0.001f)
                {
                    allZero = false;
                    break;
                }
            }
            
            if (allZero)
            {
                // Tüm Q-değerleri 0 rastgele seç
                actionIndex = UnityEngine.Random.Range(0, actions.Count);
                if (UnityEngine.Random.value < 0.02f)
                {
                    Debug.Log($"[QLearning] Tüm Q-değerleri 0, rastgele action {actionIndex} seçildi");
                }
            }
            else
            {
                // En iyi Q-değerine sahip action'ı seç
                for (int i = 1; i < qRow.Length; i++)
                {
                    if (qRow[i] > bestVal)
                    {
                        bestVal = qRow[i];
                        bestIndex = i;
                    }
                }
                actionIndex = bestIndex;
                
                // Debug: Exploitation action seçildiğinde logla 
                if (UnityEngine.Random.value < 0.02f)
                {
                    Debug.Log($"[QLearning] EXPLOITATION: En iyi action {actionIndex} seçildi (Q-value: {bestVal:F3})");
                }
            }
        }

        // Action'ı hemen kaydet (reward geldiğinde kullanılacak)
        SetLastAction(actionIndex, state);

        return actionIndex;
    }

    /// <summary>
    /// Execute the selected action.
    /// </summary>
    public void ExecuteAction(int actionIndex, params object[] parameters)
    {
        actions[actionIndex].method.Invoke(parameters);
    }

    // Son yapılan action'ı sakla (reward için gerekli)
    private int lastAction = -1;
    private string lastState = "";
    
    public void SetLastAction(int actionIndex, string state)
    {
        lastAction = actionIndex;
        lastState = state;
    }

    /// <summary>
    /// Give a positive reward.
    /// </summary>
    public void Reward(float value)
    {
        if (value != 0f)
        {
            ApplyReward(value);
        }
    }

    /// <summary>
    /// Give a negative reward.
    /// </summary>
    public void Punish(float value)
    {
        if (value != 0f)
        {
            ApplyReward(-Mathf.Abs(value));
        }
    }
    

    public void TeachActionInState(string state, int actionIndex, float hintReward)
    {
        if (string.IsNullOrEmpty(state) || actionIndex < 0 || actionIndex >= actions.Count)
            return;
            
        EnsureStateExists(state);
        float[] qRow = Q[state];
        
        // Doğru action'a küçük bir reward ver (öğrenmeyi teşvik et)
        float oldQValue = qRow[actionIndex];
        qRow[actionIndex] = qRow[actionIndex] + learningRate * hintReward;
        
        // Hint verildiğinde de learnedStateActions'a ekle (eğer Q-değeri 0'dan farklı bir değere geçtiyse)
        string stateActionKey = $"{state}_A{actionIndex}";
        bool qValueWasZero = Mathf.Abs(oldQValue) < 0.001f;
        bool qValueNowNonZero = Mathf.Abs(qRow[actionIndex]) > 0.001f;
        
        if (qValueWasZero && qValueNowNonZero && !learnedStateActions.Contains(stateActionKey))
        {
            learnedStateActions.Add(stateActionKey);
        }
        
        Debug.Log($"[QLearning] HINT: State'te action {actionIndex} öğretildi! Q: {oldQValue:F3} -> {qRow[actionIndex]:F3} (+{hintReward:F1})");
    }
    

    public void TeachActionInStateWithAngle(string state, int actionIndex, float angle, string direction, float baseReward = 1f)
    {
        TeachActionInStateWithAngle(state, actionIndex, angle, direction, baseReward, "", "");
    }
    

    public void TeachActionInStateWithAngle(string state, int actionIndex, float angle, string direction, float baseReward, string part, string target)
    {
        if (string.IsNullOrEmpty(state) || actionIndex < 0 || actionIndex >= actions.Count)
            return;
            
        EnsureStateExists(state);
        float[] qRow = Q[state];
        
        // Açıya göre hint reward'ını ayarla (daha büyük açı = daha önemli = daha fazla hint)
        // Açı 0-180 derece arası 180 derece = maksimum hint
        // 0-10 arası ölçeklendirildi: baseReward 1, max hint 2
        float angleMultiplier = Mathf.Clamp01(angle / 180f); // 0-1 arası
        float hintReward = baseReward * (1f + angleMultiplier); // 1-2 arası hint (0-10 arası ölçeklendirildi)
        
        // Doğru action'a açıya göre ayarlanmış reward ver
        float oldQValue = qRow[actionIndex];
        qRow[actionIndex] = qRow[actionIndex] + learningRate * hintReward;
        
        // Hint verildiğinde de learnedStateActions'a ekle (eğer Q-değeri 0'dan farklı bir değere geçtiyse)
        string stateActionKey = $"{state}_A{actionIndex}";
        bool qValueWasZero = Mathf.Abs(oldQValue) < 0.001f;
        bool qValueNowNonZero = Mathf.Abs(qRow[actionIndex]) > 0.001f;
        
        if (qValueWasZero && qValueNowNonZero && !learnedStateActions.Contains(stateActionKey))
        {
            learnedStateActions.Add(stateActionKey);
        }
        
        // Mesaj oluştur
        string message;
        if (!string.IsNullOrEmpty(part) && !string.IsNullOrEmpty(target))
        {
            // "Taret oyuncuya döndür" gibi anlamlı mesaj
            message = $"HINT: {part} {target} döndür! ({angle:F1} derece {direction})";
        }
        else
        {
            // Genel mesaj
            message = $"HINT: {angle:F1} derece {direction} döndür!";
        }
        
        Debug.Log($"[QLearning] {message} Action {actionIndex} öğretildi. Q: {oldQValue:F3} -> {qRow[actionIndex]:F3} (+{hintReward:F2})");
    }

    // Kaydetme için sayaçlar
    private int rewardCount = 0;
    private int lastSavedStateCount = 0;
    private float lastSaveTime = 0f;
    private float saveInterval = 10f; // Her 10 saniyede bir kaydet
    
    // Öğrenilen state-action çiftlerini takip et 
    private HashSet<string> learnedStateActions = new HashSet<string>();

    private void ApplyReward(float reward)
    {
        if (lastAction < 0 || string.IsNullOrEmpty(lastState))
        {
            // Henüz action yapılmamış reward'u görmezden gel
            Debug.Log($"[QLearning] Reward görmezden gelindi - Henüz action yapılmamış (Reward: {reward:F1})");
            return;
        }

        // Input'ların güncel olduğundan emin ol 
        if (currentInputs == null || currentInputs.Count == 0)
        {
            Debug.LogWarning($"[QLearning] UYARI: currentInputs boş! Reward uygulanamıyor. (Reward: {reward:F1})");
            return;
        }

        EnsureStateExists(lastState);
        float[] qRow = Q[lastState];

        // Şu anki state'i al
        string currentState = EncodeState(currentInputs);
        EnsureStateExists(currentState);

        float maxNext = Max(Q[currentState]);

        // Q-Learning formülü: Q(s,a) = Q(s,a) + α * (r + γ * max(Q(s',a')) - Q(s,a))
        float oldQValue = qRow[lastAction];
        float tdError = reward + discount * maxNext - qRow[lastAction]; // Temporal Difference Error
        float newQValue = qRow[lastAction] + learningRate * tdError;
        qRow[lastAction] = newQValue;
        
        // Debug: Q-değeri güncellemesini detaylı logla 
        if (rewardCount % 20 == 0)
        {
            Debug.Log($"[QLearning] Q-Update Detay: State: {lastState.Substring(0, Mathf.Min(30, lastState.Length))}... | Action: {lastAction} | Reward: {reward:F2} | MaxNext: {maxNext:F3} | TD-Error: {tdError:F3} | Old: {oldQValue:F3} -> New: {newQValue:F3}");
        }
        
        rewardCount++;
        

        string stateActionKey = $"{lastState}_A{lastAction}";
        bool alreadyLearned = learnedStateActions.Contains(stateActionKey);
        bool qValueWasZero = Mathf.Abs(oldQValue) < 0.001f;
        bool qValueNowNonZero = Mathf.Abs(newQValue) > 0.001f;
        
        if (!qValueWasZero)
        {
            // Q-değeri zaten 0'dan farklıydı (daha önce öğrenilmiş)
            // Bu "ilk kez öğrenme" değil normal güncelleme
            // HashSet'e ekle (eğer yoksa) ama log çıkarma
            if (!alreadyLearned)
            {
                learnedStateActions.Add(stateActionKey);
            }
            // Sessizce geç - bu normal bir durum
        }
        else if (qValueWasZero && qValueNowNonZero)
        {
            // Q-değeri 0'dan farklı bir değere geçti
            if (!alreadyLearned)
            {
                // Gerçekten ilk kez öğreniliyor
                learnedStateActions.Add(stateActionKey);
                Debug.Log($"[QLearning] ILK OGRENME! State'te action {lastAction} için Q-değeri 0'dan {newQValue:F3}'e yükseldi! (State: {lastState.Substring(0, Mathf.Min(30, lastState.Length))}...)");
            }
            else
            {
                // HashSet'te var ama Q-değeri 0 - bu quantization nedeniyle farklı state string'i olabilir
                // Ama daha önce öğrenilmiş, bu yüzden log çıkarma
                // Sessizce geç - bu normal bir durum
            }
        }
        else if (Mathf.Abs(oldQValue) > 0.001f && Mathf.Abs(newQValue - oldQValue) > 0.001f)
        {
            // Q-değeri güncellendi 
            // Her 20 reward'da bir logla 
            if (rewardCount % 20 == 0)
            {
                Debug.Log($"[QLearning] Q-Value Updated (Existing): State action {lastAction} | Old: {oldQValue:F3} -> New: {newQValue:F3} | Reward: {reward:F1}");
            }
        }
        
        // Debug: Q-değerlerinin güncellendiğini göster 
        if (rewardCount % 10 == 0)
        {
            string statePreview = lastState.Length > 40 ? lastState.Substring(0, 40) + "..." : lastState;
            string learnedStatus = learnedStateActions.Contains(stateActionKey) ? "Ogrenilmis" : "Yeni";
            Debug.Log($"[QLearning] Q-Value Updated! Reward #{rewardCount} | State: {statePreview} | Action: {lastAction} | Old: {oldQValue:F3} -> New: {newQValue:F3} | Reward: {reward:F1} | MaxNext: {maxNext:F3} | {learnedStatus}");
        }

        // Her 50 reward'da bir veya her 10 saniyede bir state sayısını logla
        if (rewardCount % 50 == 0 || Time.time - lastSaveTime >= saveInterval)
        {
            int currentStateCount = Q.Count;
            if (currentStateCount != lastSavedStateCount)
            {
                // State sayısı çok fazlaysa uyarı ver
                string warning = currentStateCount > 1000 ? " COK FAZLA STATE!" : "";
                Debug.Log($"[QLearning] States: {currentStateCount} (+{currentStateCount - lastSavedStateCount}) | Rewards: {rewardCount} | Learned Actions: {learnedStateActions.Count} | Last Reward: {reward:F1}{warning}");
                lastSavedStateCount = currentStateCount;
            }
        }

        // Otomatik kaydetme 
        bool shouldSave = false;
        if (Time.time - lastSaveTime >= saveInterval)
        {
            shouldSave = true;
            lastSaveTime = Time.time;
        }
        else if (Q.Count != lastSavedStateCount && Q.Count > 0)
        {
            // Yeni state eklendiyse kaydet
            shouldSave = true;
        }

        if (shouldSave)
        {
            SaveModelToFile();
        }
    }

    private float Max(float[] arr)
    {
        float m = arr[0];
        for (int i = 1; i < arr.Length; i++)
            if (arr[i] > m) m = arr[i];
        return m;
    }

    private void EnsureStateExists(string state)
    {
        if (!Q.ContainsKey(state))
        {
            // Yeni state oluştur - tüm Q-değerleri 0 ile başlar 
            Q[state] = new float[actions.Count];
        }
    }
    

    // State'in boş olup olmadığını kontrol et (tüm Q-değerleri 0 mı?)

    private bool IsStateEmpty(string state)
    {
        if (!Q.ContainsKey(state)) return true;
        
        float[] qRow = Q[state];
        foreach (float qVal in qRow)
        {
            if (Mathf.Abs(qVal) > 0.001f) // En az bir Q-değeri 0'dan farklıysa boş değil
                return false;
        }
        return true; // Tüm Q-değerleri 0 ise boş
    }



    public string EncodeState(List<float> inputs)
    {
        // State'i quantize et 
        // Her input'u 0.5 aralıklarla yuvarla (daha az state = daha hızlı öğrenme)
        // 0.25 -> 0.5 değişti (5 değer yerine 3 değer: 0, 0.5, 1.0)
        List<string> quantized = new List<string>();
        foreach (float val in inputs)
        {
            float quantizedVal = Mathf.Round(val * 2f) / 2f; // 0.5 hassasiyetinde
            quantized.Add(quantizedVal.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
        }
        return string.Join("_", quantized);
    }

    private void LoadOrCreateModel()
    {
        // Actions henüz register edilmemişse bekle
        if (actions.Count == 0)
        {
            Debug.LogWarning("QLearningBrain: Actions not registered yet. Model will be created after registration.");
            return;
        }

        // AI yükleme aktif mi kontrol et
        bool shouldLoadAI = PlayerPrefs.GetInt("LoadAI", 0) == 1;
        
        // Özel model yolu var mı kontrol et
        string customModelPath = PlayerPrefs.GetString("CustomAIModelPath", "");
        string loadPath = savePath; // Varsayılan olarak normal save path
        
        if (shouldLoadAI && !string.IsNullOrEmpty(customModelPath) && File.Exists(customModelPath))
        {
            // Özel model dosyası var, onu kullan
            loadPath = customModelPath;
            Debug.Log($"[QLearningBrain] Ozel model dosyasi yukleniyor: {loadPath}");
        }
        else if (!shouldLoadAI)
        {
            // AI yükleme kapalı, yeni model oluştur
            Debug.Log("[QLearningBrain] AI yukleme kapali. Yeni model olusturuluyor.");
            CreateNewModel();
            return;
        }
        else if (!File.Exists(loadPath))
        {
            Debug.Log("QLearningBrain: Creating new model file.");
            CreateNewModel();
            return;
        }

        try
        {
            string json = File.ReadAllText(loadPath);
            Debug.Log($"[QLearningBrain] Model yukleniyor: {loadPath}");
            var model = JsonUtility.FromJson<SaveModel>(json);

            // Action sayısını kontrol et
            if (model.actionCount != actions.Count)
            {
                Debug.LogWarning($"QLearningBrain: Action count changed ({model.actionCount}->{actions.Count}). Creating new model.");
                CreateNewModel();
                return;
            }

            // Input sayısını kontrol et (sabit değer: 14)
            int expectedInputCount = 14;
            if (model.inputCount != expectedInputCount)
            {
                Debug.LogWarning($"QLearningBrain: Input count mismatch ({model.inputCount}->{expectedInputCount}). Model may be incompatible, but loading anyway.");
                // Yine de yüklemeyi dene, sadece uyarı ver
            }

            // Q-Table'ı yükle (List'ten Dictionary'ye çevir)
            Q = new Dictionary<string, float[]>();
            
            if (model.qTableEntries != null && model.qTableEntries.Count > 0)
            {
                int loadedCount = 0;
                int skippedCount = 0;
                foreach (var entry in model.qTableEntries)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.state) && entry.qValues != null)
                    {
                        // Q-değerlerinin boş olup olmadığını kontrol et
                        bool hasNonZeroValue = false;
                        foreach (float qVal in entry.qValues)
                        {
                            if (Mathf.Abs(qVal) > 0.001f)
                            {
                                hasNonZeroValue = true;
                                break;
                            }
                        }
                        
                        if (hasNonZeroValue)
                        {
                            Q[entry.state] = entry.qValues;
                            loadedCount++;
                        }
                        else
                        {
                            skippedCount++;
                        }
                    }
                }
                
                Debug.Log($"QLearningBrain: Model loaded successfully! States: {loadedCount} (skipped {skippedCount} empty states)");
                
                // Eğer çok fazla boş state varsa uyarı ver
                if (skippedCount > loadedCount)
                {
                    Debug.LogWarning($"[QLearning] UYARI: Yüklenen state'lerden daha fazla boş state var! ({skippedCount} boş, {loadedCount} dolu)");
                    Debug.LogWarning("Bu, eski model dosyasında çok fazla boş state olduğunu gösterir. Model dosyasını temizlemek isteyebilirsiniz.");
                }
                
                // learnedStateActions'ı yükle (eğer varsa)
                learnedStateActions = new HashSet<string>();
                
                if (model.learnedStateActions != null && model.learnedStateActions.Count > 0)
                {
                    // Kaydedilmiş learnedStateActions'ı yükle
                    foreach (string key in model.learnedStateActions)
                    {
                        learnedStateActions.Add(key);
                    }
                    Debug.Log($"QLearningBrain: {learnedStateActions.Count} öğrenilmiş state-action çifti dosyadan yüklendi");
                }
                
                // Bu quantization nedeniyle oluşan farklılıkları önler
                int autoGeneratedCount = 0;
                int totalNonZeroQValues = 0;
                foreach (var kvp in Q)
                {
                    string state = kvp.Key;
                    float[] qValues = kvp.Value;
                    
                    // Her action için Q-değerini kontrol et
                    for (int actionIndex = 0; actionIndex < qValues.Length; actionIndex++)
                    {
                        if (Mathf.Abs(qValues[actionIndex]) > 0.001f) // Q-değeri 0'dan farklıysa
                        {
                            totalNonZeroQValues++;
                            string stateActionKey = $"{state}_A{actionIndex}";
                            if (!learnedStateActions.Contains(stateActionKey))
                            {
                                learnedStateActions.Add(stateActionKey);
                                autoGeneratedCount++;
                            }
                        }
                    }
                }
                
                Debug.Log($"QLearningBrain: Model yüklendi! Toplam {loadedCount} state, {totalNonZeroQValues} sıfırdan farklı Q-değeri bulundu.");
                
                if (autoGeneratedCount > 0)
                {
                    Debug.Log($"QLearningBrain: {autoGeneratedCount} öğrenilmiş state-action çifti Q-değerlerinden otomatik oluşturuldu (toplam: {learnedStateActions.Count})");
                }
                else if (learnedStateActions.Count == 0)
                {
                    Debug.Log("QLearningBrain: Öğrenilmiş state-action çifti bulunamadı (eski model formatı veya yeni model). Yeni HashSet oluşturuldu.");
                }
                else
                {
                    Debug.Log($"QLearningBrain: Toplam {learnedStateActions.Count} öğrenilmiş state-action çifti hazır (tekrar öğrenme logları önlenecek)");
                }
                
                // Eğer Q-değerleri varsa ama learnedStateActions boşsa bir sorun var demektir
                if (totalNonZeroQValues > 0 && learnedStateActions.Count == 0)
                {
                    Debug.LogError($"QLearningBrain: HATA! {totalNonZeroQValues} sıfırdan farklı Q-değeri var ama learnedStateActions boş! Bu bir bug olabilir.");
                }
                
                // İlk birkaç state'in Q-değerlerini kontrol et (debug için)
                int sampleCount = 0;
                foreach (var entry in Q)
                {
                    if (sampleCount < 3)
                    {
                        string qValuesStr = string.Join(", ", entry.Value);
                        Debug.Log($"Sample State {sampleCount}: {entry.Key.Substring(0, Mathf.Min(50, entry.Key.Length))}... | Q-Values: [{qValuesStr}]");
                        sampleCount++;
                    }
                }
            }
            else
            {
                Debug.LogWarning("QLearningBrain: Model file exists but Q-Table entries are empty or null. Starting fresh.");
                CreateNewModel();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"QLearningBrain: Error loading model: {e.Message}. Creating new model.");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            CreateNewModel();
        }
    }

    private void CreateNewModel()
    {
        Q = new Dictionary<string, float[]>();
        learnedStateActions.Clear(); // Öğrenilen state-action çiftlerini de temizle
        SaveModelToFile();
    }

    private void SaveModelToFile()
    {
        try
        {
            // Input count'u sabit tut (state encoding değişse bile uyumlu olmalı)
            // QLearningAgent.BuildStateInputs()'den: mesafe(1) + oyuncu(1) + pozisyon(2) + oyuncuya_göre_açı(4) + can(1) + cooldown(3) + kalkan(1) + hareket(1) = 14
            int expectedInputCount = 14;
            
            // Dictionary'yi List'e çevir 
            // Sadece boş olmayan state'leri kaydet (boş state'leri temizle)
            List<QTableEntry> entries = new List<QTableEntry>();
            int emptyStatesSkipped = 0;
            foreach (var kvp in Q)
            {
                // Boş state'leri atla (tüm Q-değerleri 0 olan)
                bool isEmpty = true;
                foreach (float qVal in kvp.Value)
                {
                    if (Mathf.Abs(qVal) > 0.001f)
                    {
                        isEmpty = false;
                        break;
                    }
                }
                
                if (!isEmpty)
                {
                    entries.Add(new QTableEntry(kvp.Key, kvp.Value));
                }
                else
                {
                    emptyStatesSkipped++;
                }
            }
            
            if (emptyStatesSkipped > 0)
            {
                Debug.Log($"[QLearning] Kaydetme: {emptyStatesSkipped} boş state atlandı (temizlendi)");
            }
            
            // learnedStateActions HashSet'ini List'e çevir
            List<string> learnedActionsList = new List<string>(learnedStateActions);
            
            var model = new SaveModel
            {
                inputCount = expectedInputCount,
                actionCount = actions.Count,
                qTableEntries = entries,
                learnedStateActions = learnedActionsList
            };

            string json = JsonUtility.ToJson(model, true);
            File.WriteAllText(savePath, json);
            Debug.Log($"QLearningBrain: Model saved. States: {Q.Count}, Actions: {actions.Count}, Learned State-Actions: {learnedStateActions.Count}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"QLearningBrain: Error saving model: {e.Message}");
        }
    }


    public void SaveModelManually()
    {
        SaveModelToFile();
    }
}

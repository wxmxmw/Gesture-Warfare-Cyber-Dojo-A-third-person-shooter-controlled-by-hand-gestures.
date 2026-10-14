using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// 对话数据结构
[System.Serializable]
public class Dialogue
{
    [Header("对话设置")]
    public string dialogue_id = ""; // 对话ID
    public float duration_time = 3f; // 对话显示时长
    public float delay_time = 0f; // 进入生成这条对话的过程时要等这么多秒才会生成
    
    // 验证对话设置
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(dialogue_id) && duration_time > 0f;
    }
}

[System.Serializable]
public class EnemyCombination
{
    [Header("敌人组合设置")]
    public string combinationName = "New Combination"; // 组合名称
    public List<GameObject> enemyPrefabs = new List<GameObject>(); // 敌人预制体列表
    public List<int> enemyCounts = new List<int>(); // 每个敌人类型的数量
    public List<float> spawnWeights = new List<float>(); // 每个敌人类型的生成权重（用于随机选择）
    
    // 验证组合设置
    public bool IsValid()
    {
        if (enemyPrefabs.Count == 0) return false;
        if (enemyCounts.Count != enemyPrefabs.Count) return false;
        if (spawnWeights.Count != enemyPrefabs.Count) return false;
        
        // 检查是否有有效的敌人预制体
        foreach (GameObject prefab in enemyPrefabs)
        {
            if (prefab == null) return false;
        }
        
        return true;
    }
    
    // 获取总敌人数量
    public int GetTotalEnemyCount()
    {
        int total = 0;
        foreach (int count in enemyCounts)
        {
            total += count;
        }
        return total;
    }
}

[System.Serializable]
public class SpawnWave
{
    [Header("波次设置")]
    public string waveName = "New Wave"; // 波次名称
    
    [Header("波次类型")]
    public WaveType waveType = WaveType.SpawnEnemies; // 波次类型
    
    [Header("波次执行条件")]
    public bool waitForClearField = false; // 是否等待场景清空后再执行本波次
    
    [Header("敌人组合设置（当waveType为SpawnEnemies时使用）")]
    public bool useEnemyCombination = false; // 是否使用敌人组合
    public int enemyCombinationId = 0; // 敌人组合ID
    
    [Header("传统设置（当waveType为SpawnEnemies且useEnemyCombination为false时使用）")]
    public GameObject enemyPrefab; // 单个敌人预制体
    public int enemyCount; // 敌人数量
    
    [Header("思考设置（当waveType为Think时使用）")]
    public string thinkId = ""; // 思考内容ID
    public float thinkDisplayDuration = 3f; // 思考显示时长
    
    [Header("场景切换设置（当waveType为ChangeScene时使用）")]
    public int sceneId = 0; // 场景ID
    
    [Header("BGM切换设置（当waveType为ChangeBGM时使用）")]
    public BGMManager.BGMType bgmType = BGMManager.BGMType.Battle; // BGM类型
    public bool forceRestart = false; // 是否强制重新播放（即使是相同的BGM）
    
    [Header("对话设置")]
    public List<Dialogue> dialogues = new List<Dialogue>(); // 对话列表
    
    [Header("生成时间设置")]
    public float spawnInterval = 1f; // 生成间隔
    public float waveDelay = 3f; // 波次间隔
    
    // 验证波次设置
    public bool IsValid()
    {
        switch (waveType)
        {
            case WaveType.SpawnEnemies:
                if (useEnemyCombination)
                {
                    return enemyCombinationId >= 0; // 组合ID有效
                }
                else
                {
                    return enemyPrefab != null && enemyCount > 0; // 传统模式有效
                }
                
            case WaveType.Think:
                return !string.IsNullOrEmpty(thinkId); // 思考ID有效
                
            case WaveType.ChangeScene:
                return sceneId >= 0; // 场景ID有效
                
            case WaveType.ChangeBGM:
                return true; // BGM切换总是有效
                
            default:
                return false;
        }
    }
    
    // 获取有效的对话列表
    public List<Dialogue> GetValidDialogues()
    {
        List<Dialogue> validDialogues = new List<Dialogue>();
        foreach (Dialogue dialogue in dialogues)
        {
            if (dialogue.IsValid())
            {
                validDialogues.Add(dialogue);
            }
        }
        return validDialogues;
    }
}

// 波次类型枚举
public enum WaveType
{
    SpawnEnemies,  // 生成敌人
    Think,         // 思考
    ChangeScene,   // 切换场景
    ChangeBGM      // 切换BGM
}

[System.Serializable]
public class SpawnBoss
{
    public GameObject bossPrefab;
    // 生成条件
    public bool SpawnAfterLastWave; // 在所有敌人（已经生成的和还没有遂波次生成的）都被消灭之后生成

    public bool SpawnAfterSomeTime; // 在游戏开始后固定时间下生成
    public int SpawnTime;           // 如果SpawnAfterSomeTime=true，此处为生成的固定时间（单位秒）

    public bool IsLastBoss;         // 是否为最后一个boss
}

[System.Serializable]
public class LevelData
{
    [Header("关卡设置")]
    public string levelName = "New Level";
    public List<SpawnWave> spawnWaves = new List<SpawnWave>();
    public List<SpawnBoss> spawnBosses = new List<SpawnBoss>();
    
    [Header("关卡特殊设置")]
    public float levelBGMVolume = 1f;
    public Color levelAmbientColor = Color.white;
    
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(levelName) && 
               (spawnWaves.Count > 0 || spawnBosses.Count > 0);
    }
}

public class EnemySpawner : MonoBehaviour
{
    // 击杀记录系统
    [System.Serializable]
    public class KillRecord
    {
        public string enemyName;
        public int killCount;
        public int scorePerKill;
        
        public KillRecord(string name, int count, int score)
        {
            enemyName = name;
            killCount = count;
            scorePerKill = score;
        }
        
        public int GetTotalScore()
        {
            return killCount * scorePerKill;
        }
    }
    
    // 添加击杀记录变量
    private List<KillRecord> currentLevelKillRecords = new List<KillRecord>();
    
    [Header("Spawn Settings")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnRadius = 20f;
    [SerializeField] private Camera playerCamera;
    
    [Header("敌人组合管理器")]
    [SerializeField] private List<EnemyCombination> enemyCombinations = new List<EnemyCombination>();
    
    [Header("多关卡系统")]
    [SerializeField] private List<LevelData> levels = new List<LevelData>();
    [SerializeField] private int currentLevelIndex = 0;
    [SerializeField] private bool autoStartNextLevel = true;
    
    // 原来的单个关卡数据
    // [Header("Spawn Waves")]
    // [SerializeField] private List<SpawnWave> spawnWaves = new List<SpawnWave>();
    // [Header("Boss Settings")]
    // [SerializeField] private List<SpawnBoss> spawnBoss = new List<SpawnBoss>();
    
    // 添加关卡相关的事件
    public System.Action<int> OnLevelCompleted; // 关卡完成事件，参数为关卡索引
    public System.Action<int> OnLevelStarted;   // 关卡开始事件，参数为关卡索引
    public System.Action OnAllLevelsCompleted;  // 所有关卡完成事件
    
    // 修改现有的变量
    private List<bool> bossSpawnedFlags = new List<bool>(); // 当前关卡的boss生成标志
    private bool allWavesCompleted = false; // 当前关卡所有波次完成标志
    private bool currentLevelCompleted = false; // 当前关卡完成标志

    [SerializeField] private bool autoStartSpawning = true;
    [SerializeField] private bool loopWaves = false;

    [Header("Debug")]
    [SerializeField] private int debugStartLevelIndex = 0; // 调试用：从第几关开始（0=第一关）

    private float gameStartTime; // 游戏开始时间
    private Coroutine bossSpawnCoroutine;

    private List<BossController> activeBosses = new List<BossController>(); // 当前被激活的Boss

    [Header("Dynamic Spawning")]
    [SerializeField] private int maxEnemiesOnScreen = 10;
    [SerializeField] private float continuousSpawnInterval = 3f;
    [SerializeField] private GameObject[] enemyPrefabs;

    [SerializeField] public GameObject BossWarning;
    [SerializeField] public GameObject WaveWarning;
    [SerializeField] public TextMeshProUGUI WaveWarningText;
    // BGM管理器引用
    private BGMManager bgmManager;
    private BGMManager.BGMType previousBGMType = BGMManager.BGMType.Battle; // 记录之前的BGM类型

    private List<EnemyWithNewAttackSystem> activeEnemies = new List<EnemyWithNewAttackSystem>();
    private int currentWaveIndex = 0;
    private bool isSpawning = false;
    private Coroutine spawnCoroutine;

    // 小Boss暂停波次系统
    private bool isWavePaused = false;
    private HashSet<BossController> miniBosses = new HashSet<BossController>(); // 追踪当前存活的小Boss

    // 添加事件
    public System.Action OnAllEnemiesDefeated; // 所有敌人被消灭事件





    // 添加关卡管理方法


    public LevelData GetCurrentLevel()
    {
        if (currentLevelIndex >= 0 && currentLevelIndex < levels.Count)
        {
            return levels[currentLevelIndex];
        }
        return null;
    }
    
    public int GetCurrentLevelIndex()
    {
        return currentLevelIndex;
    }
    
    public int GetTotalLevelCount()
    {
        return levels.Count;
    }

    public bool GetautoStartNextLevel() 
    {
        return autoStartNextLevel;
    }

    public bool HasNextLevel()
    {
        return currentLevelIndex + 1 < levels.Count;
    }
    
    public void StartNextLevel()
    {
        if (!HasNextLevel())
        {
            Debug.Log("No more levels available!");
            OnAllLevelsCompleted?.Invoke();
            return;
        }
        
        currentLevelIndex++;
        StartCurrentLevel();
    }
    
    public void StartCurrentLevel()
    {
        LevelData currentLevel = GetCurrentLevel();
        if (currentLevel == null)
        {
            Debug.LogError($"Level {currentLevelIndex} is invalid!");
            return;
        }
        
        Debug.Log($"Starting Level {currentLevelIndex + 1}: {currentLevel.levelName}");
        
        // 重置关卡状态
        currentLevelCompleted = false;
        allWavesCompleted = false;
        
        // 清空击杀记录
        ClearCurrentLevelKillRecords();
        
        // 初始化当前关卡的boss系统
        InitializeCurrentLevelBossSystem();
        
        // 触发关卡开始事件
        OnLevelStarted?.Invoke(currentLevelIndex);
        
        // 开始生成当前关卡的敌人
        if (isSpawning)
        {
            StartCoroutine(SpawnCurrentLevelWaves());
        }
    }
    
    private void InitializeBossSystem()
    {
        // 记录游戏开始时间
        gameStartTime = Time.time;
        
        // 初始化boss生成标志列表
        bossSpawnedFlags.Clear();
        for (int i = 0; i < GetCurrentLevel().spawnBosses.Count; i++)
        {
            bossSpawnedFlags.Add(false);
        }
        
        // 开始boss生成检查协程
        if (GetCurrentLevel().spawnBosses.Count > 0)
        {
            bossSpawnCoroutine = StartCoroutine(CheckBossSpawnConditions());
        }
        
        Debug.Log($"Boss system initialized with {GetCurrentLevel().spawnBosses.Count} bosses");
    }
    
    private void ValidateEnemyCombinations()
    {
        // 验证所有敌人组合设置
        for (int i = 0; i < enemyCombinations.Count; i++)
        {
            if (!enemyCombinations[i].IsValid())
            {
                Debug.LogWarning($"Enemy Combination {i} ({enemyCombinations[i].combinationName}) is invalid!");
            }
        }
        
        Debug.Log($"Enemy combinations validated. Total combinations: {enemyCombinations.Count}");
    }
    
    public void StartSpawning()
    {
        if (!isSpawning)
        {
            isSpawning = true;
            
            // 重置到指定关卡（调试用：debugStartLevelIndex 控制从第几关开始）
            currentLevelIndex = Mathf.Clamp(debugStartLevelIndex, 0, levels.Count - 1);
            currentLevelCompleted = false;
            
            // 开始第一关卡
            StartCurrentLevel();
        }
    }
    
    public void StopSpawning()
    {
        isSpawning = false;
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        
        // 停止boss生成检查
        if (bossSpawnCoroutine != null)
        {
            StopCoroutine(bossSpawnCoroutine);
            bossSpawnCoroutine = null;
        }
    }
    
    /// <summary>
    /// 重新初始化boss系统（用于游戏重新开始时）
    /// </summary>
    public void ReinitializeBossSystem()
    {
        // 重置boss生成标志
        for (int i = 0; i < bossSpawnedFlags.Count; i++)
        {
            bossSpawnedFlags[i] = false;
        }
        
        // 重置波次完成标志
        allWavesCompleted = false;
        
        // 重置游戏开始时间
        gameStartTime = Time.time;
        
        Debug.Log("Boss system reinitialized");
    }
    
    private System.Collections.IEnumerator SpawnWaves()
    {
        do
        {
            for (int waveIndex = 0; waveIndex < GetCurrentLevel().spawnWaves.Count; waveIndex++)
            {
                currentWaveIndex = waveIndex;
                SpawnWave wave = GetCurrentLevel().spawnWaves[waveIndex];
                
                Debug.Log($"Starting wave {waveIndex + 1}: {wave.waveName} (Type: {wave.waveType})");

                // 验证波次设置
                if (!wave.IsValid())
                {
                    Debug.LogWarning($"Wave {waveIndex + 1} is invalid, skipping...");
                    continue;
                }
                
                // 检查是否需要等待场景清空
                if (wave.waitForClearField)
                {
                    Debug.Log($"Wave {waveIndex + 1} waiting for clear field...");
                    yield return StartCoroutine(WaitForClearField());
                }
                
                // 根据波次类型执行不同逻辑
                switch (wave.waveType)
                {
                    case WaveType.SpawnEnemies:
                        // 显示波次警告
                        WaveWarning.SetActive(true);
                        WaveWarning.GetComponentInParent<Animator>().Play("BossWarning_appear");
                        WaveWarningText.text = "wave" + (waveIndex + 1);
                        
                        // 生成这一波的敌人
                        if (wave.useEnemyCombination)
                        {
                            yield return StartCoroutine(SpawnEnemyCombination(wave));
                        }
                        else
                        {
                            yield return StartCoroutine(SpawnTraditionalWave(wave));
                        }
                        
                        // 等待波次间隔
                        yield return new WaitForSeconds(wave.waveDelay);
                        WaveWarning.SetActive(false);
                        break;
                        
                    case WaveType.Think:
                        // 执行思考逻辑
                        yield return StartCoroutine(ExecuteThinkWave(wave));
                        break;
                        
                    case WaveType.ChangeScene:
                        // 执行场景切换逻辑
                        yield return StartCoroutine(ExecuteSceneChangeWave(wave));
                        break;
                        
                    case WaveType.ChangeBGM:
                        // 执行BGM切换逻辑
                        yield return StartCoroutine(ExecuteBGMChangeWave(wave));
                        break;
                }
            }

            if (loopWaves)
            {
                Debug.Log("Restarting spawn waves...");
            }
            
        } while (loopWaves && isSpawning);
        
        // 标记所有波次完成
        allWavesCompleted = true;
        Debug.Log("All spawn waves completed!");
        
        // 如果不循环，开始连续生成
        if (isSpawning)
        {
            StartCoroutine(ContinuousSpawn());
        }
    }
    
    private System.Collections.IEnumerator SpawnEnemyCombination(SpawnWave wave)
    {
        // 获取敌人组合
        EnemyCombination combination = GetEnemyCombination(wave.enemyCombinationId);
        if (combination == null)
        {
            Debug.LogError($"Enemy Combination with ID {wave.enemyCombinationId} not found!");
            yield break;
        }
        
        Debug.Log($"Spawning combination: {combination.combinationName}");
        
        // 生成组合中的所有敌人
        for (int enemyTypeIndex = 0; enemyTypeIndex < combination.enemyPrefabs.Count; enemyTypeIndex++)
        {
            GameObject enemyPrefab = combination.enemyPrefabs[enemyTypeIndex];
            int enemyCount = combination.enemyCounts[enemyTypeIndex];
            
            if (enemyPrefab == null || enemyCount <= 0) continue;
            
            // 生成指定数量的该类型敌人
            for (int i = 0; i < enemyCount; i++)
            {
                if (!isSpawning) yield break;
                if (isWavePaused) yield break; // 小Boss出现，停止本波剩余生成

                SpawnEnemy(enemyPrefab);
                yield return new WaitForSeconds(wave.spawnInterval);
            }
        }
    }
    
    private System.Collections.IEnumerator SpawnTraditionalWave(SpawnWave wave)
    {
        // 传统的单敌人类型生成
        for (int i = 0; i < wave.enemyCount; i++)
        {
            if (!isSpawning) yield break;
            if (isWavePaused) yield break; // 小Boss出现，停止本波剩余生成

            SpawnEnemy(wave.enemyPrefab);
            yield return new WaitForSeconds(wave.spawnInterval);
        }
    }
    
    private System.Collections.IEnumerator ContinuousSpawn()
    {
        while (isSpawning)
        {
            // 小Boss战斗期间暂停连续生成
            if (isWavePaused)
            {
                yield return new WaitForSeconds(0.2f);
                continue;
            }

            if (activeEnemies.Count < maxEnemiesOnScreen && enemyPrefabs.Length > 0)
            {
                GameObject randomPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                SpawnEnemy(randomPrefab);
            }

            yield return new WaitForSeconds(continuousSpawnInterval);
        }
    }
    
    public void SpawnEnemy(GameObject enemyPrefab)
    {
        if (enemyPrefab == null || spawnPoints.Length == 0) return;
        
        // 选择一个随机生成点
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        
        // 在生成点周围添加一些随机偏移
        Vector3 spawnPosition = spawnPoint.position + 
            new Vector3(
                Random.Range(-2f, 2f),
                Random.Range(-2f, 2f),
                0f
            );
        
        // 生成敌人
        GameObject enemyObject = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        EnemyWithNewAttackSystem enemy = enemyObject.GetComponent<EnemyWithNewAttackSystem>();
        
        if (enemy != null)
        {
            // 订阅敌人事件
            enemy.OnEnemyDeath += OnEnemyDied;
            activeEnemies.Add(enemy);
            
            Debug.Log($"Spawned {enemy.name} at {spawnPosition} + {activeEnemies.Count}");
        }
    }
    
    // 修改OnEnemyDied方法，添加击杀记录和道具掉落
    private void OnEnemyDied(EnemyWithNewAttackSystem deadEnemy)
    {
        if (activeEnemies.Contains(deadEnemy))
        {
            activeEnemies.Remove(deadEnemy);
            Debug.Log($"Enemy {deadEnemy.name} died. Active enemies: {activeEnemies.Count}");
            
            // 记录击杀
            RecordEnemyKill(deadEnemy);
            
            // 尝试掉落道具
            if (PowerUpManager.Instance != null)
            {
                PowerUpManager.Instance.TryDropPowerUp(deadEnemy.transform.position);
            }
            
            // 检查是否所有敌人都被消灭
            CheckAllEnemiesDefeated();
        }
    }
    
    // 敌人组合管理方法
    public EnemyCombination GetEnemyCombination(int combinationId)
    {
        if (combinationId >= 0 && combinationId < enemyCombinations.Count)
        {
            return enemyCombinations[combinationId];
        }
        return null;
    }
    
    public void AddEnemyCombination(EnemyCombination combination)
    {
        if (combination != null && combination.IsValid())
        {
            enemyCombinations.Add(combination);
            Debug.Log($"Added enemy combination: {combination.combinationName}");
        }
    }
    
    public void RemoveEnemyCombination(int combinationId)
    {
        if (combinationId >= 0 && combinationId < enemyCombinations.Count)
        {
            string name = enemyCombinations[combinationId].combinationName;
            enemyCombinations.RemoveAt(combinationId);
            Debug.Log($"Removed enemy combination: {name}");
        }
    }
    
    public List<EnemyCombination> GetAllEnemyCombinations()
    {
        return new List<EnemyCombination>(enemyCombinations);
    }
    
    // 公共方法
    public void AddSpawnWave(SpawnWave wave)
    {
        GetCurrentLevel().spawnWaves.Add(wave);
    }
    
    public void ClearAllEnemies()
    {
        foreach (EnemyWithNewAttackSystem enemy in activeEnemies.ToArray())
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
        activeEnemies.Clear();
    }
    
    public int GetActiveEnemyCount()
    {
        return activeEnemies.Count;
    }
    
    public List<EnemyWithNewAttackSystem> GetActiveEnemies()
    {
        return new List<EnemyWithNewAttackSystem>(activeEnemies);
    }
    
    // 编辑器辅助方法
    [ContextMenu("Create Test Enemy Wave")]
    public void CreateTestEnemyWave()
    {
        SpawnWave testWave = new SpawnWave();
        testWave.waveName = "Test Enemy Wave";
        testWave.waveType = WaveType.SpawnEnemies;
        testWave.waitForClearField = false;
        testWave.useEnemyCombination = false;
        testWave.enemyCount = 5;
        testWave.spawnInterval = 1f;
        testWave.waveDelay = 3f;
        GetCurrentLevel().spawnWaves.Add(testWave);
    }

    [ContextMenu("Create Test Enemy Wave (Wait for Clear)")]
    public void CreateTestEnemyWaveWaitForClear()
    {
        SpawnWave testWave = new SpawnWave();
        testWave.waveName = "Test Enemy Wave (Wait for Clear)";
        testWave.waveType = WaveType.SpawnEnemies;
        testWave.waitForClearField = true;
        testWave.useEnemyCombination = false;
        testWave.enemyCount = 5;
        testWave.spawnInterval = 1f;
        testWave.waveDelay = 3f;
        GetCurrentLevel().spawnWaves.Add(testWave);
    }
    
    [ContextMenu("Create Test Combination")]
    public void CreateTestCombination()
    {
        EnemyCombination testCombination = new EnemyCombination();
        testCombination.combinationName = "Test Combination";
        testCombination.enemyPrefabs = new List<GameObject>();
        testCombination.enemyCounts = new List<int>();
        testCombination.spawnWeights = new List<float>();
        
        // 添加示例数据
        if (enemyPrefabs.Length > 0)
        {
            testCombination.enemyPrefabs.Add(enemyPrefabs[0]);
            testCombination.enemyCounts.Add(3);
            testCombination.spawnWeights.Add(1f);
        }
        
        enemyCombinations.Add(testCombination);
        Debug.Log("Created test enemy combination");
    }
    
    [ContextMenu("Create Test Think Wave")]
    public void CreateTestThinkWave()
    {
        SpawnWave testWave = new SpawnWave();
        testWave.waveName = "Test Think Wave";
        testWave.waveType = WaveType.Think;
        testWave.waitForClearField = false;
        testWave.thinkId = "thinking_1";
        testWave.thinkDisplayDuration = 3f;
        testWave.waveDelay = 1f;
        GetCurrentLevel().spawnWaves.Add(testWave);
    }

    [ContextMenu("Create Test Think Wave (Wait for Clear)")]
    public void CreateTestThinkWaveWaitForClear()
    {
        SpawnWave testWave = new SpawnWave();
        testWave.waveName = "Test Think Wave (Wait for Clear)";
        testWave.waveType = WaveType.Think;
        testWave.waitForClearField = true;
        testWave.thinkId = "thinking_1";
        testWave.thinkDisplayDuration = 3f;
        testWave.waveDelay = 1f;
        GetCurrentLevel().spawnWaves.Add(testWave);
    }

    [ContextMenu("Create Test Scene Change Wave")]
    public void CreateTestSceneChangeWave()
    {
        SpawnWave testWave = new SpawnWave();
        testWave.waveName = "Test Scene Change Wave";
        testWave.waveType = WaveType.ChangeScene;
        testWave.waitForClearField = false;
        testWave.sceneId = 0;
        testWave.waveDelay = 1f;
        GetCurrentLevel().spawnWaves.Add(testWave);
    }

    [ContextMenu("Create Test Scene Change Wave (Wait for Clear)")]
    public void CreateTestSceneChangeWaveWaitForClear()
    {
        SpawnWave testWave = new SpawnWave();
        testWave.waveName = "Test Scene Change Wave (Wait for Clear)";
        testWave.waveType = WaveType.ChangeScene;
        testWave.waitForClearField = true;
        testWave.sceneId = 0;
        testWave.waveDelay = 1f;
        GetCurrentLevel().spawnWaves.Add(testWave);
    }
    
    private void OnDrawGizmosSelected()
    {
        // 绘制生成点和生成半径
        if (playerCamera != null)
        {
            Gizmos.color = Color.yellow;
            // 使用 DrawWireSphere 替代 DrawWireCircle
            Gizmos.DrawWireSphere(playerCamera.transform.position, spawnRadius);
        }
        
        if (spawnPoints != null)
        {
            Gizmos.color = Color.red;
            foreach (Transform spawnPoint in spawnPoints)
            {
                if (spawnPoint != null)
                {
                    Gizmos.DrawWireSphere(spawnPoint.position, 1f);
                }
            }
        }
    }
    
    // Boss生成系统方法
    private System.Collections.IEnumerator CheckBossSpawnConditions()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f); // 每秒检查一次
            
            for (int i = 0; i < GetCurrentLevel().spawnBosses.Count; i++)
            {
                if (bossSpawnedFlags[i]) continue; // 如果已经生成，跳过
                
                SpawnBoss boss = GetCurrentLevel().spawnBosses[i];
                bool shouldSpawn = false;
                
                // 检查时间触发条件
                if (boss.SpawnAfterSomeTime)
                {
                    float elapsedTime = Time.time - gameStartTime;
                    if (elapsedTime >= boss.SpawnTime)
                    {
                        shouldSpawn = true;
                        Debug.Log($"Boss {i} triggered by time: {elapsedTime}s >= {boss.SpawnTime}s");
                    }
                }
                
                // 检查波次完成触发条件
                if (boss.SpawnAfterLastWave)
                {
                    if(allWavesCompleted)
                        Debug.Log(activeEnemies.Count);

                    if (allWavesCompleted && activeEnemies.Count == 0)
                    {
                        shouldSpawn = true;
                        Debug.Log($"Boss {i} triggered by last wave completion");
                    }
                }
                
                // 生成boss
                if (shouldSpawn)
                {
                    SpawnBoss(boss, i);
                    bossSpawnedFlags[i] = true;
                }
            }
        }
    }
    
    public void SpawnBoss(SpawnBoss bossConfig, int bossIndex)
    {
        if (bossConfig.bossPrefab == null || spawnPoints.Length == 0) return;
        
        BossWarning.SetActive(true);
        BossWarning.GetComponent<Animator>().Play("BossWarning_appear");

        // 启动协程，等待3秒后生成boss
        StartCoroutine(SpawnBossAfterDelay(bossConfig, bossIndex));
    }

    private IEnumerator SpawnBossAfterDelay(SpawnBoss bossConfig, int bossIndex)
    {
        // 等待3秒
        yield return new WaitForSeconds(3f);

        Vector3 spawnPosition = new Vector3(Random.Range(-3f, 3f) + 400,Random.Range(-3f, 3f) + 300,0);
        
        // 生成boss
        GameObject bossObject = Instantiate(bossConfig.bossPrefab, spawnPosition, Quaternion.identity * new Quaternion(0,180,0,0));
        BossController boss = bossObject.GetComponent<BossController>();
        
        if (boss != null)
        {
            // 订阅boss事件
            boss.OnBossDeath += OnBossDied;
            activeBosses.Add(boss);

            // 若为小Boss（非最终Boss），暂停波次生成直到其被击败
            if (!bossConfig.IsLastBoss)
            {
                isWavePaused = true;
                miniBosses.Add(boss);
                Debug.Log($"Mini-boss [{boss.name}] spawned, wave spawning paused.");
            }

            // 切换到Boss BGM
            if (bgmManager != null)
            {
                bgmManager.PlayBGM(BGMManager.BGMType.Boss);
                Debug.Log("切换到Boss BGM");
            }

            Debug.Log($"Spawned Boss {bossIndex} ({boss.name}) at {spawnPosition}");

            // 如果是最后一个boss，可以添加特殊逻辑
            if (bossConfig.IsLastBoss)
            {
                Debug.Log("Final boss has been spawned!");
                OnFinalBossSpawned();
            }
        }

        BossWarning.SetActive(false);
    }
    
    // 修改OnBossDied方法，添加Boss击杀记录和道具掉落
    private void OnBossDied(BossController deadBoss)
    {
        if (activeBosses.Contains(deadBoss))
        {
            activeBosses.Remove(deadBoss);
            Debug.Log($"Boss {deadBoss.name} died. Active bosses: {activeBosses.Count}");

            // 若为小Boss，从追踪集合移除；全部小Boss消灭后恢复波次生成
            if (miniBosses.Contains(deadBoss))
            {
                miniBosses.Remove(deadBoss);
                if (miniBosses.Count == 0)
                {
                    isWavePaused = false;
                    Debug.Log("All mini-bosses defeated, wave spawning resumed.");
                }
            }

            // 记录Boss击杀
            RecordBossKill(deadBoss);
            
            // Boss有更高的道具掉落率
            if (PowerUpManager.Instance != null)
            {
                // 尝试多次掉落（Boss掉落更多道具）
                for (int i = 0; i < 3; i++)
                {
                    Vector3 dropPos = deadBoss.transform.position + new Vector3(Random.Range(-2f, 2f), Random.Range(-2f, 2f), 0);
                    PowerUpManager.Instance.TryDropPowerUp(dropPos);
                }
            }
            
            // 检查是否所有boss都被击败
            CheckAllBossesDefeated();
            
            // 如果所有boss都被击败，切换回原来的BGM
            if (activeBosses.Count == 0 && bgmManager != null)
            {
                bgmManager.PlayBGM(previousBGMType);
                Debug.Log($"所有Boss被击败，切换回 {previousBGMType} BGM");
            }
        }
    }
        
    private void OnFinalBossSpawned()
    {
        // 最终boss生成时的特殊逻辑
        // 例如：播放特殊音效、显示UI提示等
        Debug.Log("The final boss has appeared! Prepare for the ultimate battle!");
    }

    /// <summary>
    /// 检查当前关卡是否完成
    /// </summary>
    private void CheckCurrentLevelCompleted()
    {
        LevelData currentLevel = GetCurrentLevel();
        if (currentLevel == null || currentLevelCompleted) return;

        // 检查是否所有普通敌人都被消灭
        bool allEnemiesDead = (activeEnemies.Count == 0);

        // 检查是否所有Boss都被消灭
        bool allBossesDead = (activeBosses.Count == 0);

        // 检查是否所有波次都已完成
        bool allWavesCompleted = this.allWavesCompleted;

        // 检查是否所有Boss都已经生成
        bool allBossesSpawned = AreAllCurrentLevelBossesSpawned();

        // 如果当前关卡所有敌人都被消灭，所有波次都完成，所有Boss都生成并被消灭
        if (allEnemiesDead && allBossesDead && allWavesCompleted && allBossesSpawned)
        {
            currentLevelCompleted = true;
            Debug.Log($"Level {currentLevelIndex + 1} completed! Triggering level completion...");


            OnLevelCompleted?.Invoke(currentLevelIndex);


            // autoStartNextLevel 为 true 时自动开始下一关卡，如果要添加关卡完成后的结算界面记得点掉
            if (autoStartNextLevel && HasNextLevel())
            {
                StartNextLevel();
            }
            else if (!HasNextLevel())
            {
                Debug.Log("All levels completed! Triggering game over...");
                OnAllEnemiesDefeated?.Invoke(); // 触发游戏结束
            }
        }
    }

    private bool AreAllCurrentLevelBossesSpawned()
    {
        for (int i = 0; i < bossSpawnedFlags.Count; i++)
        {
            if (!bossSpawnedFlags[i])
                return false;
        }
        return true;
    }

    /// <summary>
    /// 检查是否所有敌人都被消灭
    /// </summary>
    private void CheckAllEnemiesDefeated()
    {
        // 检查当前关卡是否完成
        CheckCurrentLevelCompleted();
    }
    
    /// <summary>
    /// 检查是否所有boss都被击败
    /// </summary>
    private void CheckAllBossesDefeated()
    {
        // 检查是否所有配置的boss都已经生成并被击败
        bool allBossesSpawned = AreAllCurrentLevelBossesSpawned();
        
        if (allBossesSpawned && activeBosses.Count == 0)
        {
            OnAllBossesDefeated();
        }
        
        // 检查当前关卡是否完成
        CheckCurrentLevelCompleted();
    }
    
    private void OnAllBossesDefeated()
    {
    }
    
    // Boss管理的公共方法
    public void ClearAllBosses()
    {
        foreach (BossController boss in activeBosses.ToArray())
        {
            if (boss != null)
            {
                Destroy(boss.gameObject);
            }
        }
        activeBosses.Clear();
    }
    
    public int GetActiveBossCount()
    {
        return activeBosses.Count;
    }
    
    public List<BossController> GetActiveBosses()
    {
        return new List<BossController>(activeBosses);
    }
    
    public bool AreAllBossesSpawned()
    {
        for (int i = 0; i < bossSpawnedFlags.Count; i++)
        {
            if (!bossSpawnedFlags[i])
                return false;
        }
        return true;
    }
    
    public bool AreAllBossesDefeated()
    {
        return AreAllBossesSpawned() && activeBosses.Count == 0;
    }
    
    // 手动触发boss生成（用于测试或特殊情况）
    public void ManuallySpawnBoss(int bossIndex)
    {
        if (bossIndex >= 0 && bossIndex < GetCurrentLevel().spawnBosses.Count && !bossSpawnedFlags[bossIndex])
        {
            SpawnBoss(GetCurrentLevel().spawnBosses[bossIndex], bossIndex);
            bossSpawnedFlags[bossIndex] = true;
        }
    }
    
    // 编辑器辅助方法

    private void InitializeCurrentLevelBossSystem()
    {
        LevelData currentLevel = GetCurrentLevel();
        if (currentLevel == null) return;
        
        // 初始化当前关卡的boss生成标志
        bossSpawnedFlags.Clear();
        for (int i = 0; i < currentLevel.spawnBosses.Count; i++)
        {
            bossSpawnedFlags.Add(false);
        }
        
        // 重新启动boss生成检查协程
        if (currentLevel.spawnBosses.Count > 0)
        {
            if (bossSpawnCoroutine != null)
            {
                StopCoroutine(bossSpawnCoroutine);
            }
            bossSpawnCoroutine = StartCoroutine(CheckCurrentLevelBossSpawnConditions());
        }
        
        Debug.Log($"Boss system initialized for level {currentLevelIndex + 1} with {currentLevel.spawnBosses.Count} bosses");
    }
    
    private System.Collections.IEnumerator SpawnCurrentLevelWaves()
    {
        LevelData currentLevel = GetCurrentLevel();
        if (currentLevel == null) yield break;
        
        // 生成当前关卡的所有波次
        for (int waveIndex = 0; waveIndex < currentLevel.spawnWaves.Count; waveIndex++)
        {
            currentWaveIndex = waveIndex;
            SpawnWave wave = currentLevel.spawnWaves[waveIndex];
            
            Debug.Log($"Starting wave {waveIndex + 1} of level {currentLevelIndex + 1}: {wave.waveName} (Type: {wave.waveType})");

            // 验证波次设置
            if (!wave.IsValid())
            {
                Debug.LogWarning($"Wave {waveIndex + 1} of level {currentLevelIndex + 1} is invalid, skipping...");
                continue;
            }

            // 若小Boss正在战斗，暂停直到其被击败
            yield return StartCoroutine(WaitWhileWavePaused());

            // 检查是否需要等待场景清空
            if (wave.waitForClearField)
            {
                Debug.Log($"Wave {waveIndex + 1} of level {currentLevelIndex + 1} waiting for clear field...");
                yield return StartCoroutine(WaitForClearField());
            }
            
            // 开始对话系统（如果有对话的话）
            Coroutine dialogueCoroutine = null;
            if (wave.GetValidDialogues().Count > 0)
            {
                dialogueCoroutine = StartCoroutine(ExecuteWaveDialogues(wave));
            }
            
            // 根据波次类型执行不同逻辑
            switch (wave.waveType)
            {
                case WaveType.SpawnEnemies:
                    // 显示波次警告
                    WaveWarning.SetActive(true);
                    WaveWarning.GetComponentInParent<Animator>().Play("BossWarning_appear");
                    WaveWarningText.text = $"Level {currentLevelIndex + 1} - Wave {waveIndex + 1}";
                    
                    // 生成这一波的敌人
                    if (wave.useEnemyCombination)
                    {
                        yield return StartCoroutine(SpawnEnemyCombination(wave));
                    }
                    else
                    {
                        yield return StartCoroutine(SpawnTraditionalWave(wave));
                    }
                    
                    // 等待波次间隔
                    yield return new WaitForSeconds(wave.waveDelay);
                    WaveWarning.SetActive(false);
                    break;
                    
                case WaveType.Think:
                    // 执行思考逻辑
                    yield return StartCoroutine(ExecuteThinkWave(wave));
                    break;
                    
                case WaveType.ChangeScene:
                    // 执行场景切换逻辑
                    yield return StartCoroutine(ExecuteSceneChangeWave(wave));
                    break;
                    
                case WaveType.ChangeBGM:
                    // 执行BGM切换逻辑
                    yield return StartCoroutine(ExecuteBGMChangeWave(wave));
                    break;
            }
            
            // 等待对话完成后再进入下一波次
            if (dialogueCoroutine != null)
            {
                yield return dialogueCoroutine;
            }
        }
        
        // 标记当前关卡所有波次完成
        allWavesCompleted = true;
        Debug.Log($"All spawn waves completed for level {currentLevelIndex + 1}!");
        
        // 如果不循环，开始连续生成
        if (isSpawning)
        {
            StartCoroutine(ContinuousSpawn());
        }
    }
    
    private System.Collections.IEnumerator CheckCurrentLevelBossSpawnConditions()
    {
        LevelData currentLevel = GetCurrentLevel();
        if (currentLevel == null) yield break;
        
        while (true)
        {
            yield return new WaitForSeconds(1f); // 每秒检查一次
            
            for (int i = 0; i < currentLevel.spawnBosses.Count; i++)
            {
                if (bossSpawnedFlags[i]) continue; // 如果已经生成，跳过
                
                SpawnBoss boss = currentLevel.spawnBosses[i];
                bool shouldSpawn = false;
                
                // 检查时间触发条件
                if (boss.SpawnAfterSomeTime)
                {
                    float elapsedTime = Time.time - gameStartTime;
                    if (elapsedTime >= boss.SpawnTime)
                    {
                        shouldSpawn = true;
                        Debug.Log($"Boss {i} of level {currentLevelIndex + 1} triggered by time: {elapsedTime}s >= {boss.SpawnTime}s");
                    }
                }
                
                // 检查波次完成触发条件
                if (boss.SpawnAfterLastWave)
                {
                    if (allWavesCompleted && activeEnemies.Count == 0)
                    {
                        shouldSpawn = true;
                        Debug.Log($"Boss {i} of level {currentLevelIndex + 1} triggered by last wave completion");
                    }
                }
                
                // 生成boss
                if (shouldSpawn)
                {
                    SpawnBoss(boss, i);
                    bossSpawnedFlags[i] = true;
                }
            }
        }
    }
    
    // 添加设置自动开始下一关卡的方法
    public void SetAutoStartNextLevel(bool autoStart)
    {
        autoStartNextLevel = autoStart;
        Debug.Log($"Auto start next level set to: {autoStart}");
    }

    /// <summary>
    /// 记录敌人击杀
    /// </summary>
    private void RecordEnemyKill(EnemyWithNewAttackSystem deadEnemy)
    {
        string enemyName = deadEnemy.name;
        int score = GetEnemyScore(deadEnemy);
        
        // 查找是否已有该敌人的记录
        KillRecord existingRecord = currentLevelKillRecords.Find(r => r.enemyName == enemyName);
        if (existingRecord != null)
        {
            existingRecord.killCount++;
        }
        else
        {
            currentLevelKillRecords.Add(new KillRecord(enemyName, 1, score));
        }
        
        Debug.Log($"Recorded kill: {enemyName}, Total kills: {GetKillCount(enemyName)}");
    }
    
    /// <summary>
    /// 记录Boss击杀
    /// </summary>
    private void RecordBossKill(BossController deadBoss)
    {
        string bossName = deadBoss.name;
        int score = GetBossScore(deadBoss);
        
        // 查找是否已有该Boss的记录
        KillRecord existingRecord = currentLevelKillRecords.Find(r => r.enemyName == bossName);
        if (existingRecord != null)
        {
            existingRecord.killCount++;
        }
        else
        {
            currentLevelKillRecords.Add(new KillRecord(bossName, 1, score));
        }
        
        Debug.Log($"Recorded boss kill: {bossName}, Total kills: {GetKillCount(bossName)}");
    }
    
    /// <summary>
    /// 获取敌人分数
    /// </summary>
    private int GetEnemyScore(EnemyWithNewAttackSystem enemy)
    {
        // 尝试从EnemyWithNewAttackSystem组件获取分数
        if (enemy != null)
        {
            return enemy.GetScore();
        }
        
        // 如果没有分数组件，返回默认分数
        return 10;
    }
    
    /// <summary>
    /// 获取Boss分数
    /// </summary>
    private int GetBossScore(BossController boss)
    {
        // 尝试从BossController组件获取分数
        if (boss != null)
        {
            return boss.GetScore();
        }
        
        // 如果没有分数组件，返回默认分数
        return 100;
    }
    
    /// <summary>
    /// 获取指定敌人的击杀数量
    /// </summary>
    private int GetKillCount(string enemyName)
    {
        KillRecord record = currentLevelKillRecords.Find(r => r.enemyName == enemyName);
        return record != null ? record.killCount : 0;
    }
    
    /// <summary>
    /// 获取当前关卡击杀记录
    /// </summary>
    public List<KillRecord> GetCurrentLevelKillRecords()
    {
        return new List<KillRecord>(currentLevelKillRecords);
    }
    
    /// <summary>
    /// 获取当前关卡总分数
    /// </summary>
    public int GetCurrentLevelTotalScore()
    {
        int totalScore = 0;
        foreach (KillRecord record in currentLevelKillRecords)
        {
            totalScore += record.GetTotalScore();
        }
        return totalScore;
    }
    
    /// <summary>
    /// 清空当前关卡击杀记录
    /// </summary>
    public void ClearCurrentLevelKillRecords()
    {
        currentLevelKillRecords.Clear();
        Debug.Log("Current level kill records cleared");
    }

    /// <summary>
    /// 执行思考波次
    /// </summary>
    private IEnumerator ExecuteThinkWave(SpawnWave wave)
    {
        Debug.Log($"Executing think wave: {wave.thinkId}");
        
        // 获取GameManager和ThinkUIController引用
        GameManager gameManager = FindObjectOfType<GameManager>();
        ThinkUIController thinkController = ThinkUIController.Instance;
        
        if (gameManager == null)
        {
            Debug.LogError("GameManager not found!");
            yield break;
        }
        
        if (thinkController == null)
        {
            Debug.LogError("ThinkUIController not found!");
            yield break;
        }
        
        // 1. 背景变黑
        gameManager.BackgroundChangeToBlackCurtain();
        
        // 等待背景变黑完成
        yield return new WaitForSeconds(1f);
        
        // 2. 显示思考内容
        thinkController.ShowThink(wave.thinkId);
        
        // 3. 等待思考内容显示完成
        // 等待思考内容显示完成（包括淡入、显示、淡出时间）
        float totalThinkTime = wave.thinkDisplayDuration + 0.3f + 0.3f; // 显示时间 + 淡入时间 + 淡出时间
        yield return new WaitForSeconds(totalThinkTime);
        
        // 4. 检查下一条波次是否也是Think类型
        bool nextWaveIsThink = IsNextWaveThink();
        
        // 5. 只有下一条波次不是Think类型时才退出黑屏
        if (!nextWaveIsThink)
        {
            // 背景变白
            gameManager.BackgroundChangeToWhiteCurtain();
            
            // 等待背景变白完成
            yield return new WaitForSeconds(1f);
            
            Debug.Log($"Think wave completed: {wave.thinkId} - Background restored to white");
        }
        else
        {
            Debug.Log($"Think wave completed: {wave.thinkId} - Keeping black background for next think wave");
        }
    }

    /// <summary>
    /// 检查下一条波次是否也是Think类型
    /// </summary>
    private bool IsNextWaveThink()
    {
        LevelData currentLevel = GetCurrentLevel();
        if (currentLevel == null) return false;
        
        // 检查是否还有下一条波次
        int nextWaveIndex = currentWaveIndex + 1;
        if (nextWaveIndex >= currentLevel.spawnWaves.Count)
        {
            return false; // 没有下一条波次了
        }
        
        // 获取下一条波次
        SpawnWave nextWave = currentLevel.spawnWaves[nextWaveIndex];
        
        // 检查下一条波次是否有效且为Think类型
        return nextWave.IsValid() && nextWave.waveType == WaveType.Think;
    }

    /// <summary>
    /// 执行场景切换波次
    /// </summary>
    private IEnumerator ExecuteSceneChangeWave(SpawnWave wave)
    {
        Debug.Log($"Executing scene change wave: Scene ID {wave.sceneId}");
        
        // 获取BackgroundCameraController引用
        BackgroundCameraController backgroundController = FindObjectOfType<BackgroundCameraController>();
        
        if (backgroundController == null)
        {
            Debug.LogError("BackgroundCameraController not found!");
            yield break;
        }
        
        // 切换场景
        backgroundController.SwitchToScene(wave.sceneId);
        
        // 等待场景切换完成
        yield return new WaitForSeconds(1f);
        
        Debug.Log($"Scene change wave completed: Scene ID {wave.sceneId}");
    }

    /// <summary>
    /// 执行BGM切换波次
    /// </summary>
    private IEnumerator ExecuteBGMChangeWave(SpawnWave wave)
    {
        Debug.Log($"Executing BGM change wave: {wave.bgmType}");
        
        // 获取BGMManager引用
        BGMManager bgmManager = BGMManager.Instance;
        if (bgmManager == null)
        {
            Debug.LogError("BGMManager not found! Cannot change BGM.");
            yield break;
        }
        
        // 切换BGM
        bgmManager.PlayBGM(wave.bgmType, wave.forceRestart);
        
        // 等待BGM切换完成（给淡入淡出时间）
        yield return new WaitForSeconds(1f);
        
        Debug.Log($"BGM change wave completed: {wave.bgmType}");
    }

    /// <summary>
    /// 等待小Boss战斗结束（波次暂停期间挂起）
    /// </summary>
    private IEnumerator WaitWhileWavePaused()
    {
        if (!isWavePaused) yield break;
        Debug.Log("Wave spawning paused: waiting for mini-boss to be defeated...");
        while (isWavePaused)
        {
            yield return new WaitForSeconds(0.2f);
        }
        Debug.Log("Mini-boss defeated, wave spawning resumed.");
    }

    /// <summary>
    /// 等待场景清空（没有敌人）
    /// </summary>
    private IEnumerator WaitForClearField()
    {
        Debug.Log("Waiting for clear field...");
        
        // 等待直到场景中没有敌人
        while (activeEnemies.Count > 0)
        {
            yield return new WaitForSeconds(0.5f); // 每0.5秒检查一次
        }
        
        Debug.Log("Field is clear, proceeding to next wave");
    }

    /// <summary>
    /// 执行波次对话
    /// </summary>
    private IEnumerator ExecuteWaveDialogues(SpawnWave wave)
    {
        List<Dialogue> validDialogues = wave.GetValidDialogues();
        if (validDialogues.Count == 0) yield break;
        
        Debug.Log($"Starting dialogues for wave: {wave.waveName}, Total dialogues: {validDialogues.Count}");
        
        // 获取SpeakUIController引用
        SpeakUIController speakController = SpeakUIController.Instance;
        if (speakController == null)
        {
            Debug.LogError("SpeakUIController not found! Cannot execute dialogues.");
            yield break;
        }
        
        // 按顺序执行每个对话
        foreach (Dialogue dialogue in validDialogues)
        {
            // 等待延迟时间
            if (dialogue.delay_time > 0f)
            {
                Debug.Log($"Waiting {dialogue.delay_time}s before showing dialogue: {dialogue.dialogue_id}");
                yield return new WaitForSeconds(dialogue.delay_time);
            }
            
            // 显示对话（使用自定义显示时长）
            Debug.Log($"Showing dialogue: {dialogue.dialogue_id} for {dialogue.duration_time}s");
            speakController.ShowTalk(dialogue.dialogue_id, dialogue.duration_time, true); // 使用自定义时长并自动隐藏
            
            // 等待对话显示时长
            yield return new WaitForSeconds(dialogue.duration_time);
            
            Debug.Log($"Dialogue completed: {dialogue.dialogue_id}");
        }
        
        Debug.Log($"All dialogues completed for wave: {wave.waveName}");
    }
    
    /// <summary>
    /// 由Boss调用：注册Boss生成的敌人到系统中
    /// </summary>
    /// <param name="enemy">要注册的敌人</param>
    public void RegisterBossSpawnedEnemy(EnemyWithNewAttackSystem enemy)
    {
        if (enemy == null) return;
        
        // 订阅敌人死亡事件
        enemy.OnEnemyDeath += OnEnemyDied;
        
        // 添加到活跃敌人列表
        if (!activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);
            Debug.Log($"Registered boss-spawned enemy {enemy.name}. Total active enemies: {activeEnemies.Count}");
        }
    }
    
    /// <summary>
    /// 由Boss调用：记录敌人击杀（在敌人死亡前调用，确保计入结算）
    /// </summary>
    /// <param name="enemy">被击杀的敌人</param>
    public void RecordBossEnemyKill(EnemyWithNewAttackSystem enemy)
    {
        if (enemy == null) return;
        
        RecordEnemyKill(enemy);
        Debug.Log($"Recorded boss enemy kill: {enemy.name}");
    }
    
    /// <summary>
    /// 由Boss调用：从活跃敌人列表中移除敌人（不触发死亡事件）
    /// </summary>
    /// <param name="enemy">要移除的敌人</param>
    public void RemoveBossSpawnedEnemy(EnemyWithNewAttackSystem enemy)
    {
        if (enemy == null) return;
        
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
            Debug.Log($"Removed boss-spawned enemy {enemy.name}. Active enemies: {activeEnemies.Count}");
        }
    }
} 
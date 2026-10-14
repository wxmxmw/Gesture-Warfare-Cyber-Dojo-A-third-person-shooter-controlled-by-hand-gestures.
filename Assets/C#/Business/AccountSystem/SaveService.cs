using System;
using System.IO;
using UnityEngine;

namespace Business.AccountSystem
{
    /// <summary>
    /// 玩家数据存储服务
    /// 负责读写 PlayerData JSON 文件到 Application.persistentDataPath
    /// 该路径在移动端和PC端均可持久化，打包后仍然有效
    /// </summary>
    public class SaveService
    {
        private const string DATA_FILE_NAME = "player_data.json";

        private readonly string _filePath;

        public SaveService()
        {
            _filePath = Path.Combine(Application.persistentDataPath, DATA_FILE_NAME);
        }

        /// <summary>
        /// 从本地文件加载玩家数据
        /// </summary>
        /// <returns>成功返回 PlayerData，文件不存在或解析失败返回 null</returns>
        public PlayerData Load()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    Debug.Log($"[SaveService] No save file found at: {_filePath}");
                    return null;
                }

                string json = File.ReadAllText(_filePath);
                PlayerData data = JsonUtility.FromJson<PlayerData>(json);

                if (data != null)
                {
                    Debug.Log($"[SaveService] Player data loaded. UserID: {data.userId}");
                    return data;
                }

                Debug.LogWarning("[SaveService] Loaded file but deserialization returned null.");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] Load failed: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 将玩家数据保存到本地文件
        /// </summary>
        public void Save(PlayerData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[SaveService] Attempted to save null PlayerData.");
                return;
            }

            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(_filePath, json);
                Debug.Log($"[SaveService] Player data saved to: {_filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] Save failed: {e.Message}");
            }
        }

        /// <summary>
        /// 检查是否存在已有存档
        /// </summary>
        public bool HasSavedData()
        {
            return File.Exists(_filePath);
        }
    }
}

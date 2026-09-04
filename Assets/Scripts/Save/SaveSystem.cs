using System;
using System.IO;
using UnityEngine;

namespace ApartmentAfterDark.Save
{
    /// <summary>
    /// Serializable data saved by the game.
    /// </summary>
    [Serializable]
    public struct GameSaveData
    {
        public Vector3 playerPosition;
        public float playerHealth;
        public int sceneIndex;
        public int saveVersion;
    }

    /// <summary>
    /// JSON save/load system. Writes compact JsonUtility files to
    /// Application.persistentDataPath/savegame.json.
    /// </summary>
    public static class SaveSystem
    {
        private const string FileName = "savegame.json";
        private const int CurrentVersion = 1;

        public static string SavePath
        {
            get { return Path.Combine(Application.persistentDataPath, FileName); }
        }

        public static bool SaveExists => File.Exists(SavePath);

        /// <summary>Saves the current game data. Returns true on success.</summary>
        public static bool SaveGame(GameSaveData data)
        {
            try
            {
                data.saveVersion = CurrentVersion;
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SavePath, json);
                Debug.Log($"[SaveSystem] Game saved to {SavePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveSystem] Failed to save: " + e.Message);
                return false;
            }
        }

        /// <summary>Loads saved game data. Returns false if none exists / corrupt.</summary>
        public static bool LoadGame(out GameSaveData data)
        {
            data = default;
            if (!SaveExists) return false;

            try
            {
                string json = File.ReadAllText(SavePath);
                data = JsonUtility.FromJson<GameSaveData>(json);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveSystem] Failed to load: " + e.Message);
                return false;
            }
        }

        public static void DeleteSave()
        {
            if (SaveExists) File.Delete(SavePath);
        }
    }
}

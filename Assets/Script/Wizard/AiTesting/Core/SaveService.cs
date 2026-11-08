using System;
using System.IO;
using UnityEngine;

namespace VonderGames.AiTesting.Core
{
    [Serializable]
    public sealed class SaveData
    {
        public int Version = 1;
        public bool ReduceShake;
        public bool ReduceFlash;
        public float BestSurvivalTime;
        public int BestKillCount;
        public int BossVictories;
    }

    public sealed class SaveService
    {
        private readonly string _path;
        public SaveData Data { get; private set; } = new SaveData();
        public string LastError { get; private set; }

        public SaveService(string path)
        {
            _path = path;
        }

        public void Load()
        {
            LastError = null;
            foreach (string candidate in new[] { _path, _path + ".bak" })
            {
                if (!File.Exists(candidate))
                    continue;

                try
                {
                    string json = File.ReadAllText(candidate);
                    var data = JsonUtility.FromJson<SaveData>(json);
                    if (!json.Contains("\"Version\"") || data == null || data.Version != 1 ||
                        float.IsNaN(data.BestSurvivalTime) || float.IsInfinity(data.BestSurvivalTime) ||
                        data.BestSurvivalTime < 0 || data.BestKillCount < 0 || data.BossVictories < 0)
                        throw new InvalidDataException("Unsupported or invalid save data.");

                    Data = data;
                    return;
                }
                catch (Exception exception) when (exception is IOException || exception is ArgumentException || exception is UnauthorizedAccessException)
                {
                    LastError = "Save could not be loaded. Using backup or defaults.";
                }
            }
        }

        public bool Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path));
                string temporary = _path + ".tmp";
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(JsonUtility.ToJson(Data, true));
                    writer.Flush();
                    stream.Flush(true);
                }
                if (File.Exists(_path))
                    File.Replace(temporary, _path, _path + ".bak");
                else
                    File.Move(temporary, _path);

                LastError = null;
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                LastError = "Could not save. Your current run can continue.";
                return false;
            }
        }
    }
}

using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

namespace TouchIT.Control
{
    [Serializable]
    public class StageRecord
    {
        public string SongTitle;
        public float BestCompletionRate; // 최고 달성률 (%)
    }

    [Serializable]
    public class SaveData
    {
        public List<StageRecord> Records = new List<StageRecord>();
    }

    public class SaveDataService
    {
        private string _path;
        private SaveData _data;

        public SaveDataService()
        {
            _path = Path.Combine(Application.persistentDataPath, "save_v1.json");
            Load();
        }

        public void SaveRecord(string title, float rate)
        {
            var record = _data.Records.Find(r => r.SongTitle == title);
            if (record == null)
            {
                record = new StageRecord { SongTitle = title, BestCompletionRate = 0f };
                _data.Records.Add(record);
            }

            // 최고 기록 갱신일 때만 저장
            if (rate > record.BestCompletionRate)
            {
                record.BestCompletionRate = rate;
                Save();
                Debug.Log($"💾 New Record Saved: {title} - {rate:F1}%");
            }
        }

        public float GetBestRate(string title)
        {
            var record = _data.Records.Find(r => r.SongTitle == title);
            return record != null ? record.BestCompletionRate : 0f;
        }

        private void Save()
        {
            string json = JsonUtility.ToJson(_data, true);
            File.WriteAllText(_path, json);
        }

        private void Load()
        {
            if (File.Exists(_path))
            {
                string json = File.ReadAllText(_path);
                _data = JsonUtility.FromJson<SaveData>(json);
            }
            else
            {
                _data = new SaveData();
            }
        }
    }
}
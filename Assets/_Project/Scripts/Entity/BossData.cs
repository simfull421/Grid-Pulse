using UnityEngine;

namespace TouchIT.Entity
{
    [System.Serializable]
    public class BossData
    {
        public string Name;
        public float MaxHp;
        public float CurrentHp;
        public int GroggyThreshold; // 그로기 상태가 되기 위한 피격 횟수

        public BossData(string name, float maxHp)
        {
            Name = name;
            MaxHp = maxHp;
            CurrentHp = maxHp;
            GroggyThreshold = 10; // 예: 10번 맞으면 그로기
        }
    }
}
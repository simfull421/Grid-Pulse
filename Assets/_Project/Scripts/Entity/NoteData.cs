using UnityEngine;

namespace TouchIT.Entity
{
    [System.Serializable]
    public struct NoteData
    {
        public int Id;
        public float SpawnTime;
        public float StartAngle; // 450도 (12시+한바퀴)
        public float Speed;
        public int SoundIndex;
        public NoteColor Color;
        public NoteType Type;
        public float HoldDuration; // 홀드 노트일 경우 길이

        public NoteData(int id, float time, float angle, float speed, int soundIdx, NoteColor color, NoteType type, float duration = 0f)
        {
            Id = id; SpawnTime = time; StartAngle = angle; Speed = speed;
            SoundIndex = soundIdx; Color = color; Type = type; HoldDuration = duration;
        }
    }
}
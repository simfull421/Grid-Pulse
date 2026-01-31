using UnityEngine;

namespace TouchIT.Entity
{
    public enum NoteType
    {
        Normal, // 일반 터치
        Bomb,   // 함정 (터치하면 안됨 - 나중에 추가 가능)
        Golden  // 점수 많이 주는 것
    }

    [System.Serializable]
    public struct NoteData
    {
        public int Id;              // 고유 ID
        public float SpawnTime;     // 생성 타이밍 (음악 박자 기준)
        public float StartAngle;    // 시작 각도 (0~360)
        public float Speed;         // 회전 속도
        public int SoundIndex;      // 재생할 음계 인덱스 (0:도, 1:레...)
        public NoteType Type;

        public NoteData(int id, float time, float angle, float speed, int soundIdx, NoteType type)
        {
            Id = id;
            SpawnTime = time;
            StartAngle = angle;
            Speed = speed;
            SoundIndex = soundIdx;
            Type = type;
        }
    }
}
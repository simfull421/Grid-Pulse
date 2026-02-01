using UnityEngine;
using System.Collections.Generic;

namespace TouchIT.Entity
{
    // 개별 패턴 정의 (하나의 리듬 덩어리)
    [System.Serializable]
    public class BeatPattern
    {
        public string PatternName; // 구분용 이름 (예: "Basic House", "Trap Roll")

        // 박자 간격 배열
        // 1.0 = 4분음표 (한 박자)
        // 0.5 = 8분음표 (반 박자)
        // 0.25 = 16분음표 (반의 반 박자)
        public float[] Intervals;   // 박자 간격
        public int[] SoundIndices;  // [신규] 악기 인덱스 (0:Kick, 1:Snare 등)
    }

    // 패턴들을 모아놓은 도서관 (이게 실제 파일로 생성됨)
    [CreateAssetMenu(fileName = "BeatLibrary", menuName = "TouchIT/BeatLibrary")]
    public class BeatLibrary : ScriptableObject
    {
        public List<BeatPattern> Patterns;

        // 랜덤으로 하나 뽑아주는 편의 함수
        public BeatPattern GetRandomPattern()
        {
            if (Patterns == null || Patterns.Count == 0) return null;
            return Patterns[Random.Range(0, Patterns.Count)];
        }
    }
}
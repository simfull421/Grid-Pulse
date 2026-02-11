using UnityEngine;
using System.Collections.Generic;

namespace TouchIT.Control
{
    public static class PatternRepository
    {
        // 메모리 할당 방지를 위해 미리 정의된 패턴들
        public static List<Vector2[]> Patterns = new List<Vector2[]>();

        static PatternRepository()
        {
            // 1. Z 모양 (기본)
            Patterns.Add(new Vector2[] { new(-1f, 1.5f), new(1f, 1.5f), new(-1f, -1.5f), new(1f, -1.5f) });
            // 2. ㄷ 모양
            Patterns.Add(new Vector2[] { new(1f, 1.5f), new(-1f, 1.5f), new(-1f, -1.5f), new(1f, -1.5f) });
            // 3. 번개 (ZigZag)
            Patterns.Add(new Vector2[] { new(0f, 2f), new(-1f, 0.5f), new(1f, -0.5f), new(0f, -2f) });
            // 4. 다이아몬드 (회전)
            Patterns.Add(new Vector2[] { new(0f, 1.5f), new(1.2f, 0f), new(0f, -1.5f), new(-1.2f, 0f), new(0f, 1.5f) });
            // 5. 직선 내려찍기
            Patterns.Add(new Vector2[] { new(0f, 2f), new(0f, 1f), new(0f, 0f), new(0f, -1f), new(0f, -2f) });
        }

        public static Vector2[] GetRandomPattern()
        {
            return Patterns[Random.Range(0, Patterns.Count)];
        }
    }
}
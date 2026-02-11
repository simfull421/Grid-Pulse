using UnityEngine;
using System.Collections.Generic;

namespace TouchIT.Control
{
    // 📱 스마트폰 잠금 해제 스타일 패턴 생성기
    public static class PatternGenerator
    {
        // 3x3 그리드 대신 좀 더 촘촘한 좌표 후보군 사용
        // (-1.5 ~ 1.5 범위)
        private static List<Vector2> _gridPoints = new List<Vector2>();

        static PatternGenerator()
        {
            // 3x4 그리드 포인트 미리 생성 (화면 비율 고려)
            for (float x = -1.5f; x <= 1.5f; x += 1.5f) // -1.5, 0, 1.5 (3열)
            {
                for (float y = -2.0f; y <= 2.0f; y += 1.0f) // -2, -1, 0, 1, 2 (5행)
                {
                    _gridPoints.Add(new Vector2(x, y));
                }
            }
        }

        // BFS/Random Walk로 이어진 선(패턴) 생성
        public static Vector2[] GenerateUnlockPattern(int length = 5)
        {
            List<Vector2> path = new List<Vector2>();
            List<Vector2> candidates = new List<Vector2>(_gridPoints);

            // 1. 시작점 랜덤 선택
            Vector2 current = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            path.Add(current);
            candidates.Remove(current);

            // 2. 다음 점 찾기 (너무 멀지 않은 곳으로)
            for (int i = 0; i < length - 1; i++)
            {
                // 현재 점과 거리가 적당한(너무 멀지도 가깝지도 않은) 후보 찾기
                // 1.0 ~ 2.5 사이 거리의 점들만 필터링
                List<Vector2> neighbors = candidates.FindAll(p =>
                {
                    float d = Vector2.Distance(current, p);
                    return d >= 1.0f && d <= 2.2f;
                });

                if (neighbors.Count == 0) break; // 갈 곳 없으면 종료

                // 랜덤 선택
                Vector2 next = neighbors[UnityEngine.Random.Range(0, neighbors.Count)];
                path.Add(next);
                candidates.Remove(next);
                current = next;
            }

            return path.ToArray();
        }
    }
}
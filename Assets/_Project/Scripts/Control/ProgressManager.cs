using UnityEngine;

namespace ReflexPuzzle.Control
{
    public static class ProgressManager
    {
        // 간단하게 PlayerPrefs 사용 (JSON보다 빠르고 간편함)
        private const string KEY_BEST_SCORE_CLASSIC = "Best_Classic";
        private const string KEY_BEST_SCORE_COLOR = "Best_Color";

        // 해금 조건 (기획)
        public const int UNLOCK_SCORE_MIXED = 1000;  // Classic이나 Color 1000점 넘으면 해금
        public const int UNLOCK_SCORE_MEMORY = 2000; // Mixed 2000점 넘으면 해금

        public static void SaveScore(Entity.GameMode mode, int score)
        {
            string key = $"Best_{mode}";
            int currentBest = PlayerPrefs.GetInt(key, 0);

            if (score > currentBest)
            {
                PlayerPrefs.SetInt(key, score);
                PlayerPrefs.Save();
            }
        }

        public static int GetBestScore(Entity.GameMode mode)
        {
            return PlayerPrefs.GetInt($"Best_{mode}", 0);
        }

        public static bool IsModeUnlocked(Entity.GameMode mode)
        {
            switch (mode)
            {
                case Entity.GameMode.Classic:
                case Entity.GameMode.Color:
                    return true; // 기본 해금

                case Entity.GameMode.Mixed:
                    // Classic이나 Color 중 하나라도 1000점 넘으면
                    return GetBestScore(Entity.GameMode.Classic) >= UNLOCK_SCORE_MIXED ||
                           GetBestScore(Entity.GameMode.Color) >= UNLOCK_SCORE_MIXED;

                case Entity.GameMode.Memory:
                    // Mixed 모드 2000점 넘으면 (예시)
                    return GetBestScore(Entity.GameMode.Mixed) >= UNLOCK_SCORE_MEMORY;

                default: return false;
            }
        }
    }
}
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using TouchIT.Entity;
using System.IO;

namespace TouchIT.Editor
{
    public class BeatGenerator
    {
        private const string ASSET_PATH = "Assets/_Project/Data/MainBeats.asset";

        [MenuItem("Tools/Generate Beat Library")]
        public static void Generate()
        {
            BeatLibrary lib = ScriptableObject.CreateInstance<BeatLibrary>();
            lib.Patterns = new List<BeatPattern>();

            // =========================================================
            // 🥁 [규칙] 0:Kick, 1:Snare, 2:Hihat, 3:Clap
            // =========================================================

            // --- 1. 기초 & 정박 (Warm-up) ---
            AddPattern(lib, "01. Basic House (4/4)",
                new float[] { 1f, 1f, 1f, 1f },
                new int[] { 0, 1, 0, 1 }); // 쿵 짝 쿵 짝

            AddPattern(lib, "02. Double Time",
                new float[] { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f },
                new int[] { 0, 2, 1, 2, 0, 2, 1, 2 }); // 쿵 츠 짝 츠...

            AddPattern(lib, "03. Slow Breath",
                new float[] { 2f, 2f, 2f },
                new int[] { 0, 3, 0 }); // 쿵... 박수... 쿵...

            AddPattern(lib, "04. Waltz (3/4)",
                new float[] { 1f, 1f, 1f, 1f, 1f, 1f },
                new int[] { 0, 2, 2, 0, 2, 2 }); // 쿵 츠 츠 쿵 츠 츠

            // --- 2. 리듬 변주 (Groove) ---
            AddPattern(lib, "05. Gallop",
                new float[] { 0.5f, 0.25f, 0.25f, 0.5f, 0.25f, 0.25f },
                new int[] { 0, 2, 2, 1, 2, 2 }); // 쿵 다닥 짝 다닥

            AddPattern(lib, "06. Reggaeton",
                new float[] { 0.75f, 0.25f, 0.5f, 0.5f, 0.75f, 0.25f, 0.5f, 0.5f },
                new int[] { 0, 2, 1, 0, 0, 2, 1, 0 }); // 쿵..치 짝 쿵

            AddPattern(lib, "07. Swing Triplet",
                new float[] { 0.66f, 0.33f, 0.66f, 0.33f, 0.66f, 0.33f },
                new int[] { 0, 2, 1, 2, 0, 2 }); // 쿵.츠 짝.츠

            AddPattern(lib, "08. Funky Syncopation",
                new float[] { 0.75f, 0.25f, 0.75f, 0.25f, 1f, 1f },
                new int[] { 0, 2, 0, 2, 1, 3 }); // 쿵 츠 쿵 츠 짝 박수

            AddPattern(lib, "09. Trap Roll",
                new float[] { 1f, 1f, 0.25f, 0.25f, 0.25f, 0.25f, 0.5f, 0.5f },
                new int[] { 0, 1, 2, 2, 2, 2, 3, 3 }); // 쿵 짝 츠츠츠츠 탁 탁

            // --- 3. 심리전 & 엇박 (Trick) ---
            AddPattern(lib, "10. Heartbeat",
                new float[] { 0.2f, 0.8f, 0.2f, 0.8f, 0.2f, 0.8f },
                new int[] { 0, 0, 0, 0, 0, 0 }); // 쿵쿵... 쿵쿵... (전부 킥)

            AddPattern(lib, "11. Double Tap Dash",
                new float[] { 0.2f, 0.2f, 0.6f, 0.2f, 0.2f, 0.6f },
                new int[] { 2, 2, 0, 2, 2, 1 }); // 츠츠 쿵... 츠츠 짝...

            AddPattern(lib, "12. The Pause",
                new float[] { 0.5f, 0.5f, 0.5f, 1.5f, 0.5f, 0.5f },
                new int[] { 0, 0, 0, 3, 1, 1 }); // 쿵 쿵 쿵 (쉼) 박수 짝 짝

            AddPattern(lib, "13. Polyrhythm (3vs2)",
                new float[] { 0.5f, 1f, 0.5f, 1f, 0.5f, 0.5f },
                new int[] { 0, 2, 0, 2, 1, 1 });

            AddPattern(lib, "14. Off-Beat Chaos",
                new float[] { 0.5f, 0.25f, 0.25f, 0.5f, 1.2f, 0.3f },
                new int[] { 0, 2, 3, 0, 1, 1 }); // 쿵 츠 탁 쿵... 짝 짝

            AddPattern(lib, "15. Morse Code",
                new float[] { 0.2f, 0.2f, 0.2f, 0.6f, 0.6f, 0.6f },
                new int[] { 2, 2, 2, 0, 0, 0 }); // 츠츠츠 쿵 쿵 쿵

            // --- 4. 가속 및 감속 ---
            AddPattern(lib, "16. Acceleration",
                new float[] { 1.0f, 0.8f, 0.6f, 0.4f, 0.2f },
                new int[] { 0, 0, 1, 1, 2 }); // 쿵... 쿵.. 짝. 짝 츠

            AddPattern(lib, "17. Deceleration",
                new float[] { 0.2f, 0.4f, 0.6f, 0.8f, 1.0f },
                new int[] { 2, 2, 1, 1, 0 });

            AddPattern(lib, "18. Rubber Band",
                new float[] { 0.2f, 0.4f, 0.8f, 0.4f, 0.2f },
                new int[] { 2, 1, 0, 1, 2 }); // 츠 짝 쿵 짝 츠

            // --- 5. 고난이도 (Expert) ---
            AddPattern(lib, "19. Fibonacci",
                new float[] { 0.1f, 0.1f, 0.2f, 0.3f, 0.5f, 0.8f },
                new int[] { 2, 2, 2, 3, 1, 0 });

            AddPattern(lib, "20. Stutter Step",
                new float[] { 0.1f, 0.1f, 0.8f, 0.1f, 0.1f, 0.8f },
                new int[] { 0, 1, 0, 0, 1, 0 }); // 쿵짝... 쿵짝...

            AddPattern(lib, "21. Machine Gun",
                new float[] { 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f },
                new int[] { 2, 2, 2, 2, 1, 1, 1, 1 }); // 츠츠츠츠 짝짝짝짝

            AddPattern(lib, "22. Broken Walz",
                new float[] { 1f, 0.5f, 1.5f, 1f, 0.5f, 1.5f },
                new int[] { 0, 2, 3, 0, 2, 3 });

            AddPattern(lib, "23. Prime Numbers",
                new float[] { 0.2f, 0.3f, 0.5f, 0.7f, 1.1f },
                new int[] { 2, 2, 1, 0, 3 });

            AddPattern(lib, "24. Dubstep Drop",
                new float[] { 1.5f, 1.5f, 0.25f, 0.25f, 0.25f, 0.25f },
                new int[] { 0, 1, 2, 2, 0, 1 }); // 쿵... 짝... 츠츠쿵짝

            AddPattern(lib, "25. Random Burst",
                new float[] { 0.2f, 1.2f, 0.2f, 0.2f, 1.5f, 0.2f },
                new int[] { 3, 0, 3, 3, 0, 2 });

            // --- 6. 긴 호흡 (Long) ---
            AddPattern(lib, "26. Standard Rock",
                new float[] { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.25f, 0.25f, 0.5f },
                new int[] { 0, 2, 1, 2, 0, 2, 0, 0, 1 });

            AddPattern(lib, "27. Build Up",
                new float[] { 0.5f, 0.5f, 0.25f, 0.25f, 0.25f, 0.25f, 0.12f, 0.12f, 0.12f, 0.12f },
                new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }); // 스네어 연타

            AddPattern(lib, "28. Three-Three-Two",
                new float[] { 0.75f, 0.75f, 0.5f, 0.75f, 0.75f, 0.5f },
                new int[] { 0, 0, 1, 0, 0, 1 });

            AddPattern(lib, "29. Missing Pulse",
                new float[] { 1f, 1f, 2f, 1f, 1f },
                new int[] { 0, 1, 3, 1, 0 });

            AddPattern(lib, "30. The Finale",
                new float[] { 0.5f, 0.5f, 0.5f, 0.25f, 0.25f, 0.1f, 0.1f, 0.1f, 0f },
                new int[] { 0, 1, 0, 3, 3, 2, 2, 2, 0 });


            // 폴더 및 파일 생성
            string folderPath = Path.GetDirectoryName(ASSET_PATH);
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            AssetDatabase.CreateAsset(lib, ASSET_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"✅ 비트 라이브러리(사운드 포함) 생성 완료! 위치: {ASSET_PATH}");
            Selection.activeObject = lib;
        }

        private static void AddPattern(BeatLibrary lib, string name, float[] intervals, int[] sounds)
        {
            BeatPattern pattern = new BeatPattern();
            pattern.PatternName = name;
            pattern.Intervals = intervals;
            pattern.SoundIndices = sounds;
            lib.Patterns.Add(pattern);
        }
    }
}
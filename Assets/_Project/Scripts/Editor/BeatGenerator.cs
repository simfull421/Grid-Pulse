using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using TouchIT.Entity;
using System.IO;

namespace TouchIT.Editor
{
    public class BeatGenerator
    {
        private const string ASSET_PATH = "Assets/_Project/Resources/Data/MainBeats.asset";

        // [설정] 게임의 기준 BPM (여기서 바꾸면 계산됨)
        private const float BPM = 110f;
        private const float BEAT_SEC = 60f / BPM; // 1박자 시간

        [MenuItem("Tools/Generate Beat Library")]
        public static void Generate()
        {
            BeatLibrary lib = ScriptableObject.CreateInstance<BeatLibrary>();
            lib.Patterns = new List<BeatPattern>();

            // =========================================================
            // 🥁 0:Kick, 1:Snare, 2:Hihat, 3:Clap
            // =========================================================
            // * 1.0f = 1박자 (4분음표)
            // * 0.5f = 반박자 (8분음표)
            // * 0.25f = 반의반박자 (16분음표)
            // =========================================================

            // --- 1. Basic (House / Pop) ---
            AddPattern(lib, "Basic 4/4",
                new float[] { 1f, 1f, 1f, 1f },
                new int[] { 0, 1, 0, 1 });

            // --- 2. Hip-hop (Boom Bap) ---
            // 쿵.. 쿵.짝.. 쿵.쿵.짝..
            AddPattern(lib, "Boom Bap Groove",
                new float[] { 1.5f, 0.5f, 1.0f, 0.5f, 0.5f },
                new int[] { 0, 0, 1, 0, 1 });

            // --- 3. Trap (Hi-hat Rolls) ---
            // 쪼개기 패턴 (0.25f 위주)
            AddPattern(lib, "Trap Hats",
                new float[] { 1f, 1f, 0.25f, 0.25f, 0.25f, 0.25f, 0.5f, 0.5f },
                new int[] { 0, 1, 2, 2, 2, 2, 3, 3 });

            // --- 4. Phonk (Cowbell Rhythm) ---
            // 엇박자가 중요함
            AddPattern(lib, "Phonk Drift",
                new float[] { 0.75f, 0.25f, 1.0f, 0.75f, 0.25f, 1.0f },
                new int[] { 0, 2, 1, 0, 2, 1 });

            // ... (기존 패턴들을 BEAT_SEC 곱하지 말고 '박자 단위'로 유지)
            // RhythmEngine에서 (60/BPM) * interval로 계산하므로 여기선 박자 비율만 넣으면 됨.

            // 파일 저장
            string folderPath = Path.GetDirectoryName(ASSET_PATH);
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            AssetDatabase.CreateAsset(lib, ASSET_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"✅ BPM {BPM} 기준 비트 라이브러리 생성 완료!");
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
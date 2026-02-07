using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using TouchIT.Entity;
using System.Numerics;
using System.Linq;

namespace TouchIT.Editor
{
    public class RhythmBaker : EditorWindow
    {
        private AudioClip _targetClip;
        private string _songTitle = "Overkill";
        private float _bpm = 174f;

        public enum DifficultyTarget
        {
            Easy = 300,
            Normal = 600,
            Hard = 850,
            Insane = 1200
        }
        private DifficultyTarget _targetDifficulty = DifficultyTarget.Hard;
        private float _minInterval = 0.1f;

        // 🛠️ [테스트용] 첫 노트 강제 홀드 체크박스
        private bool _debugForceFirstHold = false;

        [MenuItem("Tools/Rhythm Baker (Pro)")]
        public static void ShowWindow()
        {
            GetWindow<RhythmBaker>("Rhythm Baker");
        }

        private void OnGUI()
        {
            GUILayout.Label("🏆 Rhythm Baker Pro", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _targetClip = (AudioClip)EditorGUILayout.ObjectField("Clip", _targetClip, typeof(AudioClip), false);
            _songTitle = EditorGUILayout.TextField("Title", _songTitle);
            _bpm = EditorGUILayout.FloatField("BPM", _bpm);

            EditorGUILayout.Space();
            GUILayout.Label("🎯 Pattern Settings", EditorStyles.boldLabel);
            _targetDifficulty = (DifficultyTarget)EditorGUILayout.EnumPopup("Note Count", _targetDifficulty);
            _minInterval = EditorGUILayout.Slider("Min Interval (s)", _minInterval, 0.05f, 0.3f);

            // ✅ [추가] 디버깅 옵션
            EditorGUILayout.Space();
            GUILayout.Label("🧪 Debug Options", EditorStyles.boldLabel);
            _debugForceFirstHold = EditorGUILayout.Toggle("Force First Note HOLD", _debugForceFirstHold);
            if (_debugForceFirstHold)
            {
                EditorGUILayout.HelpBox("첫 번째 노트가 무조건 1.5초 길이의 홀드 노트로 생성됩니다.\n꼬리가 잘 그려지는지 테스트할 때 사용하세요.", MessageType.Warning);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("🔥 Bake Pattern", GUILayout.Height(40)))
            {
                if (_targetClip != null) BakeRankedPattern();
            }
        }

        private void BakeRankedPattern()
        {
            // 1. 기본 데이터 준비
            float[] samples = new float[_targetClip.samples * _targetClip.channels];
            _targetClip.GetData(samples, 0);
            int sampleRate = _targetClip.frequency;

            // 2. 분석 & 피크 추출
            List<float> flux = CalculateSpectralFlux(samples, _targetClip.channels, sampleRate);
            List<PeakInfo> allPeaks = FindAllPeaks(flux, sampleRate);

            // 3. 정렬 및 필터링 (랭킹 시스템)
            allPeaks.Sort((a, b) => b.Strength.CompareTo(a.Strength));
            List<NoteInfo> finalNotes = SelectTopPeaks(allPeaks, (int)_targetDifficulty);

            // 4. 시간순 정렬
            finalNotes.Sort((a, b) => a.Time.CompareTo(b.Time));

            // ✨ [핵심 1] 랜덤 홀드 변환 (Tasty Logic)
            ApplyRandomHolds(finalNotes);

            // ✨ [핵심 2] 디버그 강제 적용
            if (_debugForceFirstHold && finalNotes.Count > 0)
            {
                finalNotes[0].Type = NoteType.Hold;
                finalNotes[0].Duration = 1.5f; // 1.5초 동안 길게
                Debug.Log("🧪 Debug: First note forced to HOLD (1.5s)");
            }

            SaveAsset(finalNotes);
        }

        // 🎲 랜덤 홀드 변환 함수 (사용자 요청 로직)
        // 두 노트가 가까이 붙어있으면, 확률적으로 앞 노트를 홀드로 바꿔서 연결해버림
        private void ApplyRandomHolds(List<NoteInfo> notes)
        {
            float step4 = 60f / _bpm;        // 1박자 (4분음표)
            float step8 = step4 / 2f;        // 반박자 (8분음표)
            float holdConnectThreshold = step8 * 1.2f; // 8분음표 정도 거리면 연결 시도

            // 리스트를 수정해야 하므로 루프 주의
            // 뒤쪽 노트가 삭제될 수 있으므로 i는 천천히 증가
            for (int i = 0; i < notes.Count - 1; i++)
            {
                NoteInfo current = notes[i];
                NoteInfo next = notes[i + 1];

                float gap = next.Time - current.Time;

                // 조건 1: 다음 노트와의 거리가 가까움 (연타 구간)
                // 조건 2: 너무 짧진 않음 (0.1초 미만은 그냥 연타로 두는게 타격감 좋음)
                if (gap <= holdConnectThreshold && gap > 0.1f)
                {
                    // 🎲 확률: 40% 확률로 연결 (너무 많이 연결되면 지루함)
                    if (Random.value < 0.4f)
                    {
                        // 앞 노트를 홀드로 변환
                        current.Type = NoteType.Hold;
                        current.Duration = gap; // 딱 다음 노트 시간까지

                        // 🔥 중요: 뒤에 있는 노트(next)는 삭제해야 '연결된' 느낌이 남
                        // (삭제 안 하면 홀드 끝나는 순간에 또 노트가 있어서 겹침)
                        notes.RemoveAt(i + 1);

                        // 뒤 노트를 지웠으므로 인덱스 유지 (다음 루프에서 새로운 i+1과 검사)
                        i--;
                    }
                }
            }
        }

        // --- (아래는 기존 로직과 동일) ---

        private void SaveAsset(List<NoteInfo> notes)
        {
            string path = $"Assets/_Project/Resources/MusicData/{_songTitle}.asset";
            var data = AssetDatabase.LoadAssetAtPath<MusicData>(path);
            if (data == null) { data = CreateInstance<MusicData>(); AssetDatabase.CreateAsset(data, path); }

            data.Title = _songTitle;
            data.Clip = _targetClip;
            data.BPM = _bpm;
            data.Notes = notes;

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Debug.Log($"✅ BAKER: Generated {notes.Count} notes.");
        }

        private class PeakInfo { public float Time; public float Strength; }

        private List<PeakInfo> FindAllPeaks(List<float> flux, int sampleRate)
        {
            List<PeakInfo> peaks = new List<PeakInfo>();
            float timePerFlux = 512f / sampleRate;
            for (int i = 1; i < flux.Count - 1; i++)
            {
                if (flux[i] > flux[i - 1] && flux[i] > flux[i + 1] && flux[i] > 0.01f)
                    peaks.Add(new PeakInfo { Time = i * timePerFlux, Strength = flux[i] });
            }
            return peaks;
        }

        private List<NoteInfo> SelectTopPeaks(List<PeakInfo> sortedPeaks, int targetCount)
        {
            List<NoteInfo> selected = new List<NoteInfo>();
            float beatDuration = 60f / _bpm;
            float snapUnit = beatDuration / 4f;

            foreach (var peak in sortedPeaks)
            {
                if (selected.Count >= targetCount) break;
                float snappedTime = Mathf.Round(peak.Time / snapUnit) * snapUnit;

                bool isTooClose = false;
                foreach (var existing in selected)
                {
                    if (Mathf.Abs(existing.Time - snappedTime) < _minInterval) { isTooClose = true; break; }
                }

                if (!isTooClose)
                {
                    selected.Add(new NoteInfo { Time = snappedTime, Type = NoteType.Tap, LaneIndex = Random.Range(0, 32) });
                }
            }
            return selected;
        }

        private List<float> CalculateSpectralFlux(float[] samples, int channels, int sampleRate)
        {
            int fftSize = 1024; int hopSize = 512;
            int totalSamples = samples.Length / channels; int numWindows = totalSamples / hopSize;
            List<float> fluxList = new List<float>();
            float[] prevSpectrum = new float[fftSize / 2];
            int minIndex = Mathf.Clamp((int)(20 * fftSize / sampleRate), 0, fftSize / 2);
            int maxIndex = Mathf.Clamp((int)(300 * fftSize / sampleRate), minIndex + 1, fftSize / 2);

            for (int i = 0; i < numWindows; i++)
            {
                int startIdx = i * hopSize; Complex[] buffer = new Complex[fftSize];
                for (int j = 0; j < fftSize; j++)
                {
                    int idx = startIdx + j; float val = 0f;
                    if (idx < totalSamples) val = (channels == 2) ? (samples[idx * 2] + samples[idx * 2 + 1]) * 0.5f : samples[idx];
                    val *= 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * j / (fftSize - 1)));
                    buffer[j] = new Complex(val, 0);
                }
                FFT(buffer);
                float flux = 0f;
                for (int k = minIndex; k < maxIndex; k++)
                {
                    float mag = (float)buffer[k].Magnitude; float diff = mag - prevSpectrum[k];
                    if (diff > 0) flux += diff; prevSpectrum[k] = mag;
                }
                fluxList.Add(flux);
            }
            return fluxList;
        }

        private void FFT(Complex[] x)
        {
            int N = x.Length; if (N <= 1) return;
            Complex[] even = new Complex[N / 2]; Complex[] odd = new Complex[N / 2];
            for (int i = 0; i < N / 2; i++) { even[i] = x[2 * i]; odd[i] = x[2 * i + 1]; }
            FFT(even); FFT(odd);
            for (int k = 0; k < N / 2; k++)
            {
                double theta = -2 * System.Math.PI * k / N; Complex t = Complex.Exp(new Complex(0, theta)) * odd[k];
                x[k] = even[k] + t; x[k + N / 2] = even[k] - t;
            }
        }
    }
}
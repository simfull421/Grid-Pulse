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

        // 🎯 목표: 정확히 이 개수만큼만 뽑습니다.
        public enum DifficultyTarget
        {
            Easy = 300,
            Normal = 600,
            Hard = 850,    // Overkill 추천
            Insane = 1200
        }
        private DifficultyTarget _targetDifficulty = DifficultyTarget.Hard;

        // 🛑 최소 간격 (초 단위) - BPM 174 기준 16분 음표 = 약 0.086초
        // 너무 짧으면 연타가 되어버리니 0.1s 정도로 제한
        private float _minInterval = 0.1f;

        [MenuItem("Tools/Rhythm Baker (Rank & Prune)")]
        public static void ShowWindow()
        {
            GetWindow<RhythmBaker>("Rank Baker");
        }

        private void OnGUI()
        {
            GUILayout.Label("🏆 Rank & Prune Baker (Fail-safe)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _targetClip = (AudioClip)EditorGUILayout.ObjectField("Clip", _targetClip, typeof(AudioClip), false);
            _songTitle = EditorGUILayout.TextField("Title", _songTitle);
            _bpm = EditorGUILayout.FloatField("BPM", _bpm);

            EditorGUILayout.Space();
            GUILayout.Label("🎯 Target Settings", EditorStyles.boldLabel);
            _targetDifficulty = (DifficultyTarget)EditorGUILayout.EnumPopup("Note Count", _targetDifficulty);
            _minInterval = EditorGUILayout.Slider("Min Interval (s)", _minInterval, 0.05f, 0.3f);

            EditorGUILayout.HelpBox($"전체 곡에서 에너지가 가장 강한 상위 {(int)_targetDifficulty}개의 타격점만 추출합니다.\n3500개가 생성되는 일은 절대 없습니다.", MessageType.Info);

            EditorGUILayout.Space();
            if (GUILayout.Button("🔥 Generate Exact Pattern", GUILayout.Height(40)))
            {
                if (_targetClip != null) BakeRankedPattern();
            }
        }

        private void BakeRankedPattern()
        {
            // 1. 오디오 데이터 준비
            float[] samples = new float[_targetClip.samples * _targetClip.channels];
            _targetClip.GetData(samples, 0);
            int sampleRate = _targetClip.frequency;

            // 2. Flux 계산 (저음 집중)
            List<float> flux = CalculateSpectralFlux(samples, _targetClip.channels, sampleRate);

            // 3. 모든 피크(Local Maxima) 찾기
            List<PeakInfo> allPeaks = FindAllPeaks(flux, sampleRate);

            // 4. ✨ [핵심] 에너지 순으로 정렬 (내림차순)
            // 가장 센 소리부터 1등, 2등, ... 줄 세우기
            allPeaks.Sort((a, b) => b.Strength.CompareTo(a.Strength));

            // 5. ✨ [핵심] 상위 N개 추출 (거리 제한 적용)
            List<NoteInfo> finalNotes = SelectTopPeaks(allPeaks, (int)_targetDifficulty);

            // 6. 시간순 정렬 및 저장
            finalNotes.Sort((a, b) => a.Time.CompareTo(b.Time));

            // ✨ [신규] 홀드 노트 후처리 (Post-Processing)
            ConvertGapsToHolds(finalNotes);
            SaveAsset(finalNotes);
        }
        // 🛠️ 홀드 노트 변환 함수 (클래스 내부에 추가)
        private void ConvertGapsToHolds(List<NoteInfo> notes)
        {
            // BPM에 따른 기준 시간 계산
            // 예: 174 BPM 기준, 1박자(4분음표) = 약 0.34초
            // 0.4초 이상 비어있으면 홀드로 채우기
            float holdThreshold = 0.4f;

            for (int i = 0; i < notes.Count - 1; i++)
            {
                NoteInfo current = notes[i];
                NoteInfo next = notes[i + 1];

                float gap = next.Time - current.Time;

                // 다음 노트까지 거리가 너무 멀면 -> 홀드로 변환!
                // 단, 너무 멀면(2초 이상) 그냥 쉬는 게 나을 수도 있으니 최대치(2.0f) 제한
                if (gap > holdThreshold && gap < 2.0f)
                {
                    current.Type = NoteType.Hold;
                    // 다음 노트 0.1초 전까지만 닿게 (겹침 방지)
                    current.Duration = gap - 0.1f;
                }
            }
        }
        // 피크 정보 구조체
        private class PeakInfo
        {
            public float Time;
            public float Strength;
        }

        // 🔍 모든 국소 피크 찾기 (개수 제한 없음)
        private List<PeakInfo> FindAllPeaks(List<float> flux, int sampleRate)
        {
            List<PeakInfo> peaks = new List<PeakInfo>();
            float timePerFlux = 512f / sampleRate;

            for (int i = 1; i < flux.Count - 1; i++)
            {
                // 나보다 양옆이 작으면 피크 (Local Maximum)
                if (flux[i] > flux[i - 1] && flux[i] > flux[i + 1])
                {
                    // 너무 작은 잡음은 1차 필터링 (0.01 이하)
                    if (flux[i] > 0.01f)
                    {
                        peaks.Add(new PeakInfo { Time = i * timePerFlux, Strength = flux[i] });
                    }
                }
            }
            return peaks;
        }

        // 🏆 상위 N개 선발 (거리두기 포함)
        private List<NoteInfo> SelectTopPeaks(List<PeakInfo> sortedPeaks, int targetCount)
        {
            List<NoteInfo> selected = new List<NoteInfo>();

            // BPM 기반 스냅 단위
            float beatDuration = 60f / _bpm;
            float snapUnit = beatDuration / 4f; // 16분 음표

            foreach (var peak in sortedPeaks)
            {
                if (selected.Count >= targetCount) break; // 목표 달성 시 즉시 종료

                // 퀀타이즈 (박자 맞추기)
                float snappedTime = Mathf.Round(peak.Time / snapUnit) * snapUnit;

                // 🛡️ 거리 제한 (Pruning)
                // 이미 선발된 노트들과 비교해서 너무 가까우면 탈락
                bool isTooClose = false;
                foreach (var existing in selected)
                {
                    if (Mathf.Abs(existing.Time - snappedTime) < _minInterval)
                    {
                        isTooClose = true;
                        break;
                    }
                }

                if (!isTooClose)
                {
                    selected.Add(new NoteInfo
                    {
                        Time = snappedTime,
                        Type = NoteType.Tap,
                        LaneIndex = Random.Range(0, 32)
                    });
                }
            }
            return selected;
        }

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
            Debug.Log($"✅ EXACT BAKER: Generated {notes.Count} notes (Target: {(int)_targetDifficulty})");
        }

        // --- (Flux 계산 로직은 기존과 동일) ---
        private List<float> CalculateSpectralFlux(float[] samples, int channels, int sampleRate)
        {
            int fftSize = 1024;
            int hopSize = 512;
            int totalSamples = samples.Length / channels;
            int numWindows = totalSamples / hopSize;

            List<float> fluxList = new List<float>();
            float[] prevSpectrum = new float[fftSize / 2];

            // 20~300Hz (저음 집중)
            int minIndex = Mathf.Clamp((int)(20 * fftSize / sampleRate), 0, fftSize / 2);
            int maxIndex = Mathf.Clamp((int)(300 * fftSize / sampleRate), minIndex + 1, fftSize / 2);

            for (int i = 0; i < numWindows; i++)
            {
                int startIdx = i * hopSize;
                Complex[] buffer = new Complex[fftSize];
                for (int j = 0; j < fftSize; j++)
                {
                    int idx = startIdx + j;
                    float val = 0f;
                    if (idx < totalSamples)
                        val = (channels == 2) ? (samples[idx * 2] + samples[idx * 2 + 1]) * 0.5f : samples[idx];
                    val *= 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * j / (fftSize - 1)));
                    buffer[j] = new Complex(val, 0);
                }
                FFT(buffer);
                float flux = 0f;
                for (int k = minIndex; k < maxIndex; k++)
                {
                    float mag = (float)buffer[k].Magnitude;
                    float diff = mag - prevSpectrum[k];
                    if (diff > 0) flux += diff;
                    prevSpectrum[k] = mag;
                }
                fluxList.Add(flux);
            }
            return fluxList;
        }

        private void FFT(Complex[] x)
        {
            int N = x.Length;
            if (N <= 1) return;
            Complex[] even = new Complex[N / 2];
            Complex[] odd = new Complex[N / 2];
            for (int i = 0; i < N / 2; i++) { even[i] = x[2 * i]; odd[i] = x[2 * i + 1]; }
            FFT(even); FFT(odd);
            for (int k = 0; k < N / 2; k++)
            {
                double theta = -2 * System.Math.PI * k / N;
                Complex t = Complex.Exp(new Complex(0, theta)) * odd[k];
                x[k] = even[k] + t;
                x[k + N / 2] = even[k] - t;
            }
        }
    }
}
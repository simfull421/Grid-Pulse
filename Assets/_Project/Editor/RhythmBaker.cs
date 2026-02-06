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

        [Header("🎛️ Analysis Settings")]
        private int _lowFreq = 20;
        private int _highFreq = 250;
        private float _sensitivity = 1.5f;

        // 🎼 [핵심] 검증된 리듬 패턴 족보 (Static Patterns)
        // true: 노트 허용, false: 강제 휴식 (쉼표)
        // 16분 음표 기준 4개 (1박자) 패턴들
        private static readonly List<bool[]> _chillPatterns = new List<bool[]>
        {
            new bool[] { true, false, false, false }, // 쿵 . . . (4분음표)
            new bool[] { false, false, false, false }, // . . . . (완전 휴식)
        };

        private static readonly List<bool[]> _dropPatterns = new List<bool[]>
        {
            new bool[] { true, false, true, false },  // 쿵 . 짝 . (8분음표)
            new bool[] { true, true, false, true },   // 쿵 쿵 . 짝 (변칙)
            new bool[] { true, true, true, false },   // 쿵 쿵 쿵 . (3연타 후 휴식)
            new bool[] { true, false, true, true },   // 쿵 . 짝 짝
            new bool[] { true, true, true, true },    // 쿵 쿵 쿵 쿵 (풀 스트림 - 가끔만 나오게)
        };

        [MenuItem("Tools/Rhythm Baker (Pattern Matcher)")]
        public static void ShowWindow()
        {
            GetWindow<RhythmBaker>("Pattern Baker");
        }

        private void OnGUI()
        {
            GUILayout.Label("🧠 Pattern Matching Baker", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _targetClip = (AudioClip)EditorGUILayout.ObjectField("Clip", _targetClip, typeof(AudioClip), false);
            _songTitle = EditorGUILayout.TextField("Title", _songTitle);
            _bpm = EditorGUILayout.FloatField("BPM", _bpm);

            EditorGUILayout.HelpBox("곡을 구간별로 분석하고, '검증된 패턴' 마스크를 씌워\n무의미한 난사를 방지하고 리듬감을 부여합니다.", MessageType.Info);

            if (GUILayout.Button("🔥 Generate Adaptive Pattern", GUILayout.Height(40)))
            {
                if (_targetClip == null) return;
                BakeAdaptivePattern();
            }
        }

        private void BakeAdaptivePattern()
        {
            // 1. 데이터 준비
            string path = $"Assets/_Project/Resources/MusicData/{_songTitle}.asset";
            var data = AssetDatabase.LoadAssetAtPath<MusicData>(path);
            if (data == null) { data = CreateInstance<MusicData>(); AssetDatabase.CreateAsset(data, path); }

            data.Title = _songTitle;
            data.Clip = _targetClip;
            data.BPM = _bpm;
            data.Notes = new List<NoteInfo>();

            // 2. 오디오 샘플 추출
            float[] samples = new float[_targetClip.samples * _targetClip.channels];
            _targetClip.GetData(samples, 0);

            // 3. ✨ [Layer 1] 구간 에너지 분석 (Segmentation)
            List<float> segmentEnergies = AnalyzeSegments(samples, _targetClip.channels, _targetClip.frequency);
            float avgSongEnergy = segmentEnergies.Average();

            // 4. ✨ [Layer 2] 물리적 Onset 검출
            List<float> rawOnsets = GetRawOnsets(samples, _targetClip.channels, _targetClip.frequency);

            // 5. ✨ [Layer 3] 패턴 매칭 & 필터링
            float secondsPerBar = (60f / _bpm) * 4f; // 4/4박자 기준 1마디 시간
            float step16 = (60f / _bpm) / 4f;        // 16분 음표 시간

            // 마디(Bar) 단위로 순회
            int totalBars = segmentEnergies.Count;
            for (int barIdx = 0; barIdx < totalBars; barIdx++)
            {
                float barStartTime = barIdx * secondsPerBar;
                float currentEnergy = segmentEnergies[barIdx];

                // 테마 결정 (Chill vs Drop)
                bool isDrop = currentEnergy > avgSongEnergy * 1.1f; // 평균보다 1.1배 시끄러우면 Drop

                // 이 마디에 적용할 패턴 골라오기 (4박자 = 16개 스텝)
                bool[] barMask = GenerateBarMask(isDrop);

                // 마디 내부 16스텝 순회
                for (int step = 0; step < 16; step++)
                {
                    float stepTime = barStartTime + (step * step16);

                    // 🛡️ 1. 패턴 마스크 체크 (음악적으로 허용된 자리인가?)
                    if (!barMask[step]) continue; // 패턴상 쉼표면 무조건 스킵 (휴식 보장)

                    // 🛡️ 2. 물리적 신호 체크 (실제로 소리가 났는가?)
                    // 해당 스텝 시간 근처(±0.05초)에 Raw Onset이 있는지 확인
                    if (HasOnsetNearby(rawOnsets, stepTime, 0.05f))
                    {
                        // 합격! 노트 생성
                        if (!data.Notes.Exists(n => Mathf.Abs(n.Time - stepTime) < 0.01f))
                        {
                            data.Notes.Add(new NoteInfo
                            {
                                Time = stepTime,
                                Type = NoteType.Tap,
                                LaneIndex = Random.Range(0, 32)
                            });
                        }
                    }
                }
            }

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Debug.Log($"✅ Adaptive Pattern Generated! ({data.Notes.Count} notes)");
        }

        // 🎹 마디별 패턴 마스크 생성기
        private bool[] GenerateBarMask(bool isDrop)
        {
            List<bool> fullBarMask = new List<bool>();

            // 1마디 = 4박자
            for (int beat = 0; beat < 4; beat++)
            {
                bool[] selectedPattern;

                if (isDrop)
                {
                    // Drop이면 복잡한 패턴 중 랜덤 선택
                    selectedPattern = _dropPatterns[Random.Range(0, _dropPatterns.Count)];
                }
                else
                {
                    // Chill이면 단순한 패턴 선택
                    selectedPattern = _chillPatterns[Random.Range(0, _chillPatterns.Count)];
                }

                fullBarMask.AddRange(selectedPattern);
            }
            return fullBarMask.ToArray();
        }

        // 📊 구간 에너지 분석 (RMS)
        private List<float> AnalyzeSegments(float[] samples, int channels, int sampleRate)
        {
            List<float> energies = new List<float>();
            float secondsPerBar = (60f / _bpm) * 4f;
            int samplesPerBar = Mathf.FloorToInt(secondsPerBar * sampleRate * channels);

            for (int i = 0; i < samples.Length; i += samplesPerBar)
            {
                float sum = 0;
                int count = 0;
                for (int j = 0; j < samplesPerBar && (i + j) < samples.Length; j++)
                {
                    sum += samples[i + j] * samples[i + j];
                    count++;
                }
                energies.Add(Mathf.Sqrt(sum / count)); // RMS
            }
            return energies;
        }

        // 🔍 물리적 Onset 찾기 (기존 로직 재사용 + 최적화)
        private List<float> GetRawOnsets(float[] samples, int channels, int sampleRate)
        {
            // (이전의 CalculateFocusedSpectralFlux + DetectOnsets 로직 호출)
            // 코드가 길어지므로 여기서는 간략화하여 호출 구조만 잡습니다.
            // 실제 구현 시 이전 코드의 Flux 계산 로직을 그대로 가져오시면 됩니다.

            var flux = CalculateFocusedSpectralFlux(samples, channels, sampleRate);
            return DetectOnsets(flux, sampleRate);
        }

        private bool HasOnsetNearby(List<float> onsets, float time, float tolerance)
        {
            // 이분 탐색 등으로 최적화 가능하지만, 에디터 툴이므로 단순 루프도 OK
            // 리스트가 정렬되어 있다고 가정
            foreach (var onset in onsets)
            {
                if (Mathf.Abs(onset - time) <= tolerance) return true;
                if (onset > time + tolerance) break; // 시간 지나면 조기 종료
            }
            return false;
        }

        // --- (이전 코드의 Flux 계산 로직 복붙 필요) ---
        // CalculateFocusedSpectralFlux, DetectOnsets, FFT 함수는 
        // 이전 답변의 코드와 동일하게 유지하세요.

        // ... [Insert CalculateFocusedSpectralFlux Here] ...
        // ... [Insert DetectOnsets Here] ...
        // ... [Insert FFT Here] ...

        // (편의를 위해 생략했지만, 실제 코드에는 꼭 포함되어야 작동합니다!)

        // ---- 아래는 복사 붙여넣기용 생략된 함수들 (참조용) ----
        private List<float> CalculateFocusedSpectralFlux(float[] samples, int channels, int sampleRate)
        {
            // 이전 답변의 "주파수 집중형" Flux 계산 로직을 여기에 넣으세요.
            // 저음역대(20~250Hz)만 타겟팅하는 것이 중요합니다.
            int fftSize = 1024;
            int hopSize = 512;
            int totalSamples = samples.Length / channels;
            int numWindows = totalSamples / hopSize;

            List<float> fluxList = new List<float>();
            float[] prevSpectrum = new float[fftSize / 2];

            int minIndex = Mathf.FloorToInt(_lowFreq * fftSize / (float)sampleRate);
            int maxIndex = Mathf.CeilToInt(_highFreq * fftSize / (float)sampleRate);

            minIndex = Mathf.Clamp(minIndex, 0, fftSize / 2);
            maxIndex = Mathf.Clamp(maxIndex, minIndex + 1, fftSize / 2);

            for (int i = 0; i < numWindows; i++)
            {
                int startIdx = i * hopSize;
                Complex[] buffer = new Complex[fftSize];

                for (int j = 0; j < fftSize; j++)
                {
                    int currentSampleIdx = startIdx + j;
                    float val = 0f;
                    if (currentSampleIdx < totalSamples)
                        val = (channels == 2) ? (samples[currentSampleIdx * 2] + samples[currentSampleIdx * 2 + 1]) * 0.5f : samples[currentSampleIdx];

                    float window = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * j / (fftSize - 1)));
                    buffer[j] = new Complex(val * window, 0);
                }

                FFT(buffer);

                float flux = 0f;
                for (int k = minIndex; k < maxIndex; k++)
                {
                    float magnitude = (float)buffer[k].Magnitude;
                    float diff = magnitude - prevSpectrum[k];
                    if (diff > 0) flux += diff;
                    prevSpectrum[k] = magnitude;
                }
                fluxList.Add(flux);
            }
            return fluxList;
        }

        private List<float> DetectOnsets(List<float> flux, int sampleRate)
        {
            List<float> onsets = new List<float>();
            int windowSize = 7;
            float timePerHop = 512f / sampleRate;

            for (int i = windowSize; i < flux.Count - windowSize; i++)
            {
                float sum = 0f;
                for (int w = -windowSize; w <= windowSize; w++) sum += flux[i + w];
                float avg = sum / (windowSize * 2 + 1);

                if (flux[i] > avg * _sensitivity + 0.1f) // delta 0.1 고정
                {
                    if (flux[i] > flux[i - 1] && flux[i] > flux[i + 1])
                        onsets.Add(i * timePerHop);
                }
            }
            return onsets;
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
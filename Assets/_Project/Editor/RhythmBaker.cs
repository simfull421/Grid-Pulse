using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using TouchIT.Entity;
using System.Numerics; // ⚠️ Project Settings > Player > API Compatibility Level 확인 (.NET Standard 2.1)
using System.Linq;

namespace TouchIT.Editor
{
    public class RhythmBaker : EditorWindow
    {
        private AudioClip _targetClip;
        private string _songTitle = "NewSong";
        private float _bpm = 120f;

        // 🧮 알고리즘 파라미터 (공식 C, delta)
        private float _sensitivityC = 1.3f; // C: 민감도 (평균보다 1.3배 커야 함)
        private float _thresholdDelta = 0.05f; // delta: 최소 에너지

        [MenuItem("Tools/Rhythm Baker (Flux)")]
        public static void ShowWindow()
        {
            GetWindow<RhythmBaker>("Rhythm Baker");
        }

        private void OnGUI()
        {
            GUILayout.Label("🎵 Spectral Flux Analyzer", EditorStyles.boldLabel);
            _targetClip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", _targetClip, typeof(AudioClip), false);
            _songTitle = EditorGUILayout.TextField("Song Title", _songTitle);
            _bpm = EditorGUILayout.FloatField("BPM", _bpm);
            _sensitivityC = EditorGUILayout.Slider("Sensitivity (C)", _sensitivityC, 1.0f, 2.0f);
            _thresholdDelta = EditorGUILayout.Slider("Threshold (delta)", _thresholdDelta, 0.01f, 0.2f);

            if (GUILayout.Button("🔥 Bake with Math!", GUILayout.Height(40)))
            {
                if (_targetClip == null) return;
                BakePattern();
            }
        }

        private void BakePattern()
        {
            // 1. 데이터 준비
            string path = $"Assets/_Project/Resources/MusicData/{_songTitle}.asset";
            var data = AssetDatabase.LoadAssetAtPath<MusicData>(path);
            if (data == null)
            {
                data = CreateInstance<MusicData>();
                AssetDatabase.CreateAsset(data, path);
            }

            data.Title = _songTitle;
            data.Clip = _targetClip;
            data.BPM = _bpm;
            data.Notes = new List<NoteInfo>();

            // 2. 오디오 데이터 추출
            float[] samples = new float[_targetClip.samples * _targetClip.channels];
            _targetClip.GetData(samples, 0);

            // 3. 알고리즘 실행
            List<float> spectralFlux = CalculateSpectralFlux(samples, _targetClip.channels);
            List<float> onsets = DetectOnsets(spectralFlux, _targetClip.frequency);

            // 4. 노트 배치
            foreach (float time in onsets)
            {
                // 퀀타이즈 (16분 음표 단위 스냅)
                float beatDuration = 60f / _bpm;
                float snapUnit = beatDuration / 4f;
                float snappedTime = Mathf.Round(time / snapUnit) * snapUnit;

                // 중복 방지
                if (!data.Notes.Exists(n => Mathf.Abs(n.Time - snappedTime) < 0.01f))
                {
                    data.Notes.Add(new NoteInfo
                    {
                        Time = snappedTime,
                        Type = NoteType.Tap, // 기본 탭
                        LaneIndex = Random.Range(0, 32)
                    });
                }
            }

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Debug.Log($"✅ Generated {data.Notes.Count} notes based on Spectral Flux!");
        }

        // 📐 Step 2: 스펙트럼 플럭스 계산
        private List<float> CalculateSpectralFlux(float[] samples, int channels)
        {
            int fftSize = 1024;
            int hopSize = 512; // 50% overlap
            int numWindows = samples.Length / hopSize;

            List<float> fluxList = new List<float>();
            float[] prevSpectrum = new float[fftSize / 2]; // 이전 프레임 크기

            for (int i = 0; i < numWindows - 1; i++)
            {
                int startIdx = i * hopSize;
                if (startIdx + fftSize >= samples.Length) break;

                // 윈도우링 + 모노 변환
                Complex[] buffer = new Complex[fftSize];
                for (int j = 0; j < fftSize; j++)
                {
                    float val = (channels == 2)
                        ? (samples[(startIdx + j) * 2] + samples[(startIdx + j) * 2 + 1]) * 0.5f
                        : samples[startIdx + j];

                    // Hanning Window
                    float window = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * j / (fftSize - 1)));
                    buffer[j] = new Complex(val * window, 0);
                }

                // FFT
                FFT(buffer);

                // Flux 계산 (Half-Wave Rectification)
                float flux = 0f;
                for (int k = 0; k < fftSize / 2; k++)
                {
                    float magnitude = (float)buffer[k].Magnitude;
                    float diff = magnitude - prevSpectrum[k];
                    if (diff > 0) flux += diff; // 양수일 때만 합산

                    prevSpectrum[k] = magnitude;
                }
                fluxList.Add(flux);
            }
            return fluxList;
        }

        // 📐 Step 3: 발병점 검출 (Thresholding)
        private List<float> DetectOnsets(List<float> flux, int sampleRate)
        {
            List<float> onsets = new List<float>();
            int windowSize = 7; // Moving Average 윈도우 크기 (좌우 7개)
            float timePerHop = 512f / sampleRate;

            for (int i = windowSize; i < flux.Count - windowSize; i++)
            {
                // 지역 평균 (Moving Average)
                float sum = 0f;
                for (int w = -windowSize; w <= windowSize; w++) sum += flux[i + w];
                float avg = sum / (windowSize * 2 + 1);

                // 임계값 판정: Flux > Average * C + delta
                if (flux[i] > avg * _sensitivityC + _thresholdDelta)
                {
                    // 피크 검사 (Local Maximum)
                    if (flux[i] > flux[i - 1] && flux[i] > flux[i + 1])
                    {
                        onsets.Add(i * timePerHop);
                    }
                }
            }
            return onsets;
        }

        // 📐 Helper: FFT (Cooley-Tukey Recursive)
        private void FFT(Complex[] x)
        {
            int N = x.Length;
            if (N <= 1) return;

            Complex[] even = new Complex[N / 2];
            Complex[] odd = new Complex[N / 2];
            for (int i = 0; i < N / 2; i++)
            {
                even[i] = x[2 * i];
                odd[i] = x[2 * i + 1];
            }

            FFT(even);
            FFT(odd);

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
using UnityEngine;

namespace TouchIT.Boundary
{
    public class AudioPulseSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private LifeRingView _lifeRingView;

        [Header("Settings")]
        // [중요] 감도를 엄청 높게 잡으세요. 모바일/에디터 볼륨이 작을 수 있습니다.
        [SerializeField] private float _sensitivity = 60.0f;
        [SerializeField] private float _threshold = 0.02f;   // 거의 모든 비트 감지

        private float[] _spectrum = new float[64];
        private float _beatTimer;

        private void Start()
        {
            if (!_musicSource) _musicSource = FindFirstObjectByType<AudioSource>();
            if (!_lifeRingView) _lifeRingView = FindFirstObjectByType<LifeRingView>();
        }

        private void Update()
        {
            if (!_musicSource || !_musicSource.isPlaying || !_lifeRingView) return;

            _beatTimer += Time.deltaTime;

            _musicSource.GetSpectrumData(_spectrum, 0, FFTWindow.Rectangular);

            // 저음역 (Kick/Bass) 에너지 합산
            float bassEnergy = 0f;
            for (int i = 0; i < 5; i++) bassEnergy += _spectrum[i];

            float power = bassEnergy * _sensitivity;

            // 쿨다운을 0.08초로 줄여서 빠른 비트에도 반응하게 함
            if (_beatTimer > 0.08f && power > _threshold)
            {
                _lifeRingView.ApplyAudioImpulse(power);
                _beatTimer = 0f;
            }
        }
    }
}
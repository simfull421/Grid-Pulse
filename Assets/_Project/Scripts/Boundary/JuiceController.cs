using UnityEngine;

namespace TouchIT.Boundary
{
    public class JuiceController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Renderer _sphereRenderer;
        [SerializeField] private LifeRingView _lifeRingView; // [변경] Transform 대신 스크립트 연결
        [SerializeField] private AudioSource _musicSource;

        [Header("Settings")]
        [SerializeField] private float _musicSensitivity = 5.0f; // 민감도

        private float[] _spectrum = new float[64];
        private Material _sphereMat;
        private float _currentWobble;

        private void Start()
        {
            if (_sphereRenderer) _sphereMat = _sphereRenderer.material;
        }

        private void Update()
        {
            // 1. 오디오 분석
            float bassEnergy = 0f;
            if (_musicSource && _musicSource.isPlaying)
            {
                _musicSource.GetSpectrumData(_spectrum, 0, FFTWindow.Rectangular);
                // 저음역대 (Kick, Bass) 에너지 추출
                for (int i = 0; i < 10; i++) bassEnergy += _spectrum[i];
            }

            // 2. 구체 쉐이더 제어 (Jelly)
            if (_sphereMat)
            {
                _currentWobble = Mathf.Lerp(_currentWobble, 0.1f, Time.deltaTime * 2f); // 기본 꿀렁임
                _sphereMat.SetFloat("_WobbleAmount", _currentWobble + (bassEnergy * 0.5f));
                _sphereMat.SetFloat("_WobbleSpeed", 5.0f + (bassEnergy * 10f));
            }

            // 3. [수정됨] 링 부분 왜곡 (Distortion) 제어
            if (_lifeRingView)
            {
                // 음악 에너지에 따라 링의 튀어나옴 강도를 전달
                _lifeRingView.ApplyAudioImpulse(bassEnergy * _musicSensitivity);
            }
        }

        public void PunchSphere()
        {
            // 터치 성공 시 구체가 확 찌그러짐
            _currentWobble = 0.5f;
        }
    }
}
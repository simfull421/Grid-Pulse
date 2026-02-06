using UnityEngine;
using DG.Tweening;

namespace TouchIT.Control
{
    public class AudioManager : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private AudioSource _sfxSource;

        // ⏱️ [핵심 수정] 노래 시작 시간 기록용 변수
        private double _songStartTime;
        private bool _isBgmPlaying = false;

        public void Initialize()
        {
            if (_bgmSource == null) _bgmSource = gameObject.AddComponent<AudioSource>();
            if (_sfxSource == null) _sfxSource = gameObject.AddComponent<AudioSource>();

            _bgmSource.loop = false; // 리듬게임은 보통 루프 안 함
            _bgmSource.playOnAwake = false;
        }

        public void PlayBGM(AudioClip clip, float fadeDuration = 0.5f)
        {
            if (clip == null) return;

            // 이미 같은 곡 재생 중이면 패스하되, 시간은 리셋해야 할 수도 있음 (여기선 생략)

            _bgmSource.clip = clip;
            _bgmSource.volume = 1.0f; // DOTween 없이 즉시 재생 테스트 권장 (버그 배제)

            // [중요] 재생 직전 시간 기록
            _songStartTime = AudioSettings.dspTime;
            _bgmSource.Play();
            _isBgmPlaying = true;
        }

        public void StopBGM(float fadeDuration = 0.5f)
        {
            _bgmSource.Stop();
            _isBgmPlaying = false;
        }

        // ⏱️ [핵심 수정] 현재 흐른 시간 = (시스템시간 - 노래시작시간)
        public double GetAudioTime()
        {
            if (!_isBgmPlaying) return 0.0;
            return AudioSettings.dspTime - _songStartTime;
        }

        public void ResumeBGM()
        {
            if (_bgmSource != null && !_bgmSource.isPlaying)
            {
                // 일시정지 후 복귀 시 시간 보정 로직이 필요하지만
                // 지금은 일단 timeSamples 기반이나 단순 재개로 둡니다.
                // 완벽한 싱크를 위해선 Pause 시점의 dspTime 차이를 저장해야 합니다.
                // 여기서는 단순하게 다시 계산
                _songStartTime = AudioSettings.dspTime - _bgmSource.time;
                _bgmSource.Play();
                _isBgmPlaying = true;
            }
        }
    }
}
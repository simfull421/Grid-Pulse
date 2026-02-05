using UnityEngine;
using DG.Tweening;

namespace TouchIT.Control
{
    public class AudioManager : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private AudioSource _sfxSource;

        [Header("Settings")]
        [SerializeField] private float _defaultVolume = 0.8f;

        public void Initialize()
        {
            if (_bgmSource == null) _bgmSource = gameObject.AddComponent<AudioSource>();
            if (_sfxSource == null) _sfxSource = gameObject.AddComponent<AudioSource>();

            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;
            _sfxSource.loop = false;
        }

        public void PlayBGM(AudioClip clip, float fadeDuration = 0.5f)
        {
            if (clip == null) return;
            if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

            // ✅ [수정] DOTween.To 사용 (안정성 확보)
            Sequence seq = DOTween.Sequence();

            if (_bgmSource.isPlaying)
            {
                // 볼륨 0으로 줄이기
                seq.Append(DOTween.To(() => _bgmSource.volume, x => _bgmSource.volume = x, 0f, fadeDuration));
            }

            seq.AppendCallback(() =>
            {
                _bgmSource.clip = clip;
                _bgmSource.volume = 0f; // 시작은 0부터
                _bgmSource.Play();
            });

            // 볼륨 다시 올리기
            seq.Append(DOTween.To(() => _bgmSource.volume, x => _bgmSource.volume = x, _defaultVolume, fadeDuration));
        }

        public void StopBGM(float fadeDuration = 0.5f)
        {
            // ✅ [수정] DOTween.To 사용
            DOTween.To(() => _bgmSource.volume, x => _bgmSource.volume = x, 0f, fadeDuration)
                .OnComplete(() => _bgmSource.Stop());
        }

        public void PlaySFX(AudioClip clip, float volume = 1.0f)
        {
            if (clip == null) return;
            _sfxSource.PlayOneShot(clip, volume);
        }
        // AudioManager 클래스 내부
        public void ResumeBGM()
        {
            if (_bgmSource != null && !_bgmSource.isPlaying)
            {
                _bgmSource.UnPause(); // 또는 Play()
            }
        }
        public double GetAudioTime() => AudioSettings.dspTime;
        public AudioSource GetBGMSource() => _bgmSource;
    }
}
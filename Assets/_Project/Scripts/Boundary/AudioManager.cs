using UnityEngine;
using TouchIT.Control;
using TouchIT.Entity; // NoteColor 사용

namespace TouchIT.Boundary
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour, IAudioManager
    {
        [Header("Sources")]
        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private AudioSource _sfxSource;

        [Header("Clips")]
        [SerializeField] private AudioClip _whiteBgm;
        [SerializeField] private AudioClip _blackBgm;
        [SerializeField] private AudioClip _hitClip;
        [SerializeField] private AudioClip _missSfx;
        [SerializeField] private AudioClip _swipeSfx;

        // 엔진용 드럼 사운드
        [SerializeField] private AudioClip _kickClip;
        [SerializeField] private AudioClip _snareClip;

        public void Initialize()
        {
            _bgmSource.loop = true;
        }

        public void PlaySfx(string name)
        {
            // [수정] 피치(음정)를 0.9 ~ 1.1 사이로 랜덤하게 주면 훨씬 자연스러움 (버블 소리 등)
            _sfxSource.pitch = Random.Range(0.9f, 1.1f);

            AudioClip clip = null;
            switch (name)
            {
                case "Hit": clip = _hitClip; break;
                case "Miss": clip = _missSfx; break;
                case "Swipe": clip = _swipeSfx; break;
            }

            if (clip != null)
            {
                _sfxSource.PlayOneShot(clip);
            }
        }

        public void PlayDrum(bool isKick)
        {
            // [중요] 드럼은 피치가 바뀌면 이상하므로 1.0으로 리셋
            _sfxSource.pitch = 1.0f;
            _sfxSource.PlayOneShot(isKick ? _kickClip : _snareClip);
        }

        public void SetBgmTheme(NoteColor theme)
        {
            AudioClip target = (theme == NoteColor.White) ? _whiteBgm : _blackBgm;

            // 이미 같은 곡이 재생 중이면 끊지 않음
            if (_bgmSource.clip != target)
            {
                _bgmSource.Stop();
                _bgmSource.clip = target;
                _bgmSource.Play();
            }
        }
    }
}
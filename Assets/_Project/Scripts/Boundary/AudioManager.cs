using UnityEngine;
using TouchIT.Control;
using TouchIT.Entity; // NoteColor 사용

namespace TouchIT.Boundary
{
    public class AudioManager : MonoBehaviour, IAudioManager
    {
        [Header("Sources")]
        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private AudioSource _sfxSource;

        [Header("Common Clips")]
        [SerializeField] private AudioClip _whiteBgm;
        [SerializeField] private AudioClip _blackBgm;
        [SerializeField] private AudioClip _missSfx;
        [SerializeField] private AudioClip _swipeSfx;

        [Header("Combo Scale Clips (8 Notes)")]
        // 도, 레, 미, 파, 솔, 라, 시, 도(높은) 순서로 할당하세요.
        [SerializeField] private AudioClip[] _comboClips;

        public void Initialize()
        {
            _bgmSource.loop = true;
        }

        public void PlaySfx(string name, int comboCount = 0)
        {
            _sfxSource.pitch = 1.0f; // 피치는 정배속 고정

            AudioClip clip = null;

            if (name == "Hit")
            {
                // [신규] 8음계 로직
                // 콤보 10개당 1음계 상승 (0~9: 도, 10~19: 레 ...)
                if (_comboClips != null && _comboClips.Length > 0)
                {
                    // 인덱스 계산 (최대값은 배열 길이 - 1)
                    int index = Mathf.Clamp(comboCount / 5, 0, _comboClips.Length - 1);
                    clip = _comboClips[index];
                }
            }
            else
            {
                switch (name)
                {
                    case "Miss": clip = _missSfx; break;
                    case "Swipe": clip = _swipeSfx; break;
                }
            }

            if (clip != null) _sfxSource.PlayOneShot(clip);
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
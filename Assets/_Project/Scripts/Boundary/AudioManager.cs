using TouchIT.Control;
using TouchIT.Entity; // NoteColor
using UnityEngine;

namespace TouchIT.Boundary
{
    public class AudioManager : MonoBehaviour, IAudioManager
    {
        [Header("Sources")]
        [SerializeField] private AudioSource _bgmSource; // [중요] 여기에 음악 파일(Clip)이 들어있어야 함
        [SerializeField] private AudioSource _sfxSource;

        [Header("Clips")]
        [SerializeField] private AudioClip _missSfx;
        [SerializeField] private AudioClip[] _comboClips; // 도레미파솔라시도

        public void Initialize()
        {
            _bgmSource.loop = true;

            // [수정] 테마 상관없이 그냥 BGM 재생
            if (!_bgmSource.isPlaying)
            {
                _bgmSource.Play();
            }
        }

        public void PlaySfx(string name, int comboCount = 0)
        {
            // ... (기존 SFX 로직 유지) ...
            _sfxSource.pitch = 1.0f;
            AudioClip clip = null;

            if (name == "Hit")
            {
                if (_comboClips != null && _comboClips.Length > 0)
                {
                    int index = Mathf.Clamp(comboCount % 8, 0, _comboClips.Length - 1);
                    clip = _comboClips[index];
                }
            }
            else if (name == "Miss") clip = _missSfx;

            if (clip != null) _sfxSource.PlayOneShot(clip);
        }

        // [수정] 인터페이스 요구사항 때문에 남겨두지만, 내용은 비움 (단일 테마)
        public void SetBgmTheme(NoteColor theme)
        {
            // 더 이상 BGM을 교체하지 않음
        }
    }
}
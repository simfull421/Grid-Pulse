using UnityEngine;
using System.Collections.Generic;
using TouchIT.Entity;
using TouchIT.Control;

namespace TouchIT.Boundary
{
    [RequireComponent(typeof(AudioSource))]
    public class GameBinder : MonoBehaviour, IGameView, IAudioManager
    {
        [Header("Control")]
        [SerializeField] private GameMain _gameMain;

        [Header("Views")]
        [SerializeField] private LifeRingView _lifeRingView;
        [SerializeField] private SphereView _sphereView;
        [SerializeField] private Transform _noteContainer;
        [SerializeField] private NoteView _notePrefab;
        [SerializeField] private SphereInteraction _sphereInteraction; // 구체 흔들기

        // =========================================================
        // [수정] 오디오 클립 교체 (음계 -> 비트 & 타격음)
        // =========================================================
        [Header("Audio Sources")]
        [SerializeField] private AudioSource _bgmSource; // [신규] 배경음악 전용 (Loop 체크 필수)
        [SerializeField] private AudioSource _sfxSource; // 효과음 전용 (OneShot)

        [Header("BGM Loops")]
        [SerializeField] private AudioClip _whiteBgm; // 4비트 심플
        [SerializeField] private AudioClip _blackBgm; // 8비트 퉁퉁 (맛도리)
        [Header("Base Beats (Auto)")]
        [SerializeField] private AudioClip _kickClip;  // 쿵
        [SerializeField] private AudioClip _snareClip; // 짝

        [Header("Player Sounds (Touch)")]
        [SerializeField] private AudioClip _hitClip;   // 틱/탁 (노트 파괴음)
        [SerializeField] private AudioClip _missSfx;
        [SerializeField] private AudioClip _swipeSfx;
        // =========================================================

        [Header("Effects")]
        [SerializeField] private ParticleSystem _shockwavePrefab;

        [Header("Pooling")]
        [SerializeField] private int _initialPoolSize = 30;

        [Header("Data")]
        [SerializeField] private BeatLibrary _beatLibrary;
        [Header("Visuals")]
        [SerializeField] private SpriteRenderer _hitZoneVisual; // 히트박스 이미지 연결
        private AudioSource _audioSource;
        private List<INoteView> _activeNotes = new List<INoteView>();
        private Queue<NoteView> _notePool = new Queue<NoteView>();

        public float RingRadius => _lifeRingView != null ? _lifeRingView.Radius : 3.0f;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();

            if (_lifeRingView) _lifeRingView.Initialize();
            if (_sphereView) _sphereView.Initialize();

            if (_sphereInteraction && _gameMain) _sphereInteraction.Initialize(_gameMain);

            InitializePool();
        }

        private void Start()
        {// BGM 소스 초기화
            _bgmSource.loop = true; // 무한 반복
            _bgmSource.playOnAwake = false;
            if (_gameMain != null)
            {
                _gameMain.Initialize(this, this, _beatLibrary);
            }
        }

        // ====================================================
        // IGameView 구현 (화면 제어)
        // ====================================================
        public void SetGroggyMode(bool isActive)
        {
            if (_sphereInteraction) _sphereInteraction.SetGroggyMode(isActive);
        }

        public void SpawnNote(NoteData data)
        {
            NoteView note = GetNoteFromPool();
            note.Initialize(data, RingRadius, this);
            _activeNotes.Add(note);
        }

        public IEnumerable<INoteView> GetActiveNotes() => _activeNotes;

        public void ReturnNote(INoteView noteInterface)
        {
            NoteView note = noteInterface as NoteView;
            if (note != null)
            {
                note.gameObject.SetActive(false);
                if (_activeNotes.Contains(note)) _activeNotes.Remove(note);
                _notePool.Enqueue(note);
            }
        }

        public void SetTheme(NoteColor mode)
        {
            if (_sphereView) _sphereView.SetColor(mode);
            if (_lifeRingView) _lifeRingView.SetColor(mode);

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.backgroundColor = (mode == NoteColor.White)
                    ? new Color(0.1f, 0.1f, 0.12f)
                    : new Color(0.9f, 0.9f, 0.92f);
            }
            // [신규] BGM 교체 로직 (자연스럽게 이어지진 않고 처음부터 재생됨)
            // 만약 비트를 맞추려면 PlayScheduled를 써야 하지만, 일단은 교체로 구현
            AudioClip targetBgm = (mode == NoteColor.White) ? _whiteBgm : _blackBgm;

            if (_bgmSource.clip != targetBgm)
            {
                float currentVolume = _bgmSource.volume;
                // 곡 교체
                _bgmSource.Stop();
                _bgmSource.clip = targetBgm;
                _bgmSource.Play();
            }// 2. [신규] 히트박스 색상 반전 (흑백 테마에 맞춰서)
            if (_hitZoneVisual != null)
            {
                // 배경이 어두우면(White모드) 히트박스는 잘 보이게 흰색/민트
                // 배경이 밝으면(Black모드) 히트박스는 검정/진한색
                if (mode == NoteColor.White)
                    _hitZoneVisual.color = new Color(0f, 1f, 1f); // 형광 민트 (잘 보임!)
                else
                    _hitZoneVisual.color = new Color(0.1f, 0.1f, 0.1f); // 진한 검정
            }
        }
        // [신규] 화면의 모든 노트 폭파 (성공 시 이펙트 포함)
        public void ClearAllNotes(bool success)
        {
            // 리스트 역순 순회하며 제거
            for (int i = _activeNotes.Count - 1; i >= 0; i--)
            {
                INoteView note = _activeNotes[i];

                // 성공했다면 폭죽놀이처럼 이펙트 터뜨려줌
                if (success)
                {
                    PlayHitEffect(note.Position, note.Color);
                }

                // 노트 반납
                ReturnNote(note);
            }
        }
        public void PlayHitEffect(Vector3 position, NoteColor color)
        {
            if (_shockwavePrefab)
            {
                // [수정] position 변수 무시! 무조건 중앙(Vector3.zero)에서 생성
                // 그래야 미리 만들어둔 '반지름 3짜리 파티클 링'이 게임 링과 딱 겹칩니다.
                var shock = Instantiate(_shockwavePrefab, Vector3.zero, Quaternion.identity);

                var main = shock.main;
                main.startColor = (color == NoteColor.White) ? Color.white : Color.black;

                // [팁] 노트 위치로 살짝 회전시켜서 "어디를 쳤는지" 방향성은 줄 수 있음
                // (선택사항: 필요 없으면 주석 처리)
                if (position != Vector3.zero)
                {
                    float angle = Mathf.Atan2(position.y, position.x) * Mathf.Rad2Deg;
                    shock.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
                }

                Destroy(shock.gameObject, 1.0f);
            }
        }

        public void ReduceLife()
        {
            if (_lifeRingView) _lifeRingView.ReduceLife();
        }

        // ====================================================
        // [신규] 오디오 구현 (비트 & 타격음)
        // ====================================================

        // 1. 배경 비트 (리듬 엔진이 자동 호출)
        public void PlayBaseSound(bool isKick)
        {
            AudioClip clip = isKick ? _kickClip : _snareClip;
            if (clip != null) _audioSource.PlayOneShot(clip);
        }

        // 2. 플레이어 타격음 (터치 성공 시 호출)
        public void PlayHitSound()
        {
            if (_hitClip != null) _audioSource.PlayOneShot(_hitClip);
        }

        public void PlaySfx(string name)
        {
            // _hitClip은 PlayHitSound로 대체되었지만, 레거시 지원을 위해 남겨둠
            if (name == "Hit") PlayHitSound();
            else if (name == "Miss" && _missSfx) _audioSource.PlayOneShot(_missSfx);
            else if (name == "Swipe" && _swipeSfx) _audioSource.PlayOneShot(_swipeSfx);
        }

        // ====================================================
        // Pooling Helper
        // ====================================================
        private void InitializePool()
        {
            for (int i = 0; i < _initialPoolSize; i++)
            {
                NoteView note = Instantiate(_notePrefab, _noteContainer);
                note.gameObject.SetActive(false);
                _notePool.Enqueue(note);
            }
        }

        public void TriggerGroggyEffect()
        {
            for (int i = _activeNotes.Count - 1; i >= 0; i--)
            {
                ReturnNote(_activeNotes[i]);
            }
            _activeNotes.Clear();

            if (_shockwavePrefab)
            {
                var shock = Instantiate(_shockwavePrefab, Vector3.zero, Quaternion.identity);
                var main = shock.main;
                main.startSize = 8f;
                main.startColor = Color.yellow;
                main.simulationSpeed = 3f;
                Destroy(shock.gameObject, 1.0f);
            }
        }

        private NoteView GetNoteFromPool()
        {
            if (_notePool.Count > 0)
            {
                NoteView note = _notePool.Dequeue();
                note.gameObject.SetActive(true);
                return note;
            }
            return Instantiate(_notePrefab, _noteContainer);
        }
    }
}
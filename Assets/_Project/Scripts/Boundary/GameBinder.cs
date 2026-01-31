using UnityEngine;
using System.Collections.Generic;
using TouchIT.Entity;
using TouchIT.Control;

namespace TouchIT.Boundary
{
    // 이 클래스가 Scene에 존재하며, 구체적인 View들을 들고 있습니다.
    [RequireComponent(typeof(AudioSource))] // 오디오 소스 필수
    public class GameBinder : MonoBehaviour, IGameView, IAudioManager
    {
        [Header("Control")]
        [SerializeField] private GameMain _gameMain; // 로직 컨트롤러 연결

        [Header("Views")]
        [SerializeField] private RingView _ringView;
        [SerializeField] private BossView _bossView;
        [SerializeField] private Transform _noteContainer;
        [SerializeField] private NoteView _notePrefab; // 구체적 프리팹
        [Header("Audio Clips")]
        // 인스펙터에서 도,레,미,솔,라 순서대로 5개 넣으세요
        [SerializeField] private AudioClip[] _pentatonicClips;
        [SerializeField] private AudioClip _hitSfx;
        [SerializeField] private AudioClip _missSfx;
        // [수정] 풀링 설정
        [Header("Pooling")]
        [SerializeField] private int _initialPoolSize = 30; // 미리 30개 만듦

        [Header("Effects")]
        [SerializeField] private ParticleSystem _shockwavePrefab; // 방금 만든 파티클 프리팹
        [SerializeField] private Transform _effectContainer; // 이펙트들 모아둘 부모 (없으면 Boundary에)
        private AudioSource _audioSource;
        // 오브젝트 풀 관리
        private List<NoteView> _activeNotes = new List<NoteView>();
        private Queue<NoteView> _notePool = new Queue<NoteView>();

        // IGameView 구현: 반경 정보 제공
        public float RingRadius => _ringView != null ? _ringView.Radius : 3.0f;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>(); // 컴포넌트 가져오기

            if (_ringView) _ringView.Initialize();
            if (_bossView) _bossView.InitializeLives(3);
            // [핵심] 게임 시작 전에 미리 다 만들어둠 (Pre-allocation)
            InitializePool();
        
        }
        // [수정] Awake -> Start로 변경!
        // GameMain이 Awake에서 Engine을 만든 후에 실행되도록 순서를 늦춥니다.
        private void Start()
        {
            if (_gameMain != null)
            {
                _gameMain.Initialize(this, this);
            }
        }
        // 1. 미리 만들기 (Awake에서 호출)
        private void InitializePool()
        {
            for (int i = 0; i < _initialPoolSize; i++)
            {
                NoteView note = Instantiate(_notePrefab, _noteContainer);
                note.gameObject.SetActive(false); // 일단 꺼둠
                _notePool.Enqueue(note);
            }
        }
        // ====================================================
        // IGameView 구현 (핵심 기능)
        // ====================================================
        public void SpawnNote(NoteData data)
        {
            NoteView note = GetNoteFromPool();
            note.Initialize(data, RingRadius);
            _activeNotes.Add(note);
        }

        public IEnumerable<INoteView> GetActiveNotes()
        {
            return _activeNotes;
        }

        public void ReturnNote(INoteView noteInterface)
        {
            NoteView note = noteInterface as NoteView;
            if (note != null)
            {
                note.Deactivate();
                _activeNotes.Remove(note);
                _notePool.Enqueue(note);
            }
        }

        public void PlayHitEffect()
        {
            // 1. 보스 줌아웃 (기존)
            if (_bossView) _bossView.PlayHitEffect();

            // 2. [추가] 링 둠칫 (Pulse)
            if (_ringView) _ringView.PlayPulseEffect();

            // 3. [추가] 파동 이펙트 생성
            if (_shockwavePrefab)
            {
                // 링 위치(0,0)에 생성
                ParticleSystem shock = Instantiate(_shockwavePrefab, Vector3.zero, Quaternion.identity);
                // 파티클은 자동 파괴 옵션이 없으면 계속 남으므로 Destroy 예약
                Destroy(shock.gameObject, 1.0f);
            }
        }

        public void ReduceLife(int amount)
        {
            if (_bossView) _bossView.ReduceLife(amount);
        }
        // ====================================================
        // [구현 완료] 오디오 시스템
        // ====================================================
        public void PlayNoteSound(int scaleIndex, int intensityLevel)
        {
            if (_pentatonicClips == null || _pentatonicClips.Length == 0) return;

            // 인덱스 안전장치 (0~4 범위로 나머지 연산)
            int clipIndex = scaleIndex % _pentatonicClips.Length;
            AudioClip clip = _pentatonicClips[clipIndex];

            // 강도(Intensity)에 따라 피치(음의 높낮이)를 살짝 조절해 화음 느낌 내기
            float pitch = 1.0f;
            if (intensityLevel == 1) pitch = 1.25f; // 3도 화음 느낌
            else if (intensityLevel == 2) pitch = 1.5f; // 5도 화음 느낌

            _audioSource.pitch = pitch;
            _audioSource.PlayOneShot(clip);
            _audioSource.pitch = 1.0f; // 원복
        }

        public void PlaySfx(string sfxName)
        {
            if (sfxName == "Hit" && _hitSfx) _audioSource.PlayOneShot(_hitSfx);
            if (sfxName == "Miss" && _missSfx) _audioSource.PlayOneShot(_missSfx);
        }

        public void SetMute(bool isMuted)
        {
            _audioSource.mute = isMuted;
        }
        // ====================================================
        // [에러 해결] 나머지 IGameView 구현 (빈 함수들)
        // ====================================================
        public void UpdateBossHp(float current, float max) { }
        public void PlayBossHitAnimation() { } // PlayHitEffect와 역할이 비슷하지만 인터페이스 요구사항 준수
        public void SetGroggyMode(bool isOn) { }
        public void ClearAllNotes() { }
        public void UpdateLifeVisual(int lifeCount) { }
        public void TriggerCameraKick(float intensity) { }
        public void SetTheme(bool isDarkMode) { }



        // ====================================================
        // Pooling Helper
        // ====================================================
        // 2. 꺼내 쓰기
        private NoteView GetNoteFromPool()
        {
            // 풀에 있으면 꺼내 씀
            if (_notePool.Count > 0)
            {
                NoteView note = _notePool.Dequeue();
                note.gameObject.SetActive(true); // 켜기
                return note;
            }

            // [예외처리] 30개 넘게 필요한 급한 상황이면 그때만 추가 생성 (비상용)
            // 혹은 그냥 null 리턴해서 스폰을 안 시킬 수도 있음 (선택)
            NoteView newNote = Instantiate(_notePrefab, _noteContainer);
            return newNote;
        }
    }
}
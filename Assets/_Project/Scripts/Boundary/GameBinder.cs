using UnityEngine;
using System.Collections.Generic;
using TouchIT.Entity;
using TouchIT.Control;

namespace TouchIT.Boundary
{
    // 이 클래스가 Scene에 존재하며, 구체적인 View들을 들고 있습니다.
    public class GameBinder : MonoBehaviour, IGameView, IAudioManager
    {
        [Header("Control")]
        [SerializeField] private GameMain _gameMain; // 로직 컨트롤러 연결

        [Header("Views")]
        [SerializeField] private RingView _ringView;
        [SerializeField] private BossView _bossView;
        [SerializeField] private Transform _noteContainer;
        [SerializeField] private NoteView _notePrefab; // 구체적 프리팹

        // 오브젝트 풀 관리
        private List<NoteView> _activeNotes = new List<NoteView>();
        private Queue<NoteView> _notePool = new Queue<NoteView>();

        // IGameView 구현: 반경 정보 제공
        public float RingRadius => _ringView != null ? _ringView.Radius : 3.0f;

        private void Awake()
        {
            // 뷰 초기화는 Awake에서 해도 됨
            if (_ringView) _ringView.Initialize();
            if (_bossView) _bossView.InitializeLives(3);
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
            if (_bossView) _bossView.PlayHitEffect();
        }

        public void ReduceLife(int amount)
        {
            if (_bossView) _bossView.ReduceLife(amount);
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
        // IAudioManager 구현
        // ====================================================
        public void PlayNoteSound(int scaleIndex, int intensityLevel)
        {
            Debug.Log($"[Audio] Play Scale: {scaleIndex}, Intensity: {intensityLevel}");
        }

        // [에러 해결] 나머지 IAudioManager 구현
        public void PlaySfx(string sfxName) { }
        public void SetMute(bool isMuted) { }


        // ====================================================
        // Pooling Helper
        // ====================================================
        private NoteView GetNoteFromPool()
        {
            if (_notePool.Count > 0) return _notePool.Dequeue();
            return Instantiate(_notePrefab, _noteContainer);
        }
    }
}
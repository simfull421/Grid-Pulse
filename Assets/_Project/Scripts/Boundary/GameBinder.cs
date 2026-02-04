using System.Collections.Generic;
using TouchIT.Control;
using TouchIT.Entity;
using UnityEngine;

namespace TouchIT.Boundary
{
    public class GameBinder : MonoBehaviour, IGameView
    {
        // === Core Systems ===
        [Header("Managers")]
        [SerializeField] private NoteManager _noteManager;
        [SerializeField] private VfxManager _vfxManager;
        [SerializeField] private EmberController _emberController; // [신규] 불꽃 관리자
        [SerializeField] private TouchInputHelper _inputHelper; // 입력 도우미 연결
        // === Views ===
        [Header("Views")]
        [SerializeField] private LifeRingView _lifeRingView;

        public float RingRadius => _lifeRingView != null ? _lifeRingView.Radius : 2.8f;

        // [신규] 불꽃 상태 확인
        public bool IsEmberDead => _emberController != null && _emberController.IsDead;

        public void Initialize()
        {
            // 1. 하위 시스템 초기화
            if (_lifeRingView) _lifeRingView.Initialize();
            if (_noteManager) _noteManager.Initialize();
            if (_vfxManager) _vfxManager.Initialize();
            if (_emberController) _emberController.Initialize();

            // 2. 초기 테마 (단일 테마) 적용
            Camera.main.backgroundColor = ThemeColors.Background;
        }

        // ==========================================
        // [IGameView Implementation]
        // ==========================================

        public void SpawnNote(NoteData data)
        {
            _noteManager.Spawn(data, RingRadius, this);
        }

        public List<INoteView> GetActiveNotes() => _noteManager.GetActiveNotes();

        public void ReturnNote(INoteView note) => _noteManager.ReturnNote(note);

        public void ClearAllNotes(bool success)
        {
            _noteManager.ClearAll(success, (note) => {
                // [수정] note.Color 대신 note.Type 전달
                if (success) PlayHitEffect(note.Position, note.Type);
            });
        }

        // --- Visual & Audio Feedback ---

        public void PlayHitEffect(Vector3 position, NoteType type)
        {
            _vfxManager.PlayHitEffect(position, type, RingRadius);

            // (선택사항) 구체 펀치 효과는 타입 상관없이 터짐
            // if (_juiceController) _juiceController.PunchSphere(); 
        }

        public void SetHoldEffect(bool isHolding)
        {
            _vfxManager.SetHoldEffectState(isHolding);
        }

        // --- Fuel & Survival ---
        // 1. 입력 중계 (Control -> View -> InputHelper)
        public float GetPinchDelta()
        {
            if (_inputHelper == null) return 0f;
            return _inputHelper.GetPinchDelta();
        }

        // 2. 비주얼 중계 (Control -> View -> Ember)
        public void SetZoomVisual(float progress)
        {
            if (_emberController) _emberController.SetZoomVisual(progress);
        }

  
        public void SpawnHyperNote(Vector2 position)
        {
            // (Osu 노트 프리팹 생성 로직 구현 필요)
            // Debug.Log($"Spawn Osu Note at {position}");
        }
        public void TriggerGroggyEffect() { }
    }
}
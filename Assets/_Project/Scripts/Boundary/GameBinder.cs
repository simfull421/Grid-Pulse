using System.Collections.Generic;
using TouchIT.Control;
using TouchIT.Entity;
using UnityEngine;

namespace TouchIT.Boundary
{
   // [Diagram]
    // [GameBinder]
    //    │
    //    ├── NoteManager  (노트 관리)
    //    ├── VfxManager   (이펙트/쉐이더/오디오반응)
    //    ├── HudManager   (2D UI)
    //    │
    //    ├── LifeRingView (링 + 타이머 역할)
    //    ├── SphereView   (구체 비주얼)
    //    └── SphereInteraction (구체 물리/위치)

    public class GameBinder : MonoBehaviour, IGameView
    {
        // === Sub Managers ===
        [Header("Sub Managers")]
        [SerializeField] private NoteManager _noteManager;
        [SerializeField] private VfxManager _vfxManager;
        [SerializeField] private HudManager _hudManager;

        // === Independent Views ===
        [Header("Independent Views")]
        [SerializeField] private LifeRingView _lifeRingView;
        [SerializeField] private SphereView _sphereView;
        [SerializeField] private SphereInteraction _sphereInteraction;

        // [신규] 쥬스 컨트롤러 (선택 사항, 직접 연결해도 됨)
        [Header("Juice & Feedback")]
        [SerializeField] private JuiceController _juiceController;

        public float RingRadius => _lifeRingView != null ? _lifeRingView.Radius : 2.8f;

        public void Initialize()
        {
            // 1. 뷰 초기화
            if (_lifeRingView) _lifeRingView.Initialize();
            if (_sphereView) _sphereView.Initialize();
            if (_sphereInteraction) _sphereInteraction.Initialize();

            // 2. 매니저 초기화
            if (_noteManager) _noteManager.Initialize();
            if (_vfxManager)
            {
                _vfxManager.Initialize();
                _vfxManager.SetHoldEffectPos(new Vector3(0, RingRadius, 0));
            }
            if (_hudManager) _hudManager.Initialize(RingRadius);

            // 3. 초기 테마 적용
            SetTheme(NoteColor.Black);
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
                if (success) PlayHitEffect(note.Position, note.Color);
            });
        }

        // --- Visual & Effects ---

        public void PlayHitEffect(Vector3 position, NoteColor color)
        {
            // 이펙트 재생
            _vfxManager.PlayHitEffect(position, color, RingRadius);

        }

        public void SetHoldEffect(bool isHolding)
        {
            _vfxManager.SetHoldEffectState(isHolding);

            // [추가] 홀드 중일 때 구체 떨림 효과 추가 가능
        }

        public void TriggerGroggyEffect()
        {
            ClearAllNotes(true);
            // 쉐이크 효과 등
        }

        public void PlayGroggyBubbleEffect(Vector3 centerPos, NoteColor theme)
        {
            _vfxManager.PlayGroggyBubble(centerPos, theme);
        }

        public void PunchRingEffect(Vector3 direction)
        {
            // 1. 링 자체의 물리적 반동
            if (_lifeRingView) StartCoroutine(RingBounceRoutine(direction));

        }
        private System.Collections.IEnumerator RingBounceRoutine(Vector3 hitDir)
        {
            if (_lifeRingView == null) yield break;

            Transform t = _lifeRingView.transform;
            Vector3 originalScale = Vector3.one;

            // 0.1초 동안 살짝 커졌다 작아짐
            float duration = 0.1f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float strength = Mathf.Sin((elapsed / duration) * Mathf.PI) * 0.15f;
                t.localScale = Vector3.one * (1.0f + strength);
                yield return null;
            }
            t.localScale = originalScale;
        }

        // --- Theme & UI ---

        public void SetTheme(NoteColor mode)
        {
            var colors = ThemeColors.GetColors(mode);

            if (Camera.main) Camera.main.backgroundColor = colors.Background;
            if (_sphereView) _sphereView.SetColor(mode);
            if (_lifeRingView) _lifeRingView.SetColor(mode); // 원래 색상 적용
            if (_hudManager) _hudManager.SetThemeColors(colors);
        }

        public void UpdateComboGauge(float fillAmount)
        {
            _hudManager.UpdateComboGauge(fillAmount);
        }

        // [변경] HudManager 대신 LifeRingView를 제어
        public void SetVisualTimer(float fillAmount, bool isActive)
        {
            if (_lifeRingView)
            {
                _lifeRingView.ShowTimerState(fillAmount, isActive);
            }
        }

        // --- Interactions ---

        public void ReduceLife() => _lifeRingView.ReduceLife();

        public void UpdateSpherePosition(Vector3 pos) => _sphereInteraction.UpdatePosition(pos);

        public void SetGroggyMode(bool isActive) => _sphereInteraction.SetGroggyVisual(isActive);
    }
}
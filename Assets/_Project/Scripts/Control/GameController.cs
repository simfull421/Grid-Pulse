using UnityEngine;
using TouchIT.Entity;
// [중요] using TouchIT.Boundary; 삭제됨 -> 의존성 끊음

namespace TouchIT.Control
{
    public class GameController : MonoBehaviour
    {
        // === Dependencies (Interfaces) ===
        // 구체 클래스가 아닌 인터페이스로만 선언
        public IGameView View { get; private set; }
        public IAudioManager Audio { get; private set; }
        private BeatLibrary _beatLibrary;

        // === Systems ===
        public RhythmEngine Engine { get; private set; }
        public HitJudgeSystem HitSystem { get; private set; }

        // === State & Data ===
        private GameState _currentState;
        public NoteColor CurrentTheme { get; private set; } = NoteColor.White;
        public int Combo { get; private set; }
        public int Score { get; private set; }

        // === Input ===
        private Vector2 _touchStartPos;
        private bool _isTouching;

        // [핵심] 외부(Initializer)에서 의존성을 꽂아주는 주입구
        public void Initialize(IGameView view, IAudioManager audio, BeatLibrary lib)
        {
            View = view;
            Audio = audio;
            _beatLibrary = lib;

            // 엔진 및 시스템 생성
            Engine = new RhythmEngine();
            Engine.Initialize(View, _beatLibrary);

            HitSystem = new HitJudgeSystem(targetAngle: 90f);

            // 초기 상태 진입
            ChangeState(new StateNormal(this));

            Debug.Log("✅ GameController Initialized via Dependency Injection");
        }

        private void Update()
        {
            // 초기화 전이면 실행 안 함
            if (View == null || Engine == null) return;

            if (_currentState != null) _currentState.Update();
            HandleInput();
        }

        public void ChangeState(GameState newState)
        {
            if (_currentState != null) _currentState.Exit();
            _currentState = newState;
            _currentState.Enter();
        }

        // ... (HandleInput, AddCombo, Gizmos 등 나머지 로직은 동일) ...

        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _isTouching = true;
                _touchStartPos = Input.mousePosition;
                _currentState?.OnTouch((Vector2)Input.mousePosition);
                // 상태에게 "손 뗐음" 알림 (그로기 복귀용)
                _currentState?.OnTouchUp();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (_isTouching)
                {
                    _isTouching = false;
                    Vector2 delta = (Vector2)Input.mousePosition - _touchStartPos;
                    if (delta.magnitude > 50f) _currentState?.OnSwipe(delta);
                }
            }

            if (_isTouching) _currentState?.OnDrag((Vector2)Input.mousePosition);
        }

        public void AddCombo()
        {
            Combo++;
            Score += 100 * Combo;
            View.UpdateComboGauge(Mathf.Clamp01((float)Combo / 10f));
            if (Combo >= 10) ChangeState(new StateGroggy(this));
        }

        public void ResetCombo()
        {
            Combo = 0;
            View.UpdateComboGauge(0f);
        }

        public void AddShakeScore()
        {
            Score += 50;
            Audio.PlaySfx("Hit");
        }

        public void SetTheme(NoteColor newTheme)
        {
            CurrentTheme = newTheme;
            View.SetTheme(newTheme);
            Audio.SetBgmTheme(newTheme);
            Engine.SetCurrentPhase(newTheme);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Vector3 dir = Quaternion.Euler(0, 0, 90f) * Vector3.right;
            Gizmos.DrawLine(Vector3.zero, dir * 3.6f);
        }
    }
}
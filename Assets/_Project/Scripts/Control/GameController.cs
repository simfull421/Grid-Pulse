using UnityEngine;
using TouchIT.Entity;
// [중요] Boundary 의존성 없음 (IGameView 사용)

namespace TouchIT.Control
{
    public class GameController : MonoBehaviour
    {
        // === Dependencies ===
        public IGameView View { get; private set; }
        public IAudioManager Audio { get; private set; }
        private BeatLibrary _beatLibrary;

        // === Systems ===
        public RhythmEngine Engine { get; private set; }
        public HitJudgeSystem HitSystem { get; private set; }

        // === State & Data ===
        private GameState _currentState;
        public int Combo { get; private set; }
        public int Score { get; private set; }
        public bool IsGameOver { get; private set; }

        // === Input ===
        private bool _isTouching;

        // [핵심] 외부(Initializer)에서 의존성을 꽂아주는 주입구
        public void Initialize(IGameView view, IAudioManager audio, BeatLibrary lib)
        {
            View = view;
            Audio = audio;
            _beatLibrary = lib;

            Engine = new RhythmEngine();
            Engine.Initialize(View, _beatLibrary);

            HitSystem = new HitJudgeSystem(targetAngle: 90f);

            // [변경] 테마 설정 삭제 (단일 테마 사용)
            // [변경] 초기 상태 -> StateSurvival (생존 모드)
            ChangeState(new StateSurvival(this));

            Debug.Log("✅ GameController Initialized (Fire Survival Mode)");
        }

        private void Update()
        {
            // 초기화 전이거나 게임 오버면 중지
            if (View == null || Engine == null || IsGameOver) return;

            if (_currentState != null) _currentState.Update();

            HandleInput();

            // [신규] 게임 오버 체크 (불꽃이 꺼졌는가?)
            if (View.IsEmberDead)
            {
                TriggerGameOver();
            }
        }

        public void ChangeState(GameState newState)
        {
            if (_currentState != null) _currentState.Exit();
            _currentState = newState;
            _currentState.Enter();
        }

        private void HandleInput()
        {
            // [단순화] 복잡한 스와이프 로직 삭제 -> 터치/드래그만 깔끔하게 처리
            if (Input.GetMouseButtonDown(0))
            {
                _isTouching = true;
                _currentState?.OnTouch((Vector2)Input.mousePosition);
            }
            else if (Input.GetMouseButton(0))
            {
                if (_isTouching) _currentState?.OnDrag((Vector2)Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (_isTouching)
                {
                    _isTouching = false;
                    _currentState?.OnTouchUp();
                }
            }
        }

        // === State에서 호출하는 액션들 ===
        public void OnNoteHit(INoteView note)
        {
            // 1. 점수 및 콤보
            Combo++;
            Score += 100 + (Combo * 10);

            // 2. 사운드 & 이펙트
            Audio.PlaySfx("Hit", Combo);

            // [수정] note.Color -> note.Type 전달
            View.PlayHitEffect(note.Position, note.Type);

            // 3. [핵심] 불꽃 연료 회복 (장작 투입)
            View.AddEmberFuel();

            // 4. 그로기(Ignition) 진입 체크
            if (Combo >= 10 && _currentState is StateSurvival)
            {
                Debug.Log("🔥 IGNITION READY! (Grind to Boost)");
                ChangeState(new StateIgnition(this));
            }

            // 노트 반환
            note.ReturnToPool();
        }
        public void OnNoteMiss(INoteView note)
        {
            Combo = 0;
            Audio.PlaySfx("Miss");

            // [핵심] 미스 시 연료 대폭 감소 (패널티)
            View.ConsumeEmberFuel(10f);

            if (note != null) note.ReturnToPool();
        }

        public void ResetCombo()
        {
            Combo = 0;
            // 콤보 게이지 UI 업데이트 삭제됨 (불꽃 크기가 곧 게이지임)
        }

        private void TriggerGameOver()
        {
            IsGameOver = true;
            Debug.Log("💀 GAME OVER: The Fire has faded...");
            // TODO: 게임 오버 UI 띄우기
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Vector3 dir = Quaternion.Euler(0, 0, 90f) * Vector3.right;
            Gizmos.DrawLine(Vector3.zero, dir * 3.6f);
        }
    }
}
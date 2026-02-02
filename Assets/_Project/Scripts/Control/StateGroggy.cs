using UnityEngine;

namespace TouchIT.Control
{
    public class StateGroggy : GameState
    {
        private float _groggyDuration = 5.0f;
        private float _timer;
        private Vector3 _lastWorldPos;
        private float _shakeCooldown;

        // [신규] 탄성 제어 변수
        private bool _isDragging = false;
        private Vector3 _currentSpherePos;

        public StateGroggy(GameController controller) : base(controller) { }

        public override void Enter()
        {
            Debug.Log("[State] 🔥 GROGGY MODE 🔥");
            _timer = _groggyDuration;
            Controller.View.SetGroggyMode(true);
            Controller.View.TriggerGroggyEffect();

            _lastWorldPos = Vector3.zero;
            _currentSpherePos = Vector3.zero;
            _isDragging = false;
        }

        public override void Update()
        {
            float dt = Time.deltaTime;
            _timer -= dt;

            // [핵심] 드래그 중이 아니면 탄력 있게 중앙(0,0,0)으로 복귀
            if (!_isDragging)
            {
                // Lerp로 부드럽게 복귀 (속도 10)
                _currentSpherePos = Vector3.Lerp(_currentSpherePos, Vector3.zero, dt * 10f);
                Controller.View.UpdateSpherePosition(_currentSpherePos);
            }

            // 게이지 & 종료 체크
            float ratio = _timer / _groggyDuration;
            Controller.View.UpdateComboGauge(ratio);

            if (_timer <= 0) Controller.ChangeState(new StateNormal(Controller));
        }

        public override void OnDrag(Vector2 mousePos)
        {
            _isDragging = true; // 드래그 중임을 표시

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            worldPos.z = 0;

            // [제약] 너무 멀리 못 가게 클램핑 (반지름 1.5 내에서만)
            worldPos = Vector3.ClampMagnitude(worldPos, 1.5f);

            _currentSpherePos = worldPos; // 현재 위치 갱신
            Controller.View.UpdateSpherePosition(_currentSpherePos);

            // 흔들기 점수 로직 (기존과 동일)
            float dist = Vector3.Distance(worldPos, _lastWorldPos);
            float speed = dist / Time.deltaTime;

            if (speed > 10.0f && Time.time > _shakeCooldown)
            {
                Controller.AddShakeScore();
                Controller.View.PlayGroggyBubbleEffect(worldPos, Controller.CurrentTheme);
                _shakeCooldown = Time.time + 0.15f;
            }
            _lastWorldPos = worldPos;
        }

        // [신규] 손 떼면 복귀 모드로 전환
        public override void OnTouchUp()
        {
            _isDragging = false;
        }

        public override void Exit()
        {
            Controller.View.SetGroggyMode(false);
            Controller.ResetCombo();
            Controller.View.UpdateComboGauge(0f);
            Controller.View.UpdateSpherePosition(Vector3.zero); // 확실하게 원위치
        }
    }
}
using UnityEngine;

namespace TouchIT.Control
{
    public class StateGroggy : GameState
    {
        private float _groggyDuration = 5.0f;
        private float _timer;
        private Vector3 _lastWorldPos;
        private float _shakeCooldown;
        private bool _isDragging = false;
        private Vector3 _currentSpherePos;

        public StateGroggy(GameController controller) : base(controller) { }
        private ShakeDetector _shakeSensor = new ShakeDetector(); // 센서 인스턴스
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

            // [탄성 복귀] 놓으면 아주 빠르게 튕겨 돌아옴 (스프링 효과)
            if (!_isDragging)
            {
                _currentSpherePos = Vector3.Lerp(_currentSpherePos, Vector3.zero, dt * 20f);
                Controller.View.UpdateSpherePosition(_currentSpherePos);
            }

            float ratio = _timer / _groggyDuration;
            Controller.View.UpdateComboGauge(ratio);

            if (_timer <= 0) Controller.ChangeState(new StateNormal(Controller));
        }

        public override void OnDrag(Vector2 mousePos)
        {
            _isDragging = true;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            worldPos.z = 0;

            // [쫀득한 물리] 0,0에서 멀어질수록 저항이 강해짐 (로그 함수나 루트 함수 사용)
            // 거리 1일 때 -> 실제이동 0.8
            // 거리 2일 때 -> 실제이동 1.2 ... 이런 식
            float distance = worldPos.magnitude;
            float resistDist = Mathf.Log(distance + 1f) * 1.5f; // 로그 저항

            // 최대 반지름(링 크기) 안쪽으로 제한
            float maxRadius = Controller.View.RingRadius - 0.5f; // 구체 반지름 고려
            if (resistDist > maxRadius)
            {
                resistDist = maxRadius;
                // [링 충돌 효과] 링에 닿았을 때 링을 출렁거리게 함
                Controller.View.PunchRingEffect(worldPos.normalized);
            }

            Vector3 finalPos = worldPos.normalized * resistDist;
            _currentSpherePos = finalPos;
            Controller.View.UpdateSpherePosition(_currentSpherePos);

            // [센서 퓨전] 터치 흔들기 + 자이로 흔들기 둘 다 인정
            bool isSensorShake = _shakeSensor.IsShaking();

            // 터치 속도 계산
            float distDelta = Vector3.Distance(finalPos, _lastWorldPos);
            float touchSpeed = distDelta / Time.deltaTime;

            // (터치속도가 빠르거나) OR (폰을 진짜로 흔들었거나)
            if ((touchSpeed > 15.0f || isSensorShake) && Time.time > _shakeCooldown)
            {
                Controller.AddShakeScore();

                // 터치 위치 말고 구체 위치에서 파티클
                Controller.View.PlayGroggyBubbleEffect(_currentSpherePos, Controller.CurrentTheme);

#if UNITY_ANDROID || UNITY_IOS
                Handheld.Vibrate();
#endif

                _shakeCooldown = Time.time + 0.1f;
            }

            _lastWorldPos = finalPos;
        }

        public override void OnTouchUp()
        {
            _isDragging = false;
        }

        public override void Exit()
        {
            Controller.View.SetGroggyMode(false);
            Controller.ResetCombo();
            Controller.View.UpdateComboGauge(0f);
            Controller.View.UpdateSpherePosition(Vector3.zero);
        }
    }
}
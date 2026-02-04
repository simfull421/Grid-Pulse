using UnityEngine;

namespace TouchIT.Control
{
    public class StateIgnition : GameState
    {
        private float _duration = 8.0f;
        private float _timer;

        public StateIgnition(GameController controller) : base(controller) { }

        public override void Enter()
        {
            Debug.Log("[State] 🔥 IGNITION MODE START");
            _timer = _duration;
            Controller.View.TriggerGroggyEffect();
            Controller.View.ClearAllNotes(true);
        }

        public override void Update()
        {
            _timer -= Time.deltaTime;

            if (_timer <= 0)
            {
                Controller.ResetCombo();
                Controller.ChangeState(new StateSurvival(Controller));
            }
        }

        public override void OnDrag(Vector2 screenPos)
        {
            // 1. 마우스 월드 좌표 변환
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -Camera.main.transform.position.z));
            worldPos.z = 0;

            // 2. [UX 핵심 개선] 좌표 제한 (Clamping)
            // 손이 링 밖으로 나가도, 구체는 링의 반지름 안쪽(내벽)까지만 따라감.
            // RingRadius보다 약간 안쪽(-0.2f)으로 잡아야 뚫고 나가는 시각적 오류를 방지함.
            float limitRadius = Controller.View.RingRadius - 0.2f;

            // 마우스 위치가 이 반경을 넘어가면, 딱 반경 위치로 잘라줌 (방향은 유지)
            Vector3 clampedPos = Vector3.ClampMagnitude(worldPos, limitRadius);

            // 3. 제한된 위치로 이동 명령
            Controller.View.UpdateEmberDrag(clampedPos);
        }

        public override void OnTouch(Vector2 screenPos)
        {
            OnDrag(screenPos);
        }

        public override void OnTouchUp()
        {
            Controller.View.StopEmberDrag();
        }

        public override void Exit()
        {
            Controller.View.StopEmberDrag();
        }
    }
}
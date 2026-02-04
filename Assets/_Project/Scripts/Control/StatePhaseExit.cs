using UnityEngine;

namespace TouchIT.Control
{
    // [Phase 4] 차원 복귀 (Pinch-In)
    public class StatePhaseExit : GameState
    {
        private float _zoomProgress = 1.0f; // 1.0(최대 확대)에서 시작
        private const float TARGET_ZOOM = 0.0f; // 0.0(원래 크기)으로 가야 함

        public StatePhaseExit(GameController controller) : base(controller) { }

        public override void Enter()
        {
            Debug.Log("[State] 👌 PINCH IN TO EXIT!");
            Controller.View.ShowGuideText("PINCH TO EXIT!");
            // 남은 HyperNote가 있다면 싹 정리
            // Controller.View.ClearHyperNotes(); 
        }

        public override void Update()
        {
            // [입력] 핀치 값 (모으면 음수)
            float pinchDelta = Controller.View.GetPinchDelta();

            // [로직] 모으기(음수) -> 진행도 감소
            if (pinchDelta < -0.001f)
            {
                // pinchDelta가 음수이므로 더하면 감소함
                _zoomProgress += pinchDelta * 2.0f;
            }
            else
            {
                // 가만히 있으면 다시 확대됨 (탈출 방해)
                _zoomProgress += Time.deltaTime * 0.3f;
            }

            _zoomProgress = Mathf.Clamp01(_zoomProgress);

            // [출력] 비주얼 업데이트
            Controller.View.SetZoomVisual(_zoomProgress);

            // [전이] 0.0에 도달하면 서바이벌 복귀
            if (_zoomProgress <= 0.05f) // 거의 다 줄어들었으면
            {
                Controller.ResetCombo();
                Controller.View.SetZoomVisual(0f); // 확실하게 0으로
                Controller.ChangeState(new StateSurvival(Controller));
            }
        }
    }
}
using UnityEngine;

namespace TouchIT.Control
{
    // [Phase 2] 차원 도약 (Pinch-Out)
    public class StatePhaseEntry : GameState
    {
        private float _zoomProgress = 0f;
        private const float TARGET_ZOOM = 1.0f; // 1.0 도달 시 이동

        public StatePhaseEntry(GameController controller) : base(controller) { }

        public override void Enter()
        {
            Debug.Log("[State] 👐 PINCH OUT TO ENTER!");
            // 1. 기존 노트 싹 정리
            Controller.View.ClearAllNotes(true);
        }

        public override void Update()
        {
            // [입력] View를 통해 핀치 값 가져오기 (의존성 분리 성공)
            float pinchDelta = Controller.View.GetPinchDelta();

            // [로직] 벌리면(양수) 진행도 증가
            if (pinchDelta > 0.001f)
            {
                _zoomProgress += pinchDelta * 2.0f; // 감도
            }
            else
            {
                // 안 벌리고 있으면 서서히 줄어듦 (텐션 유지)
                _zoomProgress -= Time.deltaTime * 0.5f;
            }

            _zoomProgress = Mathf.Clamp(_zoomProgress, 0f, TARGET_ZOOM);

            // [출력] View에게 그려달라고 요청
            Controller.View.SetZoomVisual(_zoomProgress);

            // [전이] 목표치 도달 시 Osu 모드로
            if (_zoomProgress >= TARGET_ZOOM)
            {
                // Controller.ChangeState(new StateHyperStream(Controller)); // 다음 구현
                Debug.Log(">>> GO TO HYPER STREAM! <<<");
            }
        }
    }
}
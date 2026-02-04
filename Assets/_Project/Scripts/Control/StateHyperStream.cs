using UnityEngine;

namespace TouchIT.Control
{
    // [Phase 3] 하이퍼 스트림 (Osu Mode)
    public class StateHyperStream : GameState
    {
        private float _duration = 10.0f; // 10초간 진행
        private float _timer;
        private float _spawnTimer;

        public StateHyperStream(GameController controller) : base(controller) { }

        public override void Enter()
        {
            Debug.Log("[State] ⚡ HYPER STREAM START!");
            _timer = _duration;
            Controller.View.ShowGuideText("TAP!");
        }

        public override void Update()
        {
            _timer -= Time.deltaTime;
            _spawnTimer += Time.deltaTime;

            // 0.2초마다 랜덤 위치에 노트 생성
            if (_spawnTimer > 0.2f && _timer > 1.0f) // 끝나기 1초 전엔 생성 중단
            {
                SpawnRandomNote();
                _spawnTimer = 0f;
            }

            // 시간이 다 되면 -> 축소(Exit) 페이즈로 전환
            if (_timer <= 0)
            {
                Controller.ChangeState(new StatePhaseExit(Controller));
            }
        }

        private void SpawnRandomNote()
        {
            // 화면 안쪽 랜덤 위치 (반지름 2.5 범위)
            Vector2 randomPos = Random.insideUnitCircle * 2.5f;
            Controller.View.SpawnHyperNote(randomPos, 0.5f); // 0.5초 수명
        }

        public override void OnTouch(Vector2 screenPos)
        {
            // 뷰에게 "나 이거 쳤는데 맞았어?" 물어봄
            bool isHit = Controller.View.TryHitHyperNote(screenPos);

            if (isHit)
            {
                // 성공! (콤보 증가, 점수 추가 등)
                // Controller.AddScore(100); 
                Debug.Log("Hyper Note Hit!");
            }
        }
    }
}
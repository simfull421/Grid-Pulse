using UnityEngine;
using TouchIT.Boundary;
using System;

namespace TouchIT.Control
{
    public class FireService
    {
        private readonly IMainView _mainView;

        // 설정값 (밸런스 조절용)
        private const float MIN_SIZE = 0f;    // 꺼짐 (Game Over)
        private const float MAX_SIZE = 1.0f;  // 링에 닿음 (Osu 모드 진입 조건)
        private const float START_SIZE = 0.5f; // 시작 크기

        private const float FUEL_ADD = 0.05f;  // 노트 성공 시 불씨 증가량
        private const float RAIN_DMG = 0.15f;  // 노트 실패 시 불씨 감소량

        // 상태값
        public float CurrentFireSize { get; private set; }
        public float CompletionRate => (float)_hitCount / _totalNotes * 100f; // 달성률 (%)

        private int _totalNotes;
        private int _hitCount;

        // 이벤트
        public event Action OnFireExtinguished; // 불 꺼짐 (게임 오버)
        public event Action OnFireFull;         // 불 꽉 참 (Osu 모드 준비 - 줌아웃 발광)

        public FireService(IMainView mainView)
        {
            _mainView = mainView;
        }

        public void SetupGame(int totalNotes)
        {
            _totalNotes = totalNotes;
            _hitCount = 0;
            CurrentFireSize = START_SIZE;
            UpdateVisuals();
        }

        // 장작 넣기 (Hit)
        public void AddFuel()
        {
            _hitCount++;
            CurrentFireSize = Mathf.Min(CurrentFireSize + FUEL_ADD, MAX_SIZE);

            UpdateVisuals();

            // 불이 꽉 찼으면 -> Osu 모드 진입 신호 (줌아웃 껌뻑껌뻑)
            if (CurrentFireSize >= MAX_SIZE - 0.01f)
            {
                OnFireFull?.Invoke();
            }
        }

        // 비 맞음 (Miss)
        public void Rain()
        {
            CurrentFireSize = Mathf.Max(CurrentFireSize - RAIN_DMG, MIN_SIZE);
            UpdateVisuals();

            if (CurrentFireSize <= MIN_SIZE + 0.01f)
            {
                OnFireExtinguished?.Invoke(); // 게임 오버
            }
        }

        // 부활 (광고 보고 났을 때)
        public void Revive()
        {
            CurrentFireSize = START_SIZE; // 절반 정도 회복
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            // 뷰에게 구체 크기 조절 요청 (0.5배 ~ 1.5배 사이 매핑)
            // 내부 로직(0~1)을 시각적 스케일(0.5 ~ 1.5)로 변환
            float visualScale = 0.5f + CurrentFireSize;
            _mainView.SetLifeScale(visualScale);
        }
    }
}
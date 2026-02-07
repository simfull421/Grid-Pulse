using UnityEngine;
using TouchIT.Entity;
using System;

namespace TouchIT.Boundary
{
    public class NoteView : MonoBehaviour, INoteView
    {
        [SerializeField] private SpriteRenderer _renderer;

        private double _targetTime;   // 도착해야 할 시간 (노트 타임)
        private float _approachRate;  // 날아가는 시간 (_preemptTime)
        private float _ringRadius;    // 도착 반지름

        private float _startAngleRad;
        private float _targetAngleRad;

        private Action<INoteView> _onMissCallback;
        private bool _isActive = false;
        // ⏱️ [설정] 유예 기간 (Overrun)
        // 1.0이 목표 지점. 1.2까지는 살려둠 (Late 판정용)
        private const float MAX_PROGRESS = 1.2f;
        // 인터페이스 구현
        public NoteType Type { get; private set; }
        public double TargetTime => _targetTime;
        public Transform Transform => transform;
        public GameObject GameObject => gameObject;

        public void Initialize(NoteInfo data, double currentDspTime, float approachRate, float ringRadius, Action<INoteView> onMiss)
        {
            Type = data.Type;
            _approachRate = approachRate;
            _ringRadius = ringRadius;
            _onMissCallback = onMiss;

            // 도착 시간 = 현재 시간 + 날아가는 시간
            // (주의: 이미 SpawnService에서 계산해서 넘겨준 게 아니라면 여기서 계산)
            // SpawnLogic: currentTime >= Time - Preempt 
            // 즉, Time (Target) = currentTime + Preempt (약간의 오차 보정)
            // 정확히는 data.Time이 TargetTime입니다.
            _targetTime = data.Time;

            if (_renderer != null)
                _renderer.color = (Type == NoteType.Hyper) ? Color.red : new Color(1f, 0.84f, 0f);

            // 6시 -> 12시 경로 계산 (이전과 동일)
            bool isClockwise = (data.LaneIndex % 2 != 0);
            _startAngleRad = -90f * Mathf.Deg2Rad;
            _targetAngleRad = _startAngleRad + (isClockwise ? Mathf.PI : -Mathf.PI);

            Activate();
            UpdatePosition(0f); // 초기 위치 잡기
        }

        public void Activate()
        {
            _isActive = true;
            gameObject.SetActive(true);
            if (_renderer != null) _renderer.enabled = true; // ✅ 초기화
        }
        public void Deactivate() { _isActive = false; gameObject.SetActive(false); }

        // 서비스가 매 프레임 호출해줌
        public void OnUpdate(double currentDspTime)
        {
            if (!_isActive) return;

            // 진행률 계산
            float progress = 1.0f - (float)((_targetTime - currentDspTime) / _approachRate);

            // 💀 [수정] 완전히 늦었을 때 (Miss)
            // 기존 1.0f -> 1.2f (Late 판정 여유분 확보)
            if (progress >= MAX_PROGRESS)
            {
                _onMissCallback?.Invoke(this);
                return;
            }

            // 👻 [추가] 12시를 넘겼다면? (Late 구간) -> 모습만 숨김
            if (progress >= 1.0f)
            {
                // 안 보이게 처리 (이미 껐으면 다시 끌 필요 없음)
                if (_renderer.enabled) _renderer.enabled = false;

                // 위치는 12시에 고정하거나, 계속 가게 둬도 됨 (안보이니까)
                // 여기선 계산 낭비 줄이게 위치 갱신 안 함
                return;
            }
            else
            {
                // 정상 구간: 보이게 설정
                if (!_renderer.enabled) _renderer.enabled = true;
                UpdatePosition(progress);
            }
        }
        private void UpdatePosition(float progress)
        {
            // 각도 보간
            float currentAngle = Mathf.Lerp(_startAngleRad, _targetAngleRad, progress);

            // 좌표 변환 (반지름 3.0f 유지)
            float x = Mathf.Cos(currentAngle) * _ringRadius;
            float y = Mathf.Sin(currentAngle) * _ringRadius;

            transform.localPosition = new Vector3(x, y, 0f);

            // 회전
            float degrees = currentAngle * Mathf.Rad2Deg;
            transform.localRotation = Quaternion.Euler(0, 0, degrees - 90f);
        }
    }
}
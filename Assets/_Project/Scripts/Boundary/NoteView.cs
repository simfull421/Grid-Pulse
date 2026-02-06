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

        public void Activate() { _isActive = true; gameObject.SetActive(true); }
        public void Deactivate() { _isActive = false; gameObject.SetActive(false); }

        // 서비스가 매 프레임 호출해줌
        public void OnUpdate(double currentDspTime)
        {
            if (!_isActive) return;

            // 진행률 계산 (0.0 ~ 1.0)
            // TargetTime까지 남은 시간이 ApproachRate의 몇 %인가?
            // 공식: 1 - ((도착시간 - 현재시간) / 전체시간)
            float progress = 1.0f - (float)((_targetTime - currentDspTime) / _approachRate);

            // 디버깅용: progress가 안 변하면 시간이 안 흐르는 것
            // Debug.Log($"Progress: {progress}"); 

            if (progress >= 1.0f) // 도착! (Miss 처리)
            {
                _onMissCallback?.Invoke(this);
                return;
            }

            UpdatePosition(progress);
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
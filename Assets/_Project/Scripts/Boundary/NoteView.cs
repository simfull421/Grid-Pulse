using UnityEngine;
using TouchIT.Entity;
using System;

namespace TouchIT.Boundary
{
    public class NoteView : MonoBehaviour, INoteView
    {
        [SerializeField] private SpriteRenderer _renderer;

        // 이동 관련
        private double _targetTime;
        private float _approachRate; // 전체 이동 시간
        private float _ringRadius;

        // 각도: Unity는 오른쪽(3시)이 0도, 위(12시)가 90도, 아래(6시)가 270도(-90도)
        private float _startAngleRad; // 시작: 6시 (-90도)
        private float _targetAngleRad; // 도착: 12시 (90도)
        private bool _isClockwise;     // 이동 방향 (우측통행 vs 좌측통행)

        private Action<INoteView> _onMissCallback;
        private bool _isActive = false;

        // 인터페이스 구현
        public NoteType Type { get; private set; }
        public double TargetTime => _targetTime;
        public Transform Transform => transform;
        public GameObject GameObject => gameObject;

        public void Initialize(NoteInfo data, double dspTime, float approachRate, float ringRadius, Action<INoteView> onMiss)
        {
            Type = data.Type;
            _targetTime = dspTime + approachRate;
            _approachRate = approachRate;
            _ringRadius = ringRadius;
            _onMissCallback = onMiss;

            // 🎨 색상: 골드 (일반), 빨강 (하이퍼)
            if (_renderer != null)
                _renderer.color = (Type == NoteType.Hyper) ? Color.red : new Color(1f, 0.84f, 0f); // Gold

            // 📐 이동 경로 결정 (6시 -> 12시)
            // LaneIndex가 짝수면 왼쪽 길, 홀수면 오른쪽 길
            _isClockwise = (data.LaneIndex % 2 != 0); // 홀수 -> 오른쪽(반시계) / 짝수 -> 왼쪽(시계)

            // 시작점: 6시 (-90도 = 270도 * Deg2Rad)
            _startAngleRad = -90f * Mathf.Deg2Rad;

            // 목표점: 12시 (90도)
            // 오른쪽 길(반시계): -90 -> 90 (증가)
            // 왼쪽 길(시계): -90 -> -270 (감소)
            _targetAngleRad = _isClockwise ? (90f * Mathf.Deg2Rad) : (-270f * Mathf.Deg2Rad);

            // 초기 위치 설정
            UpdatePosition(0f);

            // 노트 회전 (진행 방향을 바라보게) - 선택사항
            transform.localRotation = Quaternion.identity;

            Activate();
        }

        public void Activate() { _isActive = true; gameObject.SetActive(true); }
        public void Deactivate() { _isActive = false; gameObject.SetActive(false); }

        public void OnUpdate(double currentDspTime)
        {
            if (!_isActive) return;

            // 진행률 (0.0 ~ 1.0)
            float progress = 1.0f - (float)((_targetTime - currentDspTime) / _approachRate);

            if (progress >= 1.1f) // 판정선(12시) 지남 -> Miss
            {
                _onMissCallback?.Invoke(this);
                return;
            }

            UpdatePosition(progress);
        }

        private void UpdatePosition(float progress)
        {
            // 각도 보간 (Lerp)
            float currentAngle = Mathf.Lerp(_startAngleRad, _targetAngleRad, progress);

            // 극좌표 -> 직교좌표 (x = r*cos, y = r*sin)
            float x = Mathf.Cos(currentAngle) * _ringRadius;
            float y = Mathf.Sin(currentAngle) * _ringRadius;

            transform.localPosition = new Vector3(x, y, 0f);

            // (옵션) 노트가 링을 따라 회전하게 하려면
            float degrees = currentAngle * Mathf.Rad2Deg;
            transform.localRotation = Quaternion.Euler(0, 0, degrees - 90); // -90은 머리가 진행방향 보게
        }
    }
}
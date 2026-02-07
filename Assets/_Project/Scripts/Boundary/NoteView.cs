using UnityEngine;
using TouchIT.Entity;
using System;

namespace TouchIT.Boundary
{
    [RequireComponent(typeof(LineRenderer))]
    public class NoteView : MonoBehaviour, INoteView
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private LineRenderer _lineRenderer;

        private double _targetTime;
        private float _approachRate;
        private float _ringRadius;
        private float _duration; // 홀드 길이

        private float _startAngleRad;
        private float _targetAngleRad;
        private float _travelAngle;

        private Action<INoteView> _onMissCallback;
        private bool _isActive = false;

        // 🧬 [상태 추가] 현재 홀딩 중인가?
        private bool _isHolding = false;

        // 판정 여유 (Late)
        private const float MAX_PROGRESS = 1.2f;

        public NoteType Type { get; private set; }
        public double TargetTime => _targetTime;
        public float Duration => _duration; // 서비스에서 참조용
        public Transform Transform => transform;
        public GameObject GameObject => gameObject;

        private void Awake()
        {
            if (_lineRenderer == null) _lineRenderer = GetComponent<LineRenderer>();

            // 🎨 [수정 1] 두꺼운 아크 형태 (올챙이 탈출)
            _lineRenderer.positionCount = 20;
            _lineRenderer.useWorldSpace = false;

            // 시작과 끝 두께를 동일하게 (0.2f 추천)
            _lineRenderer.startWidth = 0.2f;
            _lineRenderer.endWidth = 0.2f;

            // 끝부분을 둥글게 처리 (CapVertices)
            _lineRenderer.numCapVertices = 5;
            _lineRenderer.numCornerVertices = 5;

            if (_lineRenderer.material == null || _lineRenderer.material.name.StartsWith("Default"))
                _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        public void Initialize(NoteInfo data, double currentDspTime, float approachRate, float ringRadius, Action<INoteView> onMiss)
        {
            Type = data.Type;
            _duration = data.Duration;
            _approachRate = approachRate;
            _ringRadius = ringRadius;
            _onMissCallback = onMiss;
            _targetTime = data.Time;
            _isHolding = false; // 초기화

            // 색상 설정
            Color noteColor = (Type == NoteType.Hold) ? Color.cyan : new Color(1f, 0.84f, 0f);
            if (_renderer != null)
            {
                _renderer.color = noteColor;
                _renderer.enabled = true; // 머리 보이기
            }

            if (_lineRenderer != null)
            {
                _lineRenderer.startColor = noteColor;
                // 끝부분을 약간 투명하게 할지, 불투명하게 할지 결정 (여기선 약간 투명)
                _lineRenderer.endColor = new Color(noteColor.r, noteColor.g, noteColor.b, 0.8f);
                _lineRenderer.enabled = (Type == NoteType.Hold);
            }

            // 경로 계산
            bool isClockwise = (data.LaneIndex % 2 != 0);
            _startAngleRad = -90f * Mathf.Deg2Rad;
            float targetAngleRad = _startAngleRad + (isClockwise ? Mathf.PI : -Mathf.PI);
            _travelAngle = targetAngleRad - _startAngleRad;

            Activate();
            UpdatePosition(0f);
        }

        public void Activate()
        {
            _isActive = true;
            gameObject.SetActive(true);
        }

        public void Deactivate()
        {
            _isActive = false;
            gameObject.SetActive(false);
        }

        // 🖱️ [추가] 홀드 시작 시 호출 (머리만 숨김)
        public void OnHoldStart()
        {
            _isHolding = true;
            if (_renderer != null) _renderer.enabled = false; // 머리 숨기기
            // 꼬리(_lineRenderer)는 계속 켜둠
        }

        public void OnUpdate(double currentDspTime)
        {
            if (!_isActive) return;

            // 진행률 계산 (현재 시간이 타겟 타임보다 얼마나 지났는지)
            float progress = 1.0f - (float)((_targetTime - currentDspTime) / _approachRate);

            // 1. 미스 판정 (홀드 중이 아닌데 너무 지나침)
            if (!_isHolding && progress >= MAX_PROGRESS)
            {
                _onMissCallback?.Invoke(this);
                return;
            }

            // 2. 홀드 노트의 수명 관리 (머리 + 꼬리 시간까지 다 지났는지?)
            // 홀드 끝나는 시간 = TargetTime + Duration
            if (Type == NoteType.Hold)
            {
                // 홀드 종료 시점 계산
                double holdEndTime = _targetTime + _duration;

                // 완전히 다 지나갔으면 비활성화 (성공 처리는 서비스가 함)
                if (currentDspTime > holdEndTime)
                {
                    // 서비스에서 제거해야 하므로 여기선 시각적 처리만
                    if (_lineRenderer != null) _lineRenderer.enabled = false;
                }
            }
            else if (progress >= 1.0f) // 일반 노트
            {
                if (_renderer.enabled) _renderer.enabled = false;
            }

            UpdatePosition(progress);
        }

        private void UpdatePosition(float progress)
        {
            // 헤드 위치 (홀드 중이어도 계산은 계속해서 꼬리 기준점을 잡음)
            float currentAngle = _startAngleRad + (_travelAngle * progress);

            float x = Mathf.Cos(currentAngle) * _ringRadius;
            float y = Mathf.Sin(currentAngle) * _ringRadius;

            transform.localPosition = new Vector3(x, y, 0f);

            float degrees = currentAngle * Mathf.Rad2Deg;
            transform.localRotation = Quaternion.Euler(0, 0, degrees - 90f);

            // 꼬리 그리기 (홀드 노트만)
            // 홀드 중(_isHolding)이어도 꼬리는 계속 그려야 함 (점점 사라지게 하거나 지나가게)
            if (Type == NoteType.Hold && _lineRenderer.enabled)
            {
                DrawHoldTail(currentAngle);
            }
        }

        private void DrawHoldTail(float headAngle)
        {
            float tailLengthRad = (_duration / _approachRate) * _travelAngle;
            int points = _lineRenderer.positionCount;
            Vector3[] positions = new Vector3[points];

            for (int i = 0; i < points; i++)
            {
                float t = (float)i / (points - 1);
                float angle = headAngle - (tailLengthRad * t);

                float px = Mathf.Cos(angle) * _ringRadius;
                float py = Mathf.Sin(angle) * _ringRadius;

                // 부모(Ring) 기준 월드 -> 로컬 변환
                Vector3 ringPos = new Vector3(px, py, 0f);
                if (transform.parent != null)
                    positions[i] = transform.InverseTransformPoint(transform.parent.TransformPoint(ringPos));
                else
                    positions[i] = new Vector3(px, py, 0f);
            }
            _lineRenderer.SetPositions(positions);
        }
    }
}
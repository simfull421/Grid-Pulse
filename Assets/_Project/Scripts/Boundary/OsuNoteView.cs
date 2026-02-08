using UnityEngine;
using TouchIT.Entity;
using System;
using DG.Tweening;

namespace TouchIT.Boundary
{
    public class OsuNoteView : MonoBehaviour, INoteView
    {
        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer _bodyRenderer;
        [SerializeField] private LineRenderer _fixedRing;
        [SerializeField] private LineRenderer _dragPathRenderer; // ✅ 필수!

        [Header("Settings")]
        [SerializeField] private int _segments = 64;
        [SerializeField] private float _baseRadius = 0.6f;
        [SerializeField] private float _ringWidth = 0.05f;

        private double _targetTime;
        private Action<INoteView> _onMissCallback;
        private bool _isActive = false;

        // 슬라이더 관련 변수
        private Vector3 _startPos;
        private Vector3 _endPos;
        private Vector3 _controlPos; // 베지어 제어점
        private bool _isSlider = false;

        public float Duration { get; private set; }
        public NoteType Type => NoteType.Hyper;
        public double TargetTime => _targetTime;
        public Transform Transform => transform;
        public GameObject GameObject => gameObject;

        public void OnHoldStart()
        {
            if (Duration > 0)
            {
                // 드래그 중 시각적 피드백 (색상 변경 등)
                _bodyRenderer.color = Color.cyan;
            }
        }

        public void InitializeOsu(Vector3 position, NoteInfo data, float approachTime, Action<INoteView> onMiss)
        {
            transform.position = position;
            _startPos = position;
            _targetTime = data.Time;
            Duration = data.Duration;
            _onMissCallback = onMiss;

            // 초기화
            _isSlider = (Duration > 0);
            _dragPathRenderer.enabled = _isSlider;
            _dragPathRenderer.positionCount = 0;

            // 1. 고정 링 그리기
            DrawFixedRing();

            // 2. 슬라이더 경로 생성 및 그리기
            if (_isSlider)
            {
                GenerateSliderPath();
                DrawSliderPath();
            }

            // 3. 애니메이션 (Body 크기 & 색상)
            _bodyRenderer.transform.localScale = Vector3.zero;
            _bodyRenderer.transform.localPosition = Vector3.zero; // 로컬 위치 초기화

            _bodyRenderer.transform
                .DOScale(Vector3.one, approachTime)
                .SetEase(Ease.Linear);

            Color startCol = new Color(0, 0, 0, 0);
            _bodyRenderer.color = startCol;
            DOTween.To(() => _bodyRenderer.color, x => _bodyRenderer.color = x, Color.black, approachTime * 0.2f);

            Activate();
        }

        // 🧮 슬라이더 경로 생성 (베지어 곡선)
        private void GenerateSliderPath()
        {
            // 끝점은 랜덤하게 정하되, 화면 밖으로 안 나가게 제한
            // (실제로는 NoteInfo에 EndPosition이 있는 게 좋지만, 지금은 랜덤 생성)
            Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-1.5f, 1.5f), UnityEngine.Random.Range(-1.5f, 1.5f), 0);
            _endPos = _startPos + randomOffset;

            // 제어점(P1): 시작점과 끝점의 중간에서 수직으로 살짝 휨
            Vector3 midPoint = (_startPos + _endPos) * 0.5f;
            Vector3 direction = (_endPos - _startPos).normalized;
            Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0) * UnityEngine.Random.Range(-1.0f, 1.0f);

            _controlPos = midPoint + perpendicular;
        }

        // 🖌️ 라인 렌더러로 경로 그리기
        private void DrawSliderPath()
        {
            int points = 30;
            _dragPathRenderer.positionCount = points;
            _dragPathRenderer.useWorldSpace = true; // 월드 좌표 사용
            _dragPathRenderer.startWidth = 0.2f; // 경로 두께
            _dragPathRenderer.endWidth = 0.2f;

            // 회색 반투명 경로
            _dragPathRenderer.startColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            _dragPathRenderer.endColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

            for (int i = 0; i < points; i++)
            {
                float t = i / (float)(points - 1);
                Vector3 p = CalculateBezierPoint(t, _startPos, _controlPos, _endPos);
                _dragPathRenderer.SetPosition(i, p);
            }
        }

        // 📐 베지어 곡선 공식: (1-t)^2 * P0 + 2(1-t)t * P1 + t^2 * P2
        private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            return (uu * p0) + (2 * u * t * p1) + (tt * p2);
        }

        private void DrawFixedRing()
        {
            if (_fixedRing == null) return;

            _fixedRing.positionCount = _segments + 1;
            _fixedRing.useWorldSpace = false;
            _fixedRing.startWidth = _ringWidth;
            _fixedRing.endWidth = _ringWidth;
            _fixedRing.loop = true;

            _fixedRing.startColor = Color.gray;
            _fixedRing.endColor = Color.gray;

            if (_fixedRing.material == null || _fixedRing.material.name.StartsWith("Default"))
                _fixedRing.material = new Material(Shader.Find("Sprites/Default"));

            float angleStep = 360f / _segments;
            Vector3[] positions = new Vector3[_segments + 1];
            for (int i = 0; i <= _segments; i++)
            {
                float rad = Mathf.Deg2Rad * (i * angleStep);
                positions[i] = new Vector3(Mathf.Cos(rad) * _baseRadius, Mathf.Sin(rad) * _baseRadius, 0f);
            }
            _fixedRing.SetPositions(positions);
        }

        public void OnUpdate(double currentDspTime)
        {
            if (!_isActive) return;

            // 1. 슬라이더 공 이동 로직
            if (_isSlider && currentDspTime >= _targetTime)
            {
                // 진행률 (0 ~ 1)
                // Duration 동안 이동
                float t = (float)((currentDspTime - _targetTime) / Duration);

                if (t <= 1.0f)
                {
                    // 곡선을 따라 이동 (월드 좌표 -> 로컬 좌표 변환 필요)
                    Vector3 worldPos = CalculateBezierPoint(t, _startPos, _controlPos, _endPos);
                    transform.position = worldPos; // 노트 자체를 이동시킴 (간단함)
                }
            }

            // 2. 종료 체크
            double endTime = _targetTime + (Duration > 0 ? Duration : 0.15f);
            if (currentDspTime > endTime)
            {
                _onMissCallback?.Invoke(this);
            }
        }

        public void Activate() { _isActive = true; gameObject.SetActive(true); }
        public void Deactivate()
        {
            _isActive = false;
            gameObject.SetActive(false);
            _bodyRenderer.transform.DOKill();
            _bodyRenderer.DOKill();
        }

        public void Initialize(NoteInfo d, double t, float a, float r, Action<INoteView> c) { }
    }
}


using UnityEngine;
using TouchIT.Entity;
using System;
using DG.Tweening;

namespace TouchIT.Boundary
{
    public class OsuNoteView : MonoBehaviour, INoteView
    {
        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer _bodyRenderer; // 커지는 원 (검정)
        [SerializeField] private LineRenderer _fixedRing;      // 고정된 링 (회색)

        // [추가] 드래그 노트용 라인 (나중에 구현)
        [SerializeField] private LineRenderer _dragPathRenderer;

        [Header("Settings")]
        [SerializeField] private int _segments = 64;
        [SerializeField] private float _baseRadius = 0.6f;
        [SerializeField] private float _ringWidth = 0.05f;

        private double _targetTime;
        private Action<INoteView> _onMissCallback;
        private bool _isActive = false;

        // ✅ [인터페이스 구현 1] Duration 프로퍼티
        public float Duration { get; private set; }

        public NoteType Type => NoteType.Hyper; // 혹은 NoteType.Drag
        public double TargetTime => _targetTime;
        public Transform Transform => transform;
        public GameObject GameObject => gameObject;

        // ✅ [인터페이스 구현 2] 홀드/드래그 시작 시 호출
        public void OnHoldStart()
        {
            // Osu 모드에서 드래그 노트라면?
            // 여기서 "공 따라가기" 로직을 활성화하거나
            // 시각적으로 "잡았다!"라는 피드백(색상 변경 등)을 줍니다.
            if (Duration > 0)
            {
                Debug.Log("Osu Drag Started!");
                _bodyRenderer.color = Color.cyan; // 잡았다는 표시 (예시)
            }
        }

        // Osu 모드 전용 초기화 함수 (GameBinder 등에서 호출)
        public void InitializeOsu(Vector3 position, NoteInfo data, float approachTime, Action<INoteView> onMiss)
        {
            transform.position = position;
            _targetTime = data.Time;
            Duration = data.Duration; // ✅ 데이터 저장
            _onMissCallback = onMiss;

            // 1. 고정 링 그리기
            DrawFixedRing();

            // 2. 드래그(슬라이더) 노트라면? 
            if (Duration > 0)
            {
                // TODO: 나중에 여기에 꼬리(슬라이더) 그리는 로직 추가
                // DrawSliderPath(...);
            }

            // 3. 내부 원(Body) 애니메이션 (어프로치 서클)
            _bodyRenderer.transform.localScale = Vector3.zero;
            _bodyRenderer.transform
                .DOScale(Vector3.one, approachTime)
                .SetEase(Ease.Linear);

            // 색상 페이드인
            Color startCol = new Color(0, 0, 0, 0);
            _bodyRenderer.color = startCol;

            DOTween.To(
                () => _bodyRenderer.color,
                x => _bodyRenderer.color = x,
                Color.black,
                approachTime * 0.2f
            );

            Activate();
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

            // 일반 노트면 시간 지나면 미스
            // 드래그 노트면 Duration까지 고려해야 함
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

        // ✅ [인터페이스 구현 3] 일반 모드용 초기화 (Osu 모드에선 안 씀)
        // 인터페이스 규약을 맞추기 위해 존재하지만, 내용은 비워두거나 에러 로그를 띄움
        public void Initialize(NoteInfo d, double t, float a, float r, Action<INoteView> c)
        {
            // Osu 모드는 InitializeOsu를 사용하므로 여기는 비워둡니다.
        }
    }
}
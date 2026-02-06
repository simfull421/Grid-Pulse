using UnityEngine;
using TouchIT.Entity;
using System;
using DG.Tweening;

namespace TouchIT.Boundary
{
    public class OsuNoteView : MonoBehaviour, INoteView
    {
        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer _bodyRenderer; // 중앙 타격체 (Sprite)
        [SerializeField] private LineRenderer _approachLine;   // 조여드는 링 (LineRenderer)

        [Header("Settings")]
        [SerializeField] private int _segments = 64; // 원의 부드러움 정도
        [SerializeField] private float _ringWidth = 0.05f; // 링 두께
        [SerializeField] private float _baseRadius = 0.6f; // 노트 크기에 맞춘 반지름 (Sprite 크기에 맞춰 조절)

        private double _targetTime;
        private Action<INoteView> _onMissCallback;
        private bool _isActive = false;

        public NoteType Type => NoteType.Hyper; // 3번 (오수 노트)
        public double TargetTime => _targetTime;
        public Transform Transform => transform;
        public GameObject GameObject => gameObject;

        public void InitializeOsu(Vector3 position, double targetTime, float approachTime, Action<INoteView> onMiss)
        {
            transform.position = position;
            _targetTime = targetTime;
            _onMissCallback = onMiss;

            // 1. 색상 설정
            _bodyRenderer.color = Color.black;

            // 2. 링 그리기
            DrawCircle();

            // 3. 애니메이션: 3배 크기에서 1배로 축소
            if (_approachLine != null)
            {
                // 크기 초기화
                _approachLine.transform.localScale = Vector3.one * 3.0f;

                // 크기 줄이기 (3.0 -> 1.0)
                _approachLine.transform.DOScale(Vector3.one, approachTime).SetEase(Ease.Linear);

                // ------------------------------------------------------------
                // 🎨 [수정] 색상 페이드인 로직 (DOTween.To 사용)
                // ------------------------------------------------------------
                Color startCol = new Color(0.5f, 0.5f, 0.5f, 0f); // 투명한 회색
                Color endCol = new Color(0.5f, 0.5f, 0.5f, 1f);   // 진한 회색

                // 초기값 적용
                _approachLine.startColor = startCol;
                _approachLine.endColor = startCol;

                // DOTween.To(getter, setter, target, duration)
                DOTween.To(
                    () => _approachLine.startColor, // 현재 값을 가져오는 법
                    x => {
                        _approachLine.startColor = x; // 값을 적용하는 법
                        _approachLine.endColor = x;
                    },
                    endCol, // 목표 값
                    approachTime * 0.2f // 지속 시간 (빠르게 나타남)
                )
                .SetLink(gameObject); // 오브젝트가 꺼지면 트윈도 취소됨 (안전장치)
            }

            Activate();
        }

        private void DrawCircle()
        {
            if (_approachLine == null) return;

            _approachLine.positionCount = _segments + 1;
            _approachLine.useWorldSpace = false; // 로컬 좌표 사용 (그래야 부모 따라다님)
            _approachLine.startWidth = _ringWidth;
            _approachLine.endWidth = _ringWidth;
            _approachLine.loop = true;

            // 기본 쉐이더 설정 (보라색 방지)
            if (_approachLine.material == null || _approachLine.material.name.StartsWith("Default-Material"))
                _approachLine.material = new Material(Shader.Find("Sprites/Default"));

            float angleStep = 360f / _segments;
            Vector3[] positions = new Vector3[_segments + 1];

            for (int i = 0; i <= _segments; i++)
            {
                float rad = Mathf.Deg2Rad * (i * angleStep);
                // X, Y 좌표 계산 (Z는 0)
                positions[i] = new Vector3(Mathf.Cos(rad) * _baseRadius, Mathf.Sin(rad) * _baseRadius, 0f);
            }

            _approachLine.SetPositions(positions);
        }

        public void OnUpdate(double currentDspTime)
        {
            if (!_isActive) return;

            if (currentDspTime > _targetTime + 0.1f) // 시간 초과
            {
                _onMissCallback?.Invoke(this);
            }
        }

        public void Activate() { _isActive = true; gameObject.SetActive(true); }

        public void Deactivate()
        {
            _isActive = false;
            gameObject.SetActive(false);
            _approachLine?.transform.DOKill(); // 트윈 중단 필수
        }

        public void Initialize(NoteInfo d, double t, float a, float r, Action<INoteView> c) { }
    }
}
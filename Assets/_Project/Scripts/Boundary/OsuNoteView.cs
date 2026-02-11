using UnityEngine;
using DG.Tweening;
using TouchIT.Entity;
using TouchIT.Control;
using System;

namespace TouchIT.Boundary
{
    [RequireComponent(typeof(LineRenderer))]
    public class OsuNoteView : MonoBehaviour, IOsuNoteView
    {
        [Header("Settings")]
        [SerializeField] private float _baseRadius = 0.4f; // 작게 설정

        private LineRenderer _bodyLine;
        private double _targetTime;
        private Action<INoteView> _onMissCallback;
        private bool _isActive = false;

        public Vector3 Position => transform.position;
        public float Radius => _baseRadius;
        public double TargetTime => _targetTime;
        public Transform Transform => transform;
        public GameObject GameObject => gameObject;
        public float Duration => 0f;
        public NoteType Type => NoteType.Tap;
        public int CurrentHP => 1;
        public bool IsHardNote => false;

        private void Awake()
        {
            _bodyLine = GetComponent<LineRenderer>();
            _bodyLine.useWorldSpace = false;
            _bodyLine.loop = true;
            _bodyLine.positionCount = 40;
            _bodyLine.startWidth = 0.1f;
            _bodyLine.endWidth = 0.1f;
            _bodyLine.sortingOrder = 10;

            if (_bodyLine.material == null || _bodyLine.material.name.StartsWith("Default"))
                _bodyLine.material = new Material(Shader.Find("Sprites/Default"));
        }

        public void InitializeOsu(Vector3 position, NoteInfo data, float approachTime, Action<INoteView> onMiss)
        {
            transform.position = position;
            _targetTime = data.Time;
            _onMissCallback = onMiss;

            InitializeVisuals();
            Activate();
        }

        // 네온 닌자에는 연결선 필요 없음! (더미 구현)
        public void ConnectToNextNote(Vector3 nextNotePos) { }

        private void InitializeVisuals()
        {
            // 색상: 형광색 (잘 보이게)
            Color neonColor = new Color(0f, 1f, 0.8f);
            _bodyLine.startColor = neonColor;
            _bodyLine.endColor = neonColor;

            DrawCircle(_baseRadius);

            // 등장할 때 살짝 커졌다 작아지는 연출 (Pop)
            transform.localScale = Vector3.one * 0.5f;
            transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
        }

        private void DrawCircle(float radius)
        {
            float angleStep = 360f / 40;
            Vector3[] positions = new Vector3[41];
            for (int i = 0; i <= 40; i++)
            {
                float rad = Mathf.Deg2Rad * (i * angleStep);
                positions[i] = new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0f);
            }
            _bodyLine.SetPositions(positions);
        }

        public void OnUpdate(double currentDspTime)
        {
            if (!_isActive) return;
            // 타겟 타임보다 0.5초 이상 지났으면 미스 (중앙을 지나쳐감)
            if (currentDspTime > _targetTime + 0.5f)
                _onMissCallback?.Invoke(this);
        }

        public bool TakeDamage()
        {
            transform.DOKill(); // 트윈 중단
            return true; // 즉시 파괴
        }

        public void Activate() { _isActive = true; gameObject.SetActive(true); }
        public void Deactivate() { _isActive = false; gameObject.SetActive(false); }

        // 미사용
        public void Initialize(NoteInfo d, double t, float a, float r, Action<INoteView> c) { }
        public void OnHoldStart() { }
    }
}
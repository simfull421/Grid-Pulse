using UnityEngine;
using DG.Tweening;
using TouchIT.Entity;
using TouchIT.Control;
using System;

namespace TouchIT.Boundary
{
    [RequireComponent(typeof(LineRenderer))] // 도넛 테두리용
    public class OsuNoteView : MonoBehaviour, IOsuNoteView
    {
        [Header("Components")]
        [SerializeField] private SpriteRenderer _coreRenderer; // 가운데 차오르는 녀석 (자식으로 넣으세요)

        [Header("Settings")]
        [SerializeField] private float _baseRadius = 0.8f;      // 노트 반지름 (충돌 범위)
        [SerializeField] private float _ringWidth = 0.1f;       // 도넛 두께
        [SerializeField] private int _segments = 50;            // 원 해상도
        [SerializeField] private float _hitCooldown = 0.15f;    // 3타 노트 연타 간격 (비비기용)

        // 내부 변수
        private LineRenderer _lineRenderer;
        private double _targetTime;
        private Action<INoteView> _onMissCallback;
        private bool _isActive = false;
        private float _lastHitTime;

        // 인터페이스 구현 속성
        public Vector3 Position => transform.position;
        public float Radius => _baseRadius;
        public double TargetTime => _targetTime;
        public Transform Transform => transform;
        public GameObject GameObject => gameObject;
        public float Duration { get; private set; }
        public NoteType Type { get; private set; }
        public int CurrentHP { get; private set; }
        public bool IsHardNote => CurrentHP > 1;

        private void Awake()
        {
            // 1. 라인 렌더러(도넛) 설정
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.useWorldSpace = false;
            _lineRenderer.loop = true;
            _lineRenderer.positionCount = _segments + 1;
            _lineRenderer.startWidth = _ringWidth;
            _lineRenderer.endWidth = _ringWidth;

            // 기본 마테리얼 할당 (없으면 핑크색 나오니까)
            if (_lineRenderer.material == null || _lineRenderer.material.name.StartsWith("Default"))
                _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

            // 2. 코어 렌더러 설정 (없으면 자동 생성 시도)
            if (_coreRenderer == null)
            {
                GameObject coreObj = new GameObject("CoreSprite");
                coreObj.transform.SetParent(transform);
                coreObj.transform.localPosition = Vector3.zero;
                _coreRenderer = coreObj.AddComponent<SpriteRenderer>();

                // 유니티 기본 Knob 스프라이트 로드 시도 (없으면 네모라도 나옴)
                _coreRenderer.sprite = Resources.Load<Sprite>("Knob");
            }
        }

        public void InitializeOsu(Vector3 position, NoteInfo data, float approachTime, Action<INoteView> onMiss)
        {
            transform.position = position;
            _targetTime = data.Time;
            Duration = data.Duration;
            Type = data.Type;
            _onMissCallback = onMiss;

            // 🩸 HP 설정: Hard(홀드 변환됨)는 3, 나머지는 1
            CurrentHP = (Type == NoteType.Hard) ? 3 : 1;

            InitializeVisuals(approachTime);
            Activate();
        }

        private void InitializeVisuals(float approachTime)
        {
            // 색상 테마 결정
            Color themeColor = IsHardNote ? Color.red : Color.cyan; // 하드: 빨강, 노말: 시안
            Color coreColor = themeColor;
            coreColor.a = 0.7f; // 코어는 약간 투명하게

            // 1. 도넛 (LineRenderer) 그리기
            DrawCircle(_baseRadius);
            _lineRenderer.startColor = themeColor;
            _lineRenderer.endColor = themeColor;

            // 2. 코어 (Sprite) 애니메이션
            _coreRenderer.color = coreColor;
            _coreRenderer.transform.localScale = Vector3.zero; // 0에서 시작

            _coreRenderer.transform.DOKill();
            // 타겟 타임에 딱 맞춰서 도넛 안쪽(0.9배)까지 꽉 차게 커짐
            _coreRenderer.transform
                .DOScale(Vector3.one * (_baseRadius * 2f * 0.9f), approachTime)
                .SetEase(Ease.Linear);
        }

        // ⭕ 라인 렌더러로 원 그리기
        private void DrawCircle(float radius)
        {
            float angleStep = 360f / _segments;
            Vector3[] positions = new Vector3[_segments + 1];

            for (int i = 0; i <= _segments; i++)
            {
                float rad = Mathf.Deg2Rad * (i * angleStep);
                positions[i] = new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0f);
            }
            _lineRenderer.SetPositions(positions);
        }

        public void OnUpdate(double currentDspTime)
        {
            if (!_isActive) return;

            // 미스 판정: 타겟 타임보다 0.2초 이상 지났는데 아직 살아있다면
            if (currentDspTime > _targetTime + 0.2f)
            {
                _onMissCallback?.Invoke(this);
            }
        }

        // 💥 충돌 처리 (비비기 로직 포함)
        public bool TakeDamage()
        {
            // 쿨타임 체크 (3타 노트의 경우 연속 타격을 위해 간격 필요)
            if (Time.time - _lastHitTime < _hitCooldown) return false;

            _lastHitTime = Time.time;
            CurrentHP--;

            PlayHitFeedback();

            return (CurrentHP <= 0); // HP가 0이 되면 true 반환 (파괴)
        }

        private void PlayHitFeedback()
        {
            // 1. 쉐이크 효과
            transform.DOKill(true);
            transform.DOPunchScale(Vector3.one * 0.3f, 0.15f, 10, 1);

            // 2. 색상 변화 (하드 노트는 맞을수록 더 진해지거나 검게 변함)
            if (IsHardNote)
            {
                float darken = (float)CurrentHP / 3f; // 3->2->1 갈수록 어두워짐
                Color hitColor = Color.Lerp(Color.black, Color.red, darken);
                _lineRenderer.startColor = hitColor;
                _lineRenderer.endColor = hitColor;
                _coreRenderer.color = new Color(hitColor.r, hitColor.g, hitColor.b, 0.8f);
            }
        }

        public void Activate() { _isActive = true; gameObject.SetActive(true); }
        public void Deactivate()
        {
            _isActive = false;
            gameObject.SetActive(false);
            _coreRenderer.transform.DOKill(); // 트윈 킬
        }

        // 미사용 인터페이스 메서드 (빈 구현)
        public void Initialize(NoteInfo d, double t, float a, float r, Action<INoteView> c) { }
        public void OnHoldStart() { }
    }
}
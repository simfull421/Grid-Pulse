using UnityEngine;
using DG.Tweening;
using TouchIT.Entity;
using TouchIT.Control;
using System;
using TMPro;

namespace TouchIT.Boundary
{
    [RequireComponent(typeof(LineRenderer))]
    public class OsuNoteView : MonoBehaviour, IOsuNoteView
    {
        [Header("Components")]
        [SerializeField] private SpriteRenderer _coreRenderer;
        [SerializeField] private TextMeshPro _hpText;

        [Header("Settings")]
        [SerializeField] private float _baseRadius = 0.8f;
        [SerializeField] private float _ringWidth = 0.1f;
        [SerializeField] private int _segments = 50;
        [SerializeField] private float _hitCooldown = 0.15f;

        private LineRenderer _lineRenderer;
        private double _targetTime;
        private Action<INoteView> _onMissCallback;
        private bool _isActive = false;
        private float _lastHitTime;

        // 인터페이스 구현
        public Vector3 Position => transform.position;
        public float Radius => _baseRadius;
        public double TargetTime => _targetTime;
        public Transform Transform => transform;
        public GameObject GameObject => gameObject;
        public float Duration { get; private set; }
        public NoteType Type { get; private set; }
        public int CurrentHP { get; private set; }
        public bool IsHardNote => CurrentHP > 1;

        private void Reset()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            if (_coreRenderer == null) _coreRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_hpText == null) _hpText = GetComponentInChildren<TextMeshPro>();
        }

        private void Awake()
        {
            // 1. LineRenderer 설정
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.useWorldSpace = false;
            _lineRenderer.loop = true;
            _lineRenderer.positionCount = _segments + 1;
            _lineRenderer.startWidth = _ringWidth;
            _lineRenderer.endWidth = _ringWidth;

            // 라인 렌더러 오더 설정 (중요)
            _lineRenderer.sortingOrder = 10;
            if (_lineRenderer.material == null || _lineRenderer.material.name.StartsWith("Default"))
                _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

            // 2. 컴포넌트 없을 시 자동 생성
            if (_coreRenderer == null) CreateCoreRenderer();
            if (_hpText == null) CreateHPText();
        }

        // ⚪ [수정] 코어 스프라이트가 없으면 코드로 원을 그려서 만듦
        private void CreateCoreRenderer()
        {
            GameObject coreObj = new GameObject("CoreSprite");
            coreObj.transform.SetParent(transform);
            coreObj.transform.localPosition = Vector3.zero;
            _coreRenderer = coreObj.AddComponent<SpriteRenderer>();

            // Knob 스프라이트 로드 시도
            Sprite knob = Resources.Load<Sprite>("Knob");
            if (knob == null)
            {
                // 없으면 하얀색 원 텍스처를 직접 생성 (fallback)
                knob = CreateCircleSprite();
            }
            _coreRenderer.sprite = knob;

            // ✅ [중요] 레이어 순서: 배경 < 라인(10) < 코어(15) < 텍스트(20)
            _coreRenderer.sortingOrder = 15;
        }

        private void CreateHPText()
        {
            GameObject textObj = new GameObject("HP_Text");
            textObj.transform.SetParent(transform);
            textObj.transform.localPosition = new Vector3(0, 0, -0.1f); // Z값 미세 조정

            _hpText = textObj.AddComponent<TextMeshPro>();
            _hpText.alignment = TextAlignmentOptions.Center;
            _hpText.fontSize = 6;
            _hpText.color = Color.white;
            _hpText.fontStyle = FontStyles.Bold;
            _hpText.rectTransform.sizeDelta = new Vector2(4, 4);

            // ✅ [중요] 텍스트가 제일 위에 보이도록 설정
            _hpText.sortingOrder = 20;
        }

        // 🎨 절차적 원 스프라이트 생성기 (이미지 없어도 됨)
        private Sprite CreateCircleSprite()
        {
            int res = 64;
            Texture2D texture = new Texture2D(res, res);
            Color[] colors = new Color[res * res];
            float center = res * 0.5f;
            float radius = res * 0.45f;

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    // 안쪽은 흰색, 바깥쪽은 투명
                    colors[y * res + x] = (dist <= radius) ? Color.white : Color.clear;
                }
            }
            texture.SetPixels(colors);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
        }

        public void InitializeOsu(Vector3 position, NoteInfo data, float approachTime, Action<INoteView> onMiss)
        {
            transform.position = position;
            _targetTime = data.Time;
            Duration = data.Duration;
            Type = data.Type;
            _onMissCallback = onMiss;
            CurrentHP = (Type == NoteType.Hard) ? 3 : 1;

            UpdateHPText();
            InitializeVisuals(approachTime);
            Activate();
        }

        private void UpdateHPText()
        {
            if (_hpText != null)
            {
                _hpText.text = CurrentHP.ToString();
                _hpText.gameObject.SetActive(true);
            }
        }

        private void InitializeVisuals(float approachTime)
        {
            Color themeColor = IsHardNote ? Color.red : Color.cyan;
            Color coreColor = themeColor;
            coreColor.a = 0.5f; // 반투명하게

            // 1. 도넛 그리기
            DrawCircle(_baseRadius);
            _lineRenderer.startColor = themeColor;
            _lineRenderer.endColor = themeColor;

            // 2. 코어 애니메이션 (0 -> 꽉 참)
            _coreRenderer.color = coreColor;
            _coreRenderer.transform.localScale = Vector3.zero;
            _coreRenderer.transform.DOKill();

            // 도넛 안쪽까지 차오름
            _coreRenderer.transform
                .DOScale(Vector3.one * (_baseRadius * 2f * 0.9f), approachTime)
                .SetEase(Ease.Linear);
        }

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

        // ... 나머지 Update, TakeDamage 로직 기존 동일 ...
        public void OnUpdate(double currentDspTime) { if (!_isActive) return; if (currentDspTime > _targetTime + 0.2f) _onMissCallback?.Invoke(this); }
        public bool TakeDamage()
        {
            if (Time.time - _lastHitTime < _hitCooldown) return false;
            _lastHitTime = Time.time;
            CurrentHP--;
            UpdateHPText();
            PlayHitFeedback();
            return (CurrentHP <= 0);
        }
        private void PlayHitFeedback() { transform.DOKill(true); transform.DOPunchScale(Vector3.one * 0.3f, 0.15f, 10, 1); }
        public void Activate() { _isActive = true; gameObject.SetActive(true); }
        public void Deactivate() { _isActive = false; gameObject.SetActive(false); _coreRenderer.transform.DOKill(); }
        public void Initialize(NoteInfo d, double t, float a, float r, Action<INoteView> c) { }
        public void OnHoldStart() { }
    }
}
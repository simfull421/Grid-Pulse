using UnityEngine;
using System.Collections.Generic;
using TouchIT.Entity;

namespace TouchIT.Boundary
{
    // [펀치 데이터]
    public class RingBulge
    {
        public int SectorIndex;
        public float Angle;
        public float Amplitude;
        public float Decay;
        public bool IsFinished => Amplitude <= 0.001f; // 더 정밀하게 체크
        public void Update(float dt) { Amplitude = Mathf.Lerp(Amplitude, 0f, dt * Decay); }
    }

    public class LifeRingView : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _radius = 2.8f;
        // [피드백 반영] 링 두께 0.15 -> 0.08 (얇고 세련되게)
        [SerializeField] private float _width = 0.08f;

        [Header("Subtle & Sharp Punch")]
        // [피드백 반영] 높이 대폭 축소 (0.6 -> 0.25) : 산만함 방지
        [SerializeField] private float _baseKickHeight = 0.25f;
        [SerializeField] private float _elasticity = 20f; // 복원 속도 UP (잔상 없이 바로 사라짐)

        private const int SECTOR_COUNT = 32;
        private const int NEIGHBOR_BLOCK = 3; // 이웃 억제 범위 증가 (동시다발적 발생 방지)
        private const int MAX_LIFE = 16;

        private int _currentLife;
        private List<LineRenderer> _segments = new List<LineRenderer>();
        private List<RingBulge> _activeBulges = new List<RingBulge>();
        private bool[] _sectorOccupied = new bool[SECTOR_COUNT];

        private EdgeCollider2D _edgeCollider;
        private NoteColor _lastTheme = NoteColor.White;

        public float Radius => _radius;

        public void Initialize()
        {
            _currentLife = MAX_LIFE;
            foreach (Transform child in transform) Destroy(child.gameObject);
            _segments.Clear();
            _activeBulges.Clear();
            System.Array.Clear(_sectorOccupied, 0, SECTOR_COUNT);

            // 물리 벽 & 비주얼 생성
            _edgeCollider = GetComponent<EdgeCollider2D>();
            if (_edgeCollider == null) _edgeCollider = gameObject.AddComponent<EdgeCollider2D>();

            CreatePhysicsBoundary();
            CreateSegments();
            UpdateVisual();
        }

        private void Update()
        {
            if (_segments.Count == 0) return;

            for (int i = _activeBulges.Count - 1; i >= 0; i--)
            {
                var bulge = _activeBulges[i];
                bulge.Update(Time.deltaTime);
                if (bulge.IsFinished)
                {
                    _sectorOccupied[bulge.SectorIndex] = false;
                    _activeBulges.RemoveAt(i);
                }
            }
            UpdateRingShape();
        }

        public void ApplyAudioImpulse(float power)
        {
            // [피드백 반영] 임계값 상향 (0.15 -> 0.3) : 확실한 비트만 통과
            if (power < 0.3f) return;

            // 강도 레벨링 (미세함 ~ 약간 튐)
            // 너무 크게 튀어나오면 정신사나우므로 최대치를 제한
            float levelMultiplier = 1.0f;
            if (power > 0.8f) levelMultiplier = 1.5f; // 강한 비트도 1.5배까지만

            int targetSector = Random.Range(0, SECTOR_COUNT);
            if (IsAreaClear(targetSector))
            {
                SpawnPunch(targetSector, _baseKickHeight * levelMultiplier);
            }
        }

        private bool IsAreaClear(int centerIndex)
        {
            for (int offset = -NEIGHBOR_BLOCK; offset <= NEIGHBOR_BLOCK; offset++)
            {
                int checkIndex = (centerIndex + offset + SECTOR_COUNT) % SECTOR_COUNT;
                if (_sectorOccupied[checkIndex]) return false;
            }
            return true;
        }

        private void SpawnPunch(int sectorIndex, float amplitude)
        {
            _sectorOccupied[sectorIndex] = true;
            float angleStep = 360f / SECTOR_COUNT;
            _activeBulges.Add(new RingBulge()
            {
                SectorIndex = sectorIndex,
                Angle = sectorIndex * angleStep,
                Amplitude = amplitude,
                Decay = _elasticity
            });
        }

        private void CreatePhysicsBoundary()
        {
            int pointsCount = 60;
            Vector2[] points = new Vector2[pointsCount + 1];
            for (int i = 0; i <= pointsCount; i++)
            {
                float angle = (float)i / pointsCount * 360f * Mathf.Deg2Rad;
                points[i] = new Vector2(Mathf.Cos(angle) * _radius, Mathf.Sin(angle) * _radius);
            }
            _edgeCollider.points = points;
        }

        private void CreateSegments()
        {
            for (int i = 0; i < MAX_LIFE; i++)
            {
                GameObject obj = new GameObject($"Seg_{i}");
                obj.transform.localPosition = new Vector3(0, 0, 1.0f);
                obj.transform.SetParent(transform, false);
                LineRenderer lr = obj.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.material = new Material(Shader.Find("Sprites/Default"));
                // [피드백 반영] 두께 적용
                lr.startWidth = _width; lr.endWidth = _width;
                lr.positionCount = 20;
                _segments.Add(lr);
            }
        }

        private void UpdateRingShape()
        {
            float angleStep = 360f / MAX_LIFE;
            float gap = 2.0f; // 틈새는 유지 (시인성 위해)

            for (int i = 0; i < MAX_LIFE; i++)
            {
                LineRenderer lr = _segments[i];
                if (!lr.enabled) continue;
                float startAngle = 90f - (i * angleStep);
                float endAngle = startAngle - angleStep;
                DrawBulgedArc(lr, startAngle - gap / 2, endAngle + gap / 2);
            }
        }

        private void DrawBulgedArc(LineRenderer lr, float startDeg, float endDeg)
        {
            int points = lr.positionCount;
            for (int j = 0; j < points; j++)
            {
                float t = (float)j / (points - 1);
                float currentDeg = Mathf.Lerp(startDeg, endDeg, t);

                float offset = CalculateBulgeOffset(currentDeg);
                float finalRadius = _radius + offset;

                float rad = currentDeg * Mathf.Deg2Rad;
                lr.SetPosition(j, new Vector3(Mathf.Cos(rad) * finalRadius, Mathf.Sin(rad) * finalRadius, 0));
            }
        }

        private float CalculateBulgeOffset(float currentAngle)
        {
            float totalOffset = 0f;
            // [피드백 반영] 뾰족함(Sharpness)을 위해 너비를 좁힘 (1.2 -> 0.6)
            float punchWidth = (360f / SECTOR_COUNT) * 0.6f;

            foreach (var bulge in _activeBulges)
            {
                float diff = Mathf.DeltaAngle(currentAngle, bulge.Angle);

                if (Mathf.Abs(diff) < punchWidth)
                {
                    float ratio = diff / punchWidth;

                    // [핵심] 아주 뾰족한 가시 모양
                    // Cosine 기반이지만, Pow 5제곱을 사용하여 바늘처럼 만듦
                    float baseShape = Mathf.Cos(ratio * Mathf.PI * 0.5f);
                    float sharpShape = Mathf.Pow(baseShape, 5.0f); // 3->5제곱 (더 뾰족하게)

                    totalOffset += sharpShape * bulge.Amplitude;
                }
            }
            return totalOffset;
        }

        public void SetColor(NoteColor theme) { _lastTheme = theme; var c = ThemeColors.GetColors(theme).Foreground; foreach (var s in _segments) { s.startColor = c; s.endColor = c; } }
        public void ReduceLife() { if (_currentLife > 0) _currentLife--; UpdateVisual(); }
        public void ShowTimerState(float progress, bool isActive)
        {
            if (!isActive) { RestoreLifeState(); return; }
            Color c = new Color(1f, 0.2f, 0.2f, 0.6f);
            foreach (var s in _segments) { s.startColor = c; s.endColor = c; }
            int count = Mathf.CeilToInt(MAX_LIFE * progress);
            for (int i = 0; i < MAX_LIFE; i++) _segments[i].enabled = i < count;
        }
        private void RestoreLifeState() { SetColor(_lastTheme); UpdateVisual(); }
        private void UpdateVisual() { for (int i = 0; i < MAX_LIFE; i++) _segments[i].enabled = i < _currentLife; }
    }
}
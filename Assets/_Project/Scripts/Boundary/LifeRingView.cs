using UnityEngine;
using System.Collections.Generic;
using TouchIT.Entity;

namespace TouchIT.Boundary
{
    // [펀치 데이터]
    public class RingBulge
    {
        public int SectorIndex;  // 0 ~ 31
        public float Angle;      // 중심 각도
        public float Amplitude;  // 현재 높이
        public float Decay;      // 사라지는 속도

        public bool IsFinished => Amplitude <= 0.01f;

        public void Update(float dt)
        {
            // 탄력적으로 줄어듦 (스프링)
            Amplitude = Mathf.Lerp(Amplitude, 0f, dt * Decay);
        }
    }

    public class LifeRingView : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _radius = 2.8f;
        [SerializeField] private float _width = 0.15f;

        [Header("32-Grid Punch System")]
        [SerializeField] private float _baseKickHeight = 0.6f; // 강도 1일 때 높이
        [SerializeField] private float _elasticity = 12f;      // 복원 속도

        // 32등분 설정
        private const int SECTOR_COUNT = 32;
        private const int NEIGHBOR_BLOCK = 2; // 양옆 2칸씩 억제

        private const int MAX_LIFE = 16;
        private int _currentLife;
        private List<LineRenderer> _segments = new List<LineRenderer>();

        // 활성 펀치 리스트
        private List<RingBulge> _activeBulges = new List<RingBulge>();

        // [중요] 각 섹터가 현재 사용 중인지 체크 (이웃 억제용)
        private bool[] _sectorOccupied = new bool[SECTOR_COUNT];

        private NoteColor _lastTheme = NoteColor.White;
        public float Radius => _radius;

        public void Initialize()
        {
            _currentLife = MAX_LIFE;
            foreach (Transform child in transform) Destroy(child.gameObject);
            _segments.Clear();
            _activeBulges.Clear();
            System.Array.Clear(_sectorOccupied, 0, SECTOR_COUNT);

            CreateSegments();
            UpdateVisual();
        }

        private void Update()
        {
            // 1. 펀치 업데이트
            for (int i = _activeBulges.Count - 1; i >= 0; i--)
            {
                var bulge = _activeBulges[i];
                bulge.Update(Time.deltaTime);

                if (bulge.IsFinished)
                {
                    // 끝나면 점유 해제
                    _sectorOccupied[bulge.SectorIndex] = false;
                    _activeBulges.RemoveAt(i);
                }
            }

            // 2. 링 그리기
            UpdateRingShape();
        }

        // [핵심 로직] 외부에서 비트가 들어오면 호출
        public void ApplyAudioImpulse(float power)
        {
            // 너무 약한 신호는 무시 (노이즈 게이트)
            if (power < 0.2f) return;

            // 1. 강도(Level) 결정: 1 ~ 3 단계
            // power가 0.2~0.5면 1, 0.5~0.8이면 2, 0.8 이상이면 3
            int level = 1;
            if (power > 0.5f) level = 2;
            if (power > 0.8f) level = 3;

            // 2. 랜덤 섹터 선정 (0 ~ 31)
            int targetSector = Random.Range(0, SECTOR_COUNT);

            // 3. [이웃 검사] 나를 포함해 양옆 2칸(총 5칸)이 비어있어야 함
            if (IsAreaClear(targetSector))
            {
                SpawnPunch(targetSector, level);
            }
        }

        private bool IsAreaClear(int centerIndex)
        {
            // -2 ~ +2 범위 검사
            for (int offset = -NEIGHBOR_BLOCK; offset <= NEIGHBOR_BLOCK; offset++)
            {
                // 원형 인덱스 처리 (음수나 32 이상이 나오면 순환)
                int checkIndex = (centerIndex + offset + SECTOR_COUNT) % SECTOR_COUNT;

                if (_sectorOccupied[checkIndex]) return false; // 누가 쓰고 있다!
            }
            return true;
        }

        private void SpawnPunch(int sectorIndex, int level)
        {
            // 점유 표시
            _sectorOccupied[sectorIndex] = true;

            float angleStep = 360f / SECTOR_COUNT;
            float centerAngle = sectorIndex * angleStep;

            _activeBulges.Add(new RingBulge()
            {
                SectorIndex = sectorIndex,
                Angle = centerAngle,
                Amplitude = _baseKickHeight * level, // 레벨에 따른 높이
                Decay = _elasticity
            });
        }

        // ... (CreateSegments, SetColor 등 기존 코드 동일) ...
        private void CreateSegments()
        {
            // [Z축 정렬] 뒤로(1.0f)
            float angleStep = 360f / MAX_LIFE;
            for (int i = 0; i < MAX_LIFE; i++)
            {
                GameObject obj = new GameObject($"Seg_{i}");
                obj.transform.localPosition = new Vector3(0, 0, 1.0f);
                obj.transform.SetParent(transform, false);
                LineRenderer lr = obj.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.startWidth = _width; lr.endWidth = _width;
                lr.positionCount = 20;
                _segments.Add(lr);
            }
        }

        private void UpdateRingShape()
        {
            float angleStep = 360f / MAX_LIFE;
            float gap = 2.0f;

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

                // [가우시안 오프셋 계산]
                float offset = CalculateBulgeOffset(currentDeg);
                float finalRadius = _radius + offset;

                float rad = currentDeg * Mathf.Deg2Rad;
                lr.SetPosition(j, new Vector3(Mathf.Cos(rad) * finalRadius, Mathf.Sin(rad) * finalRadius, 0));
            }
        }

        private float CalculateBulgeOffset(float currentAngle)
        {
            float totalOffset = 0f;
            // 펀치 너비 (각도) - 섹터 크기의 1.5배 정도가 적당
            float punchWidth = (360f / SECTOR_COUNT) * 1.5f;

            foreach (var bulge in _activeBulges)
            {
                float diff = Mathf.DeltaAngle(currentAngle, bulge.Angle);

                if (Mathf.Abs(diff) < punchWidth)
                {
                    float ratio = diff / punchWidth;
                    // Cosine Window로 뾰족하게
                    float shape = Mathf.Cos(ratio * Mathf.PI * 0.5f);
                    totalOffset += shape * bulge.Amplitude;
                }
            }
            return totalOffset;
        }

        // ... (나머지 SetColor, ReduceLife, ShowTimerState 동일) ...
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
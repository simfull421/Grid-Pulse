using UnityEngine;
using TouchIT.Entity;

namespace TouchIT.Boundary
{
    [RequireComponent(typeof(LineRenderer))]
    public class RingView : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _radius = 3.0f; // 반지름 (이게 수학적 기준!)
        [SerializeField] private float _width = 0.2f;  // 도넛 두께
        [SerializeField] private int _segments = 100;  // 원을 몇 조각으로 그릴지 (곡선 부드러움)
        [SerializeField] private Color _baseColor = new Color(1, 1, 1, 0.3f); // 흐릿한 흰색

        [Header("Target Zone (12 o'clock)")]
        [SerializeField] private GameObject _targetZonePrefab; // 12시 방향 강조용 스프라이트

        private LineRenderer _lineRenderer;

        // 외부에서 이 반지름을 가져다 씁니다 (중요)
        public float Radius => _radius;

        public void Initialize()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            DrawCircle();
            CreateTargetZone();
        }

        // 1. 수학 공식을 이용해 완벽한 원 그리기
        private void DrawCircle()
        {
            _lineRenderer.useWorldSpace = false;
            _lineRenderer.startWidth = _width;
            _lineRenderer.endWidth = _width;
            _lineRenderer.positionCount = _segments + 1;
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // 기본 쉐이더
            _lineRenderer.startColor = _baseColor;
            _lineRenderer.endColor = _baseColor;

            float angleStep = 360f / _segments;

            for (int i = 0; i <= _segments; i++)
            {
                float angle = i * angleStep;
                float rad = Mathf.Deg2Rad * angle;

                // 원의 좌표 공식: (r * cosθ, r * sinθ)
                float x = Mathf.Cos(rad) * _radius;
                float y = Mathf.Sin(rad) * _radius;

                _lineRenderer.SetPosition(i, new Vector3(x, y, 0));
            }
        }

        // 2. 12시 방향 타겟 마커 생성
        private void CreateTargetZone()
        {
            if (_targetZonePrefab != null)
            {
                // 12시 방향은 90도입니다.
                // 좌표: (0, Radius)
                GameObject zone = Instantiate(_targetZonePrefab, transform);
                zone.transform.localPosition = new Vector3(0, _radius, 0);
                // 필요하다면 회전이나 스케일 조정
            }
        }
    }
}
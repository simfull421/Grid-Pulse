using TouchIT.Entity;
using UnityEngine;

namespace TouchIT.Boundary
{
    public class LifeRingView : MonoBehaviour
    {
        [SerializeField] private float _radius = 3.0f;
        [SerializeField] private float _width = 0.15f;

        // 3개의 라인 렌더러 (각각 120도씩 담당)
        // Hierarchy에서 자식으로 빈 오브젝트 3개 만들고 LineRenderer 붙여서 여기 할당하세요.
        [SerializeField] private LineRenderer[] _segments;

        private int _currentLife = 3;

        public float Radius => _radius;

        public void Initialize()
        {
            _currentLife = 3;
            // 3등분 그리기 (수학 공식 적용)
            // Segment 0: 0 ~ 120도 (유니티 좌표계상 90도에서 시작해서 시계방향)
            // 시계방향 순서: 12시~4시, 4시~8시, 8시~12시

            DrawArc(_segments[0], 90f, -30f);   // 12시(90) ~ 4시(-30) : 총 120도
            DrawArc(_segments[1], -30f, -150f); // 4시(-30) ~ 8시(-150)
            DrawArc(_segments[2], -150f, -270f);// 8시(-150) ~ 12시(-270)
        }
        // [신규] 색상 변경 함수
        public void SetColor(NoteColor mode)
        {
            // 구체와 같은 색상 (White 모드면 하얀링, Black 모드면 검은링)
            Color targetColor = (mode == NoteColor.White) ? Color.white : Color.black;

            foreach (var lr in _segments)
            {
                if (lr != null)
                {
                    lr.startColor = targetColor;
                    lr.endColor = targetColor;
                }
            }
        }
        private void DrawArc(LineRenderer lr, float startAngle, float endAngle)
        {
            lr.enabled = true;
            lr.useWorldSpace = false;
            lr.startWidth = _width;
            lr.endWidth = _width;
            lr.positionCount = 30; // 점 30개로 부드럽게
            lr.material = new Material(Shader.Find("Sprites/Default")); // 기본 쉐이더
            // [중요] 쉐이더가 색상을 먹으려면 Sprites/Default여야 함
            if (lr.material == null || lr.material.name.StartsWith("Default-Line"))
            {
                lr.material = new Material(Shader.Find("Sprites/Default"));
            }
            float angleStep = (endAngle - startAngle) / 29f;

            for (int i = 0; i < 30; i++)
            {
                float angleDeg = startAngle + (angleStep * i);
                float angleRad = angleDeg * Mathf.Deg2Rad;

                float x = Mathf.Cos(angleRad) * _radius;
                float y = Mathf.Sin(angleRad) * _radius;

                lr.SetPosition(i, new Vector3(x, y, 0));
            }
        }

        public void ReduceLife()
        {
            if (_currentLife > 0)
            {
                _currentLife--;
                // 해당 구간 끄기
                if (_segments[_currentLife] != null)
                    _segments[_currentLife].enabled = false;
            }
        }
    }
}
using UnityEngine;
using System.Collections.Generic;
using TouchIT.Entity;

namespace TouchIT.Boundary
{
    // [신규] 콤보 게이지를 라이프 링처럼 칸으로 나눠서 그림
    public class ComboRingView : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _radius = 2.2f; // 라이프 링(2.8)보다 작게 (안쪽에 위치)
        [SerializeField] private float _width = 0.12f;
        [SerializeField] private float _gapSize = 5.0f; // 칸 간격을 좀 더 넓게

        private const int MAX_COMBO_GAUGE = 10; // 10개 모으면 그로기
        private List<LineRenderer> _segments = new List<LineRenderer>();

        public void Initialize()
        {
            foreach (Transform child in transform) Destroy(child.gameObject);
            _segments.Clear();

            float angleStep = 360f / MAX_COMBO_GAUGE;

            for (int i = 0; i < MAX_COMBO_GAUGE; i++)
            {
                GameObject obj = new GameObject($"ComboSeg_{i}");
                obj.transform.SetParent(transform, false);
                // Z축을 1.0으로 보내서 노트보다 뒤에, 하지만 라이프 링과는 겹치지 않게
                obj.transform.localPosition = new Vector3(0, 0, 1.0f);

                LineRenderer lr = obj.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.startWidth = _width;
                lr.endWidth = _width;
                lr.positionCount = 10;

                // 12시부터 시계방향
                float startAngle = 90f - (i * angleStep);
                float endAngle = startAngle - angleStep;

                DrawArc(lr, startAngle - _gapSize / 2, endAngle + _gapSize / 2, _radius);

                // 처음엔 꺼둠 (빈 게이지)
                lr.enabled = false;

                // 색상 (노란색/골드 느낌)
                Color c = new Color(1f, 0.8f, 0.2f, 1f);
                lr.startColor = c; lr.endColor = c;

                _segments.Add(lr);
            }
        }

        private void DrawArc(LineRenderer lr, float startAngle, float endAngle, float radius)
        {
            int points = lr.positionCount;
            for (int i = 0; i < points; i++)
            {
                float t = (float)i / (points - 1);
                float rad = Mathf.Lerp(startAngle, endAngle, t) * Mathf.Deg2Rad;
                lr.SetPosition(i, new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0));
            }
        }

        // 0.0 ~ 1.0 (fillAmount)
        public void UpdateGauge(float fillAmount)
        {
            // 10칸 중 몇 칸 켤지 계산
            int count = Mathf.RoundToInt(fillAmount * MAX_COMBO_GAUGE);

            for (int i = 0; i < MAX_COMBO_GAUGE; i++)
            {
                // 채워진 만큼 켜기
                bool isOn = i < count;
                _segments[i].enabled = isOn;

                // [Juice] 방금 켜진 칸은 살짝 반짝이거나 커지는 효과 주면 좋음 (여기선 생략)
            }
        }

        public void SetColor(Color c)
        {
            foreach (var seg in _segments) { seg.startColor = c; seg.endColor = c; }
        }
    }
}
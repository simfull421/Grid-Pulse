using UnityEngine;
using System.Collections.Generic;
using TouchIT.Entity;

namespace TouchIT.Boundary
{
    [RequireComponent(typeof(LineRenderer))]
    public class LifeRingView : MonoBehaviour
    {
        [Header("Ring Settings")]
        [SerializeField] private Color _goldColor = new Color(1f, 0.84f, 0f, 1f); // ✨ 골드
        [SerializeField] private int _resolution = 128; // 원을 그리는 점의 개수
        [SerializeField] private float _baseRadius = 3.0f; // 기본 반지름 R_base
        [SerializeField] private float _width = 0.1f;

        [Header("Spike Settings (Cos^5)")]
        [SerializeField] private int _sharpnessPower = 5; // n = 5 (홀수 추천)
        [SerializeField] private float _spikeWidth = 2.0f; // W (가시의 너비)
        [SerializeField] private float _decaySpeed = 5.0f; // 줄어드는 속도

        // 내부 클래스: 발생한 충격(Pulse) 관리
        private class ActivePulse
        {
            public float Angle; // 타겟 각도 (Theta_target)
            public float Power; // 진폭 (Power)
        }

        private List<ActivePulse> _pulses = new List<ActivePulse>();
        private LineRenderer _lineRenderer;

        public void Initialize()
        {
            _lineRenderer = GetComponent<LineRenderer>();

            // 🛠️ 렌더러 필수 설정 강제 적용
            _lineRenderer.positionCount = 128 + 1;
            _lineRenderer.startWidth = _width;
            _lineRenderer.endWidth = _width;
            _lineRenderer.useWorldSpace = false;
            _lineRenderer.loop = true;

            // 쉐이더가 없으면 기본 스프라이트 쉐이더 생성 (보라색 박스 방지)
            if (_lineRenderer.material == null || _lineRenderer.material.name.StartsWith("Default-Material"))
            {
                _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            }

            // 색상 적용 (골드)
            _lineRenderer.startColor = _goldColor;
            _lineRenderer.endColor = _goldColor;

            UpdateRingVisual();
        }

        private void Update()
        {
            if (_pulses.Count > 0)
            {
                // 1. 펄스 감쇠 (시간이 지나면 줄어듦)
                for (int i = _pulses.Count - 1; i >= 0; i--)
                {
                    _pulses[i].Power -= Time.deltaTime * _decaySpeed;
                    if (_pulses[i].Power <= 0.01f)
                    {
                        _pulses.RemoveAt(i);
                    }
                }

                // 2. 링 모양 갱신
                UpdateRingVisual();
            }
        }

        // 외부(System)에서 비트가 감지되면 호출
        public void ApplyBassImpulse(float power)
        {
            // 랜덤한 위치 혹은 정해진 위치에 가시 생성
            // (여기선 시각적 효과를 위해 4방향 혹은 랜덤)
            float randomAngle = Random.Range(0f, 360f);

            _pulses.Add(new ActivePulse
            {
                Angle = randomAngle,
                Power = power
            });
        }

        // 📐 핵심 수학 공식 적용: R(theta) = R_base + Sum(Offset)
        private void UpdateRingVisual()
        {
            Vector3[] positions = new Vector3[_resolution + 1];
            float angleStep = 360f / _resolution;

            for (int i = 0; i <= _resolution; i++)
            {
                float thetaDeg = i * angleStep;
                float thetaRad = thetaDeg * Mathf.Deg2Rad;

                // 1. 오프셋 계산 (모든 활성 펄스의 영향 합산)
                float totalOffset = 0f;
                foreach (var pulse in _pulses)
                {
                    totalOffset += CalculateSpikeOffset(thetaDeg, pulse);
                }

                // 2. 최종 반지름
                float r = _baseRadius + totalOffset;

                // 3. 극좌표 -> 직교좌표 변환
                float x = r * Mathf.Cos(thetaRad);
                float y = r * Mathf.Sin(thetaRad);

                positions[i] = new Vector3(x, y, 0f);
            }

            _lineRenderer.SetPositions(positions);
        }

        // 📐 뾰족한 형상 변형 함수 (The Sharpening Function)
        private float CalculateSpikeOffset(float currentAngle, ActivePulse pulse)
        {
            // 각도 차이 (-180 ~ 180 보정)
            float diff = Mathf.DeltaAngle(currentAngle, pulse.Angle);

            // 범위 내에 있는지 확인 (|diff| < W)
            // W를 조금 넉넉하게 잡고 Cos 변형
            float range = 45f / _spikeWidth; // 너비 조절 계수

            if (Mathf.Abs(diff) < 90f) // 90도 안쪽만 영향
            {
                // Cos 함수 적용 (0 ~ 1 사이 값)
                // 각도 차이가 0일 때 1, 멀어질수록 0
                float normalizedDiff = diff * Mathf.Deg2Rad * _spikeWidth;
                float baseCos = Mathf.Cos(normalizedDiff);

                if (baseCos > 0)
                {
                    // ⭐️ 핵심: 5제곱 (n=5) -> 뾰족하게 만듦
                    float sharpShape = Mathf.Pow(baseCos, _sharpnessPower);
                    return sharpShape * pulse.Power;
                }
            }
            return 0f;
        }
    }
}
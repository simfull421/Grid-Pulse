using UnityEngine;
using System.Collections.Generic;

namespace TouchIT.Boundary
{
    [RequireComponent(typeof(LineRenderer))]
    public class LifeRingView : MonoBehaviour
    {
        [Header("Ring Settings")]
        [SerializeField] private Color _goldColor = new Color(1f, 0.84f, 0f, 1f);
        [SerializeField] private int _resolution = 128;
        [SerializeField] private float _baseRadius = 3.0f;
        [SerializeField] private float _width = 0.1f;

        [Header("Spike Settings (Cos^5 Magic)")]
        [SerializeField] private int _sharpnessPower = 5; // 뾰족함의 핵심 (홀수)
        [SerializeField] private float _spikeWidth = 2.0f; // 가시 너비
        [SerializeField] private float _decaySpeed = 8.0f; // 사라지는 속도

        private class ActivePulse
        {
            public float Angle;
            public float Power;
        }

        private List<ActivePulse> _pulses = new List<ActivePulse>();
        private LineRenderer _lineRenderer;

        // 전체 링이 둠칫하는 반응 (MainView 호환용)
        private float _globalExpand = 0f;

        public void Initialize()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.positionCount = _resolution + 1;
            _lineRenderer.startWidth = _width;
            _lineRenderer.endWidth = _width;
            _lineRenderer.useWorldSpace = false;
            _lineRenderer.loop = true;

            if (_lineRenderer.material == null || _lineRenderer.material.name.StartsWith("Default-Material"))
                _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

            _lineRenderer.startColor = _goldColor;
            _lineRenderer.endColor = _goldColor;

            UpdateRingVisual();
        }

        private void Update()
        {
            bool needsUpdate = false;

            // 1. 전체 링 반응 감쇠
            if (_globalExpand > 0.001f)
            {
                _globalExpand -= Time.deltaTime * 5.0f;
                needsUpdate = true;
            }

            // 2. 가시(Pulse) 감쇠
            if (_pulses.Count > 0)
            {
                for (int i = _pulses.Count - 1; i >= 0; i--)
                {
                    _pulses[i].Power -= Time.deltaTime * _decaySpeed;
                    if (_pulses[i].Power <= 0.01f) _pulses.RemoveAt(i);
                }
                needsUpdate = true;
            }

            if (needsUpdate) UpdateRingVisual();
        }

        // 🔊 음악 비트에 반응 (짧은 가시)
        public void ApplyBassImpulse(float power)
        {
            float randomAngle = Random.Range(0f, 360f);
            _pulses.Add(new ActivePulse
            {
                Angle = randomAngle,
                Power = power * 1.5f // 적당한 크기
            });
        }

        // 💥 [추가] 터치 성공 시 호출 (MainView 에러 해결)
        // 더 길고 강력한 가시를 만듭니다.
        public void OnHitEffect()
        {
            // 1. 전체 링 살짝 커짐
            _globalExpand = 0.15f;

            // 2. 랜덤 위치에 아주 긴 가시 생성 (피크 느낌)
            float randomAngle = Random.Range(0f, 360f);
            _pulses.Add(new ActivePulse
            {
                Angle = randomAngle,
                Power = 0.8f // BassImpulse보다 훨씬 큼 (길게 찌름)
            });
        }

        private void UpdateRingVisual()
        {
            Vector3[] positions = new Vector3[_resolution + 1];
            float angleStep = 360f / _resolution;
            float currentBaseRadius = _baseRadius + _globalExpand;

            for (int i = 0; i <= _resolution; i++)
            {
                float thetaDeg = i * angleStep;
                float thetaRad = thetaDeg * Mathf.Deg2Rad;

                // 1. 가시 오프셋 계산 (Cos^5 방식)
                float totalOffset = 0f;
                foreach (var pulse in _pulses)
                {
                    totalOffset += CalculateSpikeOffset(thetaDeg, pulse);
                }

                // 2. 32각 그리드 (미세 떨림)
                float staticGrid = Mathf.Cos(thetaRad * 32) * 0.03f;

                float r = currentBaseRadius + totalOffset + staticGrid;
                positions[i] = new Vector3(Mathf.Cos(thetaRad) * r, Mathf.Sin(thetaRad) * r, 0f);
            }
            _lineRenderer.SetPositions(positions);
        }

        // 📐 [복구됨] 오리지널 Cos^5 공식
        private float CalculateSpikeOffset(float currentAngle, ActivePulse pulse)
        {
            float diff = Mathf.DeltaAngle(currentAngle, pulse.Angle);

            // _spikeWidth가 클수록 좁고 날카로움
            float normalizedDiff = diff * Mathf.Deg2Rad * _spikeWidth;

            // -90도 ~ 90도 범위 내에서만 코사인 적용
            if (Mathf.Abs(normalizedDiff) < Mathf.PI * 0.5f)
            {
                float baseCos = Mathf.Cos(normalizedDiff);
                if (baseCos > 0)
                {
                    // 5제곱 -> 뾰족한 바늘 모양
                    float sharpShape = Mathf.Pow(baseCos, _sharpnessPower);
                    return sharpShape * pulse.Power;
                }
            }
            return 0f;
        }
    }
}
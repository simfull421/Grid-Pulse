using UnityEngine;
using System.Collections.Generic;
using TouchIT.Entity;

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

        [Header("Reaction")]
        [SerializeField] private float _impactScale = 0.3f; // 💥 타격 시 링이 커지는 정도
        [SerializeField] private float _decaySpeed = 5.0f;  // 줄어드는 속도

        [Header("Spike Settings (Cos^5)")]
        [SerializeField] private int _sharpnessPower = 5;
        [SerializeField] private float _spikeWidth = 2.0f;

        // 펄스(가시) 관리 클래스
        private class ActivePulse
        {
            public float Angle;
            public float Power;
        }

        private List<ActivePulse> _pulses = new List<ActivePulse>();
        private LineRenderer _lineRenderer;

        // 💥 전체 링 튕김 효과 변수
        private float _currentImpact = 0f;

        public void Initialize()
        {
            Debug.Log($"💍 LifeRingView: Initialized! (GameObject: {gameObject.name})");
            _lineRenderer = GetComponent<LineRenderer>();

            _lineRenderer.positionCount = 128 + 1;
            _lineRenderer.startWidth = _width;
            _lineRenderer.endWidth = _width;
            _lineRenderer.useWorldSpace = false;
            _lineRenderer.loop = true;

            if (_lineRenderer.material == null || _lineRenderer.material.name.StartsWith("Default-Material"))
            {
                _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            }

            _lineRenderer.startColor = _goldColor;
            _lineRenderer.endColor = _goldColor;

            UpdateRingVisual();
        }

        private void Update()
        {
            bool needsUpdate = false;

            // 1. 전체 링 튕김 감쇠 (둠칫 -> 원래대로)
            if (_currentImpact > 0.001f)
            {
                _currentImpact -= Time.deltaTime * _decaySpeed;
                if (_currentImpact < 0) _currentImpact = 0f;
                needsUpdate = true;
            }

            // 2. 가시(Pulse) 감쇠
            if (_pulses.Count > 0)
            {
                for (int i = _pulses.Count - 1; i >= 0; i--)
                {
                    _pulses[i].Power -= Time.deltaTime * _decaySpeed;
                    if (_pulses[i].Power <= 0.01f)
                    {
                        _pulses.RemoveAt(i);
                    }
                }
                needsUpdate = true;
            }

            // 변화가 있을 때만 다시 그림
            if (needsUpdate)
            {
                UpdateRingVisual();
            }
        }

        // ✅ [추가됨] MainView에서 호출하는 타격 반응 함수
        public void OnHitEffect()
        {
            // 링 전체 반지름을 일시적으로 키움
            _currentImpact = _impactScale;
            UpdateRingVisual(); // 즉시 반영
        }

        public void ApplyBassImpulse(float power)
        {
            float randomAngle = Random.Range(0f, 360f);
            _pulses.Add(new ActivePulse
            {
                Angle = randomAngle,
                Power = power
            });
        }

        private void UpdateRingVisual()
        {
            Vector3[] positions = new Vector3[_resolution + 1];
            float angleStep = 360f / _resolution;

            // 💥 현재 반지름 = 기본 반지름 + 타격 임팩트
            float currentRadius = _baseRadius + _currentImpact;

            for (int i = 0; i <= _resolution; i++)
            {
                float thetaDeg = i * angleStep;
                float thetaRad = thetaDeg * Mathf.Deg2Rad;

                // 1. 가시(Spike) 오프셋 계산
                float totalOffset = 0f;
                foreach (var pulse in _pulses)
                {
                    totalOffset += CalculateSpikeOffset(thetaDeg, pulse);
                }

                // 2. 32각 그리드 느낌 (미세한 굴곡 추가)
                // 32번 출렁이게 해서 각진 느낌을 줌
                float gridEffect = Mathf.Cos(thetaRad * 32) * 0.03f;

                // 최종 반지름 결정
                float r = currentRadius + totalOffset + gridEffect;

                // 극좌표 -> 직교좌표
                float x = r * Mathf.Cos(thetaRad);
                float y = r * Mathf.Sin(thetaRad);

                positions[i] = new Vector3(x, y, 0f);
            }

            _lineRenderer.SetPositions(positions);
        }

        private float CalculateSpikeOffset(float currentAngle, ActivePulse pulse)
        {
            float diff = Mathf.DeltaAngle(currentAngle, pulse.Angle);
            if (Mathf.Abs(diff) < 90f)
            {
                float normalizedDiff = diff * Mathf.Deg2Rad * _spikeWidth;
                float baseCos = Mathf.Cos(normalizedDiff);

                if (baseCos > 0)
                {
                    return Mathf.Pow(baseCos, _sharpnessPower) * pulse.Power;
                }
            }
            return 0f;
        }
    }
}
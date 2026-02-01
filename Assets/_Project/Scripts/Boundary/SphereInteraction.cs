using UnityEngine;
using TouchIT.Control;

namespace TouchIT.Boundary
{
    // [중요] 이 컴포넌트를 쓰려면 CircleCollider2D가 무조건 있어야 함
    [RequireComponent(typeof(CircleCollider2D))]
    public class SphereInteraction : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _elasticity = 15f; // 복귀 속도
        [SerializeField] private float _dragRadius = 1.5f; // 구체가 움직일 수 있는 최대 범위 (늘림)
        [SerializeField] private float _shakeThreshold = 10.0f; // 흔들기 감도
        [SerializeField] private ParticleSystem _bubbleParticle; // [신규] 파티클 연결
        private Vector3 _initialPos = Vector3.zero;
        private bool _isGroggy = false;
        private bool _isDragging = false;
        private GameMain _gameMain;
        private Vector3 _lastPos;
        // [신규] 진동 쿨타임 변수
        private float _lastVibrateTime;
        private const float VIBRATE_COOLDOWN = 0.05f; // 0.05초마다 진동 (드르르륵 느낌)
        private const float SOUND_COOLDOWN = 0.1f; // 0.1초 쿨타임
        public void Initialize(GameMain main)
        {
            _gameMain = main;
            _initialPos = transform.localPosition;
        }

        public void SetGroggyMode(bool isOn)
        {
            _isGroggy = isOn;

            // 그로기 끝나면 강제 복귀 및 크기 초기화
            if (!isOn)
            {
                _isDragging = false;
                transform.localPosition = _initialPos;
                transform.localScale = Vector3.one;
            }
        }

        private void OnMouseDown()
        {
            if (!_isGroggy) return;

            _isDragging = true;
            _lastPos = transform.position;
            transform.localScale = Vector3.one * 1.2f;

            // [수정] 누르자마자 파티클 강제 재생
            if (_bubbleParticle) _bubbleParticle.Play();
        }

        private void OnMouseUp()
        {
            if (!_isGroggy) return;

            _isDragging = false;
            transform.localScale = Vector3.one;

            // [수정] 떼면 파티클 정지 (비눗방울 끊기게)
            if (_bubbleParticle) _bubbleParticle.Stop();
        }
        private void Update()
        {
            if (!_isGroggy) return;

            Vector3 targetPos = _initialPos;

            if (_isDragging)
            {
                // 1. 마우스 위치를 월드 좌표로 변환
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mousePos.z = 0; // 2D게임이므로 Z는 0으로 고정

                // 2. 원점에서 너무 멀어지지 않게 제한 (고무줄 효과)
                Vector3 offset = mousePos - _initialPos;
                targetPos = _initialPos + Vector3.ClampMagnitude(offset, _dragRadius);

                // 3. 흔들기 감지 (이동 속도 체크)
                // 단순히 위치 차이가 아니라, 이전 프레임 대비 얼마나 움직였는가?
                float speed = (transform.position - _lastPos).magnitude / Time.deltaTime;
                if (speed > _shakeThreshold)
                {
                    _gameMain.AddShakeScore();

                    // [신규] 진동 로직 추가
                    if (Time.time - _lastVibrateTime > VIBRATE_COOLDOWN)
                    {
                        // 안드로이드 기준 30ms (아주 짧게 틱!)
                        Vibration.Vibrate(30);
                        // 2. 소리 (GameMain을 통해 재생)
                        _gameMain.PlayShakeSound();
                        _lastVibrateTime = Time.time;
                    }
                }

                _lastPos = transform.position;

                // 드래그 중에는 즉시 따라옴 (Lerp 쓰면 반응 느림)
                transform.localPosition = targetPos;
            }
            else
            {
                // 놓았을 때는 탄성 있게 제자리로 복귀
                transform.localPosition = Vector3.Lerp(transform.localPosition, _initialPos, Time.deltaTime * _elasticity);
            }
        }
    }
}
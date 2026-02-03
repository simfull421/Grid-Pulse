using UnityEngine;

namespace TouchIT.Boundary
{
    public class EmberController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _maxScale = 1.5f;
        [SerializeField] private float _minScale = 0.3f;
        [SerializeField] private float _burnRate = 0.05f; // 초당 줄어드는 속도
        [SerializeField] private float _fuelAmount = 0.2f; // 노트 성공 시 회복량

        private Transform _transform;
        private float _currentFuel = 1.0f; // 0.0 ~ 1.0

        private void Awake()
        {
            _transform = transform;
        }

        private void Update()
        {
            // 1. 시간이 지날수록 연료(크기) 감소
            _currentFuel -= _burnRate * Time.deltaTime;

            if (_currentFuel <= 0)
            {
                _currentFuel = 0;
                // Game Over 로직 호출
                Debug.Log("🔥 불 꺼짐! GAME OVER");
            }

            // 2. 크기 반영 (서서히 바뀜)
            float targetScale = Mathf.Lerp(_minScale, _maxScale, _currentFuel);
            _transform.localScale = Vector3.Lerp(_transform.localScale, Vector3.one * targetScale, Time.deltaTime * 5f);

            // 3. 어둠 효과 (Fog)
            // 연료가 30% 미만이면 화면이 어두워짐 (RenderSettings.ambientLight 조절 등)
        }

        // 노트 히트 시 호출
        public void AddFuel()
        {
            _currentFuel += _fuelAmount;
            if (_currentFuel > 1.0f) _currentFuel = 1.0f;

            // 불꽃 확 타오르는 이펙트 (Scale Punch)
            _transform.localScale *= 1.2f;
        }

        // 그로기(마찰) 시 호출
        public void SuperCharge(float amount)
        {
            // 마찰열로 연료 대폭 회복 및 한계 돌파(파란불) 가능
            _currentFuel += amount;
        }
    }
}
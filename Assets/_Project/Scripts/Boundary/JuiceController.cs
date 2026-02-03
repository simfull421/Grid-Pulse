using UnityEngine;

namespace TouchIT.Boundary
{
    public class JuiceController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Renderer _sphereRenderer;
        [SerializeField] private LifeRingView _lifeRingView;
        [SerializeField] private AudioSource _musicSource;

        [Header("Slime Physics")]
        [SerializeField] private float _wobbleSens = 1.5f; // 관성 민감도
        [SerializeField] private float _recoverySpeed = 10f; // 원래대로 돌아오는 속도

        private float[] _spectrum = new float[64];
        private Material _sphereMat;

        // 물리 계산용
        private Vector3 _lastPos;
        private Vector3 _inertiaVector; // 쉐이더에 전달할 관성 값
        private float _beatTimer;

        private void Start()
        {
            // 자동 연결
            if (_sphereRenderer == null)
            {
                var sphereView = FindFirstObjectByType<SphereView>();
                if (sphereView) _sphereRenderer = sphereView.GetComponent<Renderer>();
            }
            if (_sphereRenderer)
            {
                _sphereMat = _sphereRenderer.material;
                _lastPos = _sphereRenderer.transform.position;
            }
            if (_lifeRingView == null) _lifeRingView = FindFirstObjectByType<LifeRingView>();
        }

        private void Update()
        {
            // 1. [슬라임 물리 계산]
            if (_sphereMat && _sphereRenderer)
            {
                Vector3 currentPos = _sphereRenderer.transform.position;

                // 이동량 (Velocity)
                Vector3 moveDelta = (currentPos - _lastPos);

                // 관성 누적 (이동하는 방향으로 관성이 생김)
                // 급격하게 움직이면 moveDelta가 커져서 쉐이더가 많이 찌그러짐
                Vector3 targetInertia = moveDelta * _wobbleSens;

                // 부드럽게 보간 (스프링 효과)
                _inertiaVector = Vector3.Lerp(_inertiaVector, targetInertia, Time.deltaTime * 5f);

                // 복원력 (가만히 있으면 0으로 돌아옴)
                _inertiaVector = Vector3.Lerp(_inertiaVector, Vector3.zero, Time.deltaTime * _recoverySpeed);

                // 쉐이더에 전달
                _sphereMat.SetVector("_Inertia", new Vector4(_inertiaVector.x, _inertiaVector.y, _inertiaVector.z, 0));

                _lastPos = currentPos;
            }

            // 2. 링 펀치 (기존 유지)
            HandleRingPunch();
        }

        // 충돌 시 (물리적 충격)
        private void OnCollisionEnter2D(Collision2D collision)
        {
            // 충돌 반대 방향으로 관성을 확 줘서 찌그러뜨림
            Vector3 normal = collision.contacts[0].normal;
            float force = collision.relativeVelocity.magnitude;

            // 충돌 방향으로 쉐이더를 밀어버림 (납작해짐)
            _inertiaVector += (Vector3)normal * Mathf.Clamp(force * 0.2f, 0f, 1.0f);
        }

        private void HandleRingPunch()
        {
            if (!_lifeRingView || !_musicSource || !_musicSource.isPlaying) return;

            _beatTimer += Time.deltaTime;
            float bassEnergy = 0f;
            _musicSource.GetSpectrumData(_spectrum, 0, FFTWindow.Rectangular);
            for (int i = 0; i < 7; i++) bassEnergy += _spectrum[i];

            if (_beatTimer > 0.1f && (bassEnergy * 5.0f) > 0.2f)
            {
                _lifeRingView.ApplyAudioImpulse(bassEnergy * 5.0f);
                _beatTimer = 0f;
            }
        }

        // [삭제됨] PunchSphere() - 터치 성공 시 인위적인 흔들림 제거
    }
}
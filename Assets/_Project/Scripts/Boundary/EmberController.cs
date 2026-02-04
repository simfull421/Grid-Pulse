using UnityEngine;

namespace TouchIT.Boundary
{
    // [비주얼 전담] 물리 X, 오직 연출만
    public class EmberController : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private ParticleSystem _fireParticles;

        private Vector3 _originalScale;

        public void Initialize()
        {
            _originalScale = _visualRoot.localScale;

            // 파티클 켜기
            if (_fireParticles && !_fireParticles.isPlaying) _fireParticles.Play();
        }

        // 0.0(기본) ~ 1.0(화면 가득)
        public void SetZoomVisual(float progress)
        {
            // 1. 크기 확대 (비선형으로 가속도 붙게)
            // 1배 -> 30배까지 확대
            float scaleMultiplier = Mathf.Lerp(1.0f, 30.0f, progress * progress);
            _visualRoot.localScale = _originalScale * scaleMultiplier;

            // 2. 파티클 속도 (빨려 들어가는 느낌)
            if (_fireParticles)
            {
                var main = _fireParticles.main;
                // 진행도에 따라 시뮬레이션 속도 증가 (1배 -> 5배)
                main.simulationSpeed = 1.0f + (progress * 4.0f);
            }
        }
    }
}
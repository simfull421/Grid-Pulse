using UnityEngine;

namespace TouchIT.Boundary
{
    // [중요] Collider 필요 없음! (Controller가 거리 계산함)
    public class SphereInteraction : MonoBehaviour
    {
        private Vector3 _initialPos;
        [SerializeField] private ParticleSystem _bubbleParticle;

        public void Initialize()
        {
            _initialPos = transform.localPosition;
        }

        public void SetGroggyVisual(bool isGroggy)
        {
            if (isGroggy)
            {
                transform.localScale = Vector3.one * 1.2f; // 커짐
                if (_bubbleParticle) _bubbleParticle.Play();
            }
            else
            {
                transform.localScale = Vector3.one; // 복귀
                transform.localPosition = _initialPos;
                if (_bubbleParticle) _bubbleParticle.Stop();
            }
        }

        // Controller가 매 프레임 위치를 꽂아줌
        public void UpdatePosition(Vector3 newPos)
        {
            transform.localPosition = newPos;
        }
    }
}
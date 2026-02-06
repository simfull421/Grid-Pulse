using UnityEngine;

namespace TouchIT.Control
{
    public class VFXService
    {
        private readonly IVFXFactory _factory;

        public VFXService(IVFXFactory factory)
        {
            _factory = factory;
        }

        public void PlayHitEffect(Vector3 position)
        {
            _factory.PlayHitEffect(position);
        }
    }
}
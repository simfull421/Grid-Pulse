using UnityEngine;
using System.Collections.Generic;
using TouchIT.Control;

namespace TouchIT.Boundary
{
    public class VFXFactory : MonoBehaviour, IVFXFactory
    {
        [SerializeField] private ParticleSystem _hitEffectPrefab;
        [SerializeField] private int _poolSize = 20;

        private Queue<ParticleSystem> _pool = new Queue<ParticleSystem>();
        private Transform _container;

        public void Initialize()
        {
            if (_hitEffectPrefab == null)
            {
                Debug.LogError("❌ VFXFactory: Hit Effect Prefab Missing!");
                return;
            }

            _container = new GameObject("VFX_Pool").transform;
            _container.SetParent(transform);

            for (int i = 0; i < _poolSize; i++)
            {
                var vfx = Instantiate(_hitEffectPrefab, _container);
                vfx.gameObject.SetActive(false);
                _pool.Enqueue(vfx);
            }
        }

        public void PlayHitEffect(Vector3 position)
        {
            if (_pool.Count == 0) return;

            ParticleSystem vfx = _pool.Dequeue();

            vfx.transform.position = position;
            vfx.gameObject.SetActive(true);
            vfx.Play();

            // 파티클의 Stop Action이 Disable로 설정되어 있어야 자동으로 꺼짐
            // 재사용을 위해 다시 큐에 넣음
            _pool.Enqueue(vfx);
        }
    }
}
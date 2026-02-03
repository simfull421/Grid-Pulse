using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TouchIT.Entity;

namespace TouchIT.Boundary
{
    public class VfxManager : MonoBehaviour
    {
        [Header("Containers")]
        [SerializeField] private Transform _particleContainer;

        [Header("Prefabs")]
        [SerializeField] private ParticleSystem _sparkPrefab;

        [SerializeField] private ParticleSystem _groggyBubblePrefab;
        [SerializeField] private ParticleSystem _holdLoopEffectPrefab;

        // 내부 상태
        private Queue<ParticleSystem> _sparkPool = new Queue<ParticleSystem>();
        private ParticleSystem _activeHoldEffect;

        // 카메라 줌 관련
        private Camera _mainCam;
        private float _defaultCamSize;
        private Coroutine _zoomCoroutine;

        public void Initialize()
        {
            _mainCam = Camera.main;
            if (_mainCam) _defaultCamSize = _mainCam.orthographicSize;

            // 스파크 풀링 생성
            if (_sparkPrefab != null && _particleContainer != null)
            {
                for (int i = 0; i < 10; i++)
                {
                    var p = Instantiate(_sparkPrefab, _particleContainer);
                    p.gameObject.SetActive(false);
                    _sparkPool.Enqueue(p);
                }
            }

            // 홀드 이펙트 생성
            if (_holdLoopEffectPrefab != null && _particleContainer != null)
            {
                _activeHoldEffect = Instantiate(_holdLoopEffectPrefab, _particleContainer);
                var main = _activeHoldEffect.main;
                main.loop = true;
                _activeHoldEffect.gameObject.SetActive(false);
            }
        }

        public void SetHoldEffectPos(Vector3 pos)
        {
            if (_activeHoldEffect) _activeHoldEffect.transform.localPosition = pos;
        }

        public void SetHoldEffectState(bool isHolding)
        {
            if (_activeHoldEffect == null) return;

            if (isHolding && !_activeHoldEffect.gameObject.activeSelf)
            {
                _activeHoldEffect.gameObject.SetActive(true);
                _activeHoldEffect.Play();
            }
            else if (!isHolding && _activeHoldEffect.gameObject.activeSelf)
            {
                _activeHoldEffect.Stop();
                _activeHoldEffect.gameObject.SetActive(false);
            }
        }

        public void PlayHitEffect(Vector3 position, NoteColor color, float radius)
        {
            var theme = ThemeColors.GetColors(color);
            Color visualColor = (color == NoteColor.Cosmic) ? Color.cyan : theme.Note;

            // 1. 줌 이펙트
            if (_zoomCoroutine != null) StopCoroutine(_zoomCoroutine);
            _zoomCoroutine = StartCoroutine(HitZoomEffect());

            // 2. 스파크 (풀링)
            if (_sparkPool.Count > 0)
            {
                var spark = _sparkPool.Dequeue();
                spark.transform.position = position; // 위치 지정
                spark.transform.rotation = Quaternion.identity;
                spark.gameObject.SetActive(true);

                var main = spark.main;
                main.startColor = visualColor;

                spark.Play();
                StartCoroutine(ReturnSparkToPool(spark));
            }

        }

        public void PlayGroggyBubble(Vector3 centerPos, NoteColor theme)
        {
            if (_groggyBubblePrefab)
            {
                Vector3 spawnPos = centerPos + (Vector3)(Random.insideUnitCircle * 1.5f);
                var bubble = Instantiate(_groggyBubblePrefab, spawnPos, Quaternion.identity);
                var main = bubble.main;
                main.startColor = (theme == NoteColor.White) ? Color.black : Color.white;
                Destroy(bubble.gameObject, 1.0f);
            }
        }

        private IEnumerator ReturnSparkToPool(ParticleSystem spark)
        {
            yield return new WaitForSeconds(0.5f);
            spark.gameObject.SetActive(false);
            _sparkPool.Enqueue(spark);
        }

        private IEnumerator HitZoomEffect()
        {
            if (_mainCam == null) yield break;
            float targetSize = _defaultCamSize + 0.2f;

            // Zoom Out
            float elapsed = 0f;
            while (elapsed < 0.05f)
            {
                elapsed += Time.deltaTime;
                _mainCam.orthographicSize = Mathf.Lerp(_defaultCamSize, targetSize, elapsed / 0.05f);
                yield return null;
            }

            // Return
            elapsed = 0f;
            while (elapsed < 0.15f)
            {
                elapsed += Time.deltaTime;
                _mainCam.orthographicSize = Mathf.Lerp(targetSize, _defaultCamSize, elapsed / 0.15f);
                yield return null;
            }
            _mainCam.orthographicSize = _defaultCamSize;
        }
    }
}
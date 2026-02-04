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

        public void PlayHitEffect(Vector3 position, NoteType type, float radius)
        {
            // [수정] NoteType에 따라 색상 결정
            // 일반 노트(Normal) -> NoteNormal (초록)
            // 홀드 노트(Hold) -> NoteHold (청록/파랑)
            Color visualColor = (type == NoteType.Hold) ? ThemeColors.NoteHold : ThemeColors.NoteNormal;

            // 1. 줌 이펙트
            if (_zoomCoroutine != null) StopCoroutine(_zoomCoroutine);
            _zoomCoroutine = StartCoroutine(HitZoomEffect());

            // 2. 스파크 (풀링)
            if (_sparkPool.Count > 0)
            {
                var spark = _sparkPool.Dequeue();
                spark.transform.position = position;
                spark.transform.rotation = Quaternion.identity;
                spark.gameObject.SetActive(true);

                var main = spark.main;
                main.startColor = visualColor;

                spark.Play();
                StartCoroutine(ReturnSparkToPool(spark));
            }
        }

        // [삭제] PlayGroggyBubble 메서드는 더 이상 사용하지 않음 (EmberController가 담당하거나 삭제)

        private IEnumerator ReturnSparkToPool(ParticleSystem spark)
        {
            yield return new WaitForSeconds(0.5f);
            spark.gameObject.SetActive(false);
            _sparkPool.Enqueue(spark);
        }

        private IEnumerator HitZoomEffect()
        {
            if (_mainCam == null) yield break;
            float targetSize = _defaultCamSize + 0.1f; // 줌 강도 약간 줄임 (0.2 -> 0.1)

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
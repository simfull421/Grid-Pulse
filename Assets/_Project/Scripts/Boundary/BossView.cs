using UnityEngine;
using System.Collections.Generic;

namespace TouchIT.Boundary
{
    public class BossView : MonoBehaviour
    {
        [Header("Boss Visual")]
        [SerializeField] private SpriteRenderer _bossSprite;
        [SerializeField] private Color _normalColor = Color.black;
        [SerializeField] private Color _hitColor = new Color(0.2f, 0.2f, 0.2f, 1f); // 피격시 살짝 밝게

        [Header("Life Orbit (Satellites)")]
        [SerializeField] private GameObject _orbitPrefab; // 작은 원(위성) 스프라이트
        [SerializeField] private float _orbitRadius = 1.5f; // 보스와 링 사이 거리
        [SerializeField] private float _orbitSpeed = 50f;

        private List<GameObject> _activeOrbits = new List<GameObject>();
        private int _maxLives = 3;

        private void Update()
        {
            // 위성들 공전시키기 (빙글빙글)
            if (_activeOrbits.Count > 0)
            {
                float angleStep = 360f / _activeOrbits.Count;
                float currentBaseAngle = Time.time * _orbitSpeed;

                for (int i = 0; i < _activeOrbits.Count; i++)
                {
                    float angle = currentBaseAngle + (i * angleStep);
                    float rad = angle * Mathf.Deg2Rad;

                    // (0,0) 기준 공전 좌표
                    float x = Mathf.Cos(rad) * _orbitRadius;
                    float y = Mathf.Sin(rad) * _orbitRadius;

                    _activeOrbits[i].transform.localPosition = new Vector3(x, y, 0);
                }
            }
        }

        // 게임 시작 시 라이프 생성
        public void InitializeLives(int maxLives)
        {
            _maxLives = maxLives;
            // 기존 것 삭제
            foreach (var orbit in _activeOrbits) Destroy(orbit);
            _activeOrbits.Clear();

            for (int i = 0; i < maxLives; i++)
            {
                GameObject orbit = Instantiate(_orbitPrefab, transform);
                _activeOrbits.Add(orbit);
            }
        }

        // 데미지 입었을 때 라이프 하나 파괴
        public void ReduceLife(int currentLife)
        {
            if (_activeOrbits.Count > currentLife)
            {
                // 하나 제거 (파티클 이펙트 추가 가능)
                GameObject lostOrbit = _activeOrbits[_activeOrbits.Count - 1];
                _activeOrbits.RemoveAt(_activeOrbits.Count - 1);
                Destroy(lostOrbit); // 나중에 파티클로 교체

                // 남은 위성들끼리 간격 재배치됨 (Update 로직 덕분)
            }
        }

        // 피격 연출 (보스 줌아웃 킥)
        public void PlayHitEffect()
        {
            StartCoroutine(HitRoutine());
        }

        private System.Collections.IEnumerator HitRoutine()
        {
            // 1. 색상 변경 & 축소
            _bossSprite.color = _hitColor;
            transform.localScale = Vector3.one * 0.9f;

            yield return new WaitForSeconds(0.05f); // 찰나의 순간

            // 2. 복구
            _bossSprite.color = _normalColor;
            transform.localScale = Vector3.one;
        }
    }
}
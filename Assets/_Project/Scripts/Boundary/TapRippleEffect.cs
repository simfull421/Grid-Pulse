using UnityEngine;
using UniRx;
using DG.Tweening;
using System.Collections.Generic;

namespace TouchIT.Boundary
{
    public class TapRippleEffect : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputAnalyzer _inputAnalyzer;
        [SerializeField] private GameObject _ripplePrefab;

        [Header("Settings")]
        [SerializeField] private Color _blackThemeColor = Color.black;
        [SerializeField] private Color _whiteThemeColor = Color.white;
        [SerializeField] private int _poolSize = 10; // 미리 만들 개수

        private bool _isDarkBackground = true;

        // 🎱 오브젝트 풀 (큐 사용)
        private Queue<GameObject> _poolQueue = new Queue<GameObject>();
        private Transform _poolParent;

        private void Start()
        {
            if (_inputAnalyzer == null)
                _inputAnalyzer = FindFirstObjectByType<InputAnalyzer>();

            // 풀 초기화
            InitializePool();

            _inputAnalyzer.OnTap
                .Subscribe(pos => SpawnRipple(pos))
                .AddTo(this);
        }

        private void InitializePool()
        {
            if (_ripplePrefab == null) return;

            // 하이어라키 정리용 부모 오브젝트
            _poolParent = new GameObject("RipplePool").transform;
            _poolParent.SetParent(this.transform);

            for (int i = 0; i < _poolSize; i++)
            {
                CreateNewPoolItem();
            }
        }

        private GameObject CreateNewPoolItem()
        {
            GameObject obj = Instantiate(_ripplePrefab, _poolParent);
            obj.SetActive(false); // 일단 꺼둠
            _poolQueue.Enqueue(obj); // 큐에 넣음
            return obj;
        }

        public void SetTheme(bool isDark)
        {
            _isDarkBackground = isDark;
        }

        private void SpawnRipple(Vector2 screenPos)
        {
            if (_poolQueue.Count == 0)
            {
                // 풀이 모자라면 하나 더 생성 (유연한 확장)
                CreateNewPoolItem();
            }

            // 1. 풀에서 꺼내기 (Dequeue)
            GameObject ripple = _poolQueue.Dequeue();

            // 2. 위치 설정
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
            worldPos.z = 0f;
            ripple.transform.position = worldPos;
            ripple.SetActive(true); // 켜기

            // 3. 색상 설정
            SpriteRenderer sr = ripple.GetComponent<SpriteRenderer>();
            sr.color = _isDarkBackground ? _whiteThemeColor : _blackThemeColor;

            // 4. 애니메이션 실행 (재사용이므로 기존 Tween 있으면 죽여야 함)
            ripple.transform.DOKill();
            sr.DOKill();

            PlayAnimation(ripple, sr);
        }

        // (PlayAnimation 메서드만 교체하세요)
        private void PlayAnimation(GameObject target, SpriteRenderer sr)
        {
            float duration = 0.5f;

            // 초기화
            target.transform.localScale = Vector3.one * 0.2f;
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;

            Sequence seq = DOTween.Sequence();

            // 1. 커지면서 (Scale)
            seq.Append(target.transform.DOScale(Vector3.one * 1.5f, duration));

            // 2. 투명해짐 (Alpha) - ✅ 안전한 방식 (DOTween.To)
            // 람다식으로 sr.color의 알파값을 0으로 트위닝합니다.
            seq.Join(DOTween.To(() => sr.color.a, x =>
            {
                Color temp = sr.color;
                temp.a = x;
                sr.color = temp;
            }, 0f, duration).SetEase(Ease.InQuad));

            // 3. 끝나면 반납
            seq.OnComplete(() => ReturnToPool(target));
        }

        private void ReturnToPool(GameObject obj)
        {
            obj.SetActive(false); // 끄고
            _poolQueue.Enqueue(obj); // 다시 큐에 넣음 (반납)
        }
    }
}
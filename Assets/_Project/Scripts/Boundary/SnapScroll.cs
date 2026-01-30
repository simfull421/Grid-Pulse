using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ReflexPuzzle.Boundary
{
    public class SnapScroll : MonoBehaviour, IEndDragHandler
    {
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _content;
        [SerializeField] private RectTransform[] _items; // 모드 카드 4개

        private float[] _itemPositions;
        private int _currentIndex = 0;

        // 현재 선택된 모드 인덱스 반환 (GameMain이 가져감)
        public int CurrentIndex => _currentIndex;

        private void Start()
        {
            // 각 아이템의 중심 좌표 계산
            _itemPositions = new float[_items.Length];
            // ... (좌표 계산 로직은 LayoutGroup 때문에 조금 복잡할 수 있으니)
            // 간단하게: 0, 0.33, 0.66, 1.0 비율로 매핑
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // 스크롤 놓았을 때 가장 가까운 아이템으로 자석처럼 이동
            // (구현이 복잡해질 수 있으니, 일단은 좌우 화살표 버튼 방식을 추천하긴 합니다만,
            // 슬라이드를 원하신다면 ScrollRect의 normalizedPosition을 Lerp로 이동시킵니다.)
        }

        // [간단 버전 제안]
        // 슬라이드 제스처 감지기만 만들고, 실제 이동은 애니메이션으로 처리하는 게 훨씬 쉽습니다.
    }
}
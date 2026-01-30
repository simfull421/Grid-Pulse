using UnityEngine;

namespace ReflexPuzzle.Entity
{
    // 프로젝트 뷰에서 우클릭 > Create > ReflexPuzzle > New Theme 으로 생성 가능
    [CreateAssetMenu(fileName = "NewTheme", menuName = "ReflexPuzzle/New Theme")]
    public class ThemeData : ScriptableObject
    {
        [Header("Base Colors")]
        public Color BackgroundColor; // 카메라 배경색
        public Color TextColor;       // 숫자 색 (흰색 or 검은색)

        [Header("Tile Palette")]
        [Tooltip("0:Normal, 1:Trap, 2:Variation...")]
        public Color[] CellColors;    // 타일 색상 배열

        [Header("VFX")]
        public Material BloomMaterial; // 네온용 머티리얼 (선택사항)
    }
}
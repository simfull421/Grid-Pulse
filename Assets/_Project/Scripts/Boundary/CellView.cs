using UnityEngine;
using ReflexPuzzle.Entity;
using TMPro;

namespace ReflexPuzzle.Boundary
{
    public class CellView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private MeshRenderer _renderer;
        [SerializeField] private TextMeshPro _numberText;
        [SerializeField] private Transform _visualRoot;

        private static MaterialPropertyBlock _propBlock;
        // 범용성을 위해 _Color로 변경
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

        public int CurrentNumber { get; private set; }
        public int CurrentColorID { get; private set; }

        private void Awake()
        {
            if (_propBlock == null)
                _propBlock = new MaterialPropertyBlock();
        }

        public void Initialize(CellData data, ThemeData theme)
        {
            CurrentNumber = data.Number;
            CurrentColorID = data.ColorID;

            // 1. 텍스트 설정 및 알파값 강제 보정
            if (_numberText != null)
            {
                _numberText.gameObject.SetActive(true);

                // Z축 정렬 (앞으로 당기기)
                Vector3 txtPos = _numberText.transform.localPosition;
                txtPos.z = -0.05f;
                _numberText.transform.localPosition = txtPos;

                _numberText.text = data.IsHidden ? "" : data.Number.ToString();

                if (theme != null)
                {
                    Color safeColor = theme.TextColor;

                    // [안전장치] 만약 알파값이 0(투명)이면 강제로 1(불투명)로 변경
                    if (safeColor.a <= 0.01f)
                    {
                        safeColor.a = 1f;
                        // Debug.LogWarning("Theme TextColor Alpha was 0. Auto-corrected to 1.");
                    }
                    _numberText.color = safeColor;
                }
            }

            // 2. 타일 색상 결정
            Color targetColor = Color.white;

            if (theme != null)
            {
                if (data.IsTrap)
                {
                    targetColor = (theme.CellColors.Length > 1) ? theme.CellColors[1] : Color.red;
                }
                else if (data.ColorID < theme.CellColors.Length)
                {
                    targetColor = theme.CellColors[data.ColorID];
                }
            }

            // [안전장치] 타일 색상도 투명하면 곤란하니 보정 (선택사항)
            if (targetColor.a <= 0.01f) targetColor.a = 1f;

            // 3. 렌더러 적용
            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor(ColorPropertyId, targetColor);
                _renderer.SetPropertyBlock(_propBlock);
            }

            // 4. 등장 초기화
            if (_visualRoot != null) _visualRoot.localScale = Vector3.one;
        }

        public void AnimateSpawn(float delay) { }
    }
}
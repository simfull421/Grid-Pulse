using UnityEngine;
using TouchIT.Entity;

namespace TouchIT.Boundary
{
    public class HudManager : MonoBehaviour
    {
        [Header("Procedural UI")]
        [SerializeField] private ComboRingView _comboRingView; // [신규]

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer _hitZoneVisual;

        public void Initialize(float ringRadius)
        {
            // 콤보 링 초기화
            if (_comboRingView) _comboRingView.Initialize();
        }

        public void UpdateComboGauge(float fillAmount)
        {
            if (_comboRingView) _comboRingView.UpdateGauge(fillAmount);
        }

        public void SetThemeColors(ThemeColors.ThemeSet colors)
        {
            if (_hitZoneVisual) _hitZoneVisual.color = colors.Foreground;
            // 콤보 링 색상도 테마 따라갈지, 아니면 골드로 고정할지 결정
            // if (_comboRingView) _comboRingView.SetColor(colors.Foreground); 
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using TouchIT.Entity;

namespace TouchIT.Boundary
{
    public class HudManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image _comboGaugeImage;
        [SerializeField] private SpriteRenderer _hitZoneVisual;

        // [삭제됨] _timerLineRenderer 관련 코드 전부 제거

        public void Initialize(float ringRadius)
        {
            if (_comboGaugeImage) _comboGaugeImage.fillAmount = 0f;
            // 판정선 위치나 크기를 ringRadius에 맞출 필요가 있다면 여기서 조정
        }

        public void UpdateComboGauge(float fillAmount)
        {
            if (_comboGaugeImage) _comboGaugeImage.fillAmount = fillAmount;
        }

        public void SetThemeColors(ThemeColors.ThemeSet colors)
        {
            if (_hitZoneVisual) _hitZoneVisual.color = colors.Foreground;
            // 콤보 게이지 색상 변경이 필요하면 여기서 추가
        }

        // [삭제됨] DrawTimerRing 메서드 제거
    }
}
using UnityEngine;
using TouchIT.Entity;

namespace TouchIT.Control
{
    public interface IMainView
    {
        void Initialize();

        // 🔄 메인/스테이지 전환
        void AnimateMainToStage(Color firstAlbumColor);
        void AnimateStageToMain(Color mainThemeColor);

        void UpdateAlbumVisual(MusicData data);
        void AnimatePreviewMode(bool isPlaying);
        void AnimateGameStart();
        void AnimateGameEnd();


        void SetInteractiveScale(float delta); // 수동 크기 조절
   
        void ResetScale();                     // 취소하고 원복

        // 🔥 생명력 크기 조절 (추가됨)
        void SetLifeScale(float ratio);
        bool IsTransitioning { get; } // 🔒 현재 연출 중인가?

        // ✅ [추가됨] 연결 안 되어 있던 기능들 등록
        void OnNoteHitSuccess(float fuelRatio); // 타격감
        void AnimateOsuReady();                 // 발광 (준비)
        void AnimateEnterOsuMode();             // 진입 (하얀 화면)

        void AnimatePortalClosing(float duration, System.Action onClosed);
        // ✅ [추가됨] 링 끄고 켜기
        void ShowRing(bool show);
        void AnimatePortalClosingReady();
        void AnimateExitOsuMode();
    }
}
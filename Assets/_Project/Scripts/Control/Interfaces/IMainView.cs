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
        void CommitTransition(bool isZoomIn);  // 손 뗐을 때 결정 (진입 or 복귀)
        void ResetScale();                     // 취소하고 원복

        // 🔥 생명력 크기 조절 (추가됨)
        void SetLifeScale(float ratio);
    }
}
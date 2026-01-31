using UnityEngine;
using TouchIT.Entity;
using System.Threading;
using System.Collections.Generic;
namespace TouchIT.Control
{
    // 1. 화면/연출 제어 (Boundary가 구현)
    public interface IGameView
    {
        // 보스 관련
        void UpdateBossHp(float current, float max);
        void PlayBossHitAnimation();   // 피격 시 줌아웃/실루엣 흔들림
        void SetGroggyMode(bool isOn); // 그로기(문지르기) 모드 전환
        // [추가됨] 활성화된 노트 목록 가져오기
        IEnumerable<INoteView> GetActiveNotes();

        // [추가됨] 노트 반납
        void ReturnNote(INoteView note);

        // 연출 관련
        // [추가됨] 피격 이펙트
        void PlayHitEffect();

        // [추가됨] 라이프 감소
        void ReduceLife(int amount);
        // 노트 관련
        void SpawnNote(NoteData data);
        void ClearAllNotes();

        // 라이프(궤도) 관련 [아이디어 반영]
        void UpdateLifeVisual(int lifeCount); // 3->2->1 깨지는 연출

        // 카메라/이펙트
        void TriggerCameraKick(float intensity);
        void SetTheme(bool isDarkMode);
    }

    // 2. 오디오 제어 (Boundary가 구현)
    public interface IAudioManager
    {
        // 펜타토닉 화음 재생 (combo에 따라 화음 수 결정)
        void PlayNoteSound(int scaleIndex, int intensityLevel);
        void PlaySfx(string sfxName); // "Hit", "Fail", "Groggy" 등
        void SetMute(bool isMuted);
    }

    // 3. 입력 제어 (Control이 사용, Boundary가 구현 or Control 내 로직)
    public interface IInputProvider
    {
        // 이번엔 TryGet 방식을 기본으로 가져갑니다.
        bool TryGetTap(out Vector2 screenPos); // 탭 했나?
        bool TryGetShake(out float intensity); // 문지르기 강도 (그로기용)
    }
    // [신규] 노트의 구체적인 정보(NoteView)를 숨기기 위한 인터페이스
    public interface INoteView
    {
        float CurrentAngle { get; }
        int SoundIndex { get; }
        void UpdateRotation(float deltaTime);
        void Initialize(NoteData data, float ringRadius);
        void Deactivate();
    }
}
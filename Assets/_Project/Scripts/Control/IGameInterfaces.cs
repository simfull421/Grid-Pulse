using UnityEngine;
using System.Collections.Generic;
using TouchIT.Entity;

namespace TouchIT.Control
{
    public interface INoteView
    {
        float CurrentAngle { get; }
        NoteColor Color { get; }
        // [Fix] 누락되었던 Type 프로퍼티 추가
        NoteType Type { get; }
        Vector3 Position { get; }
        // [신규] 상태 구분: 절반(180도)을 넘어서 타격 가능한 상태인가?
        bool IsHittable { get; }
        void UpdateRotation(float deltaTime);
        void ReturnToPool();
    }
    // [신규] Control이 바라볼 센서 인터페이스
    public interface IHitSensor
    {
        INoteView GetBestHitNote(NoteColor currentMode);
        void RemoveNote(INoteView note);
    }
    public interface IGameView
    {// [신규] 홀드 노트 누르고 있을 때 이펙트 켜기/끄기
        void SetHoldEffect(bool isHolding);

        float RingRadius { get; }
        void SpawnNote(NoteData data);
        List<INoteView> GetActiveNotes();
        void ReturnNote(INoteView note);

        // 연출 및 상태 제어
        void SetTheme(NoteColor mode);
        void PlayHitEffect(Vector3 position, NoteColor color);
        void ReduceLife();

        // 그로기 관련
        void SetGroggyMode(bool isActive);
        void TriggerGroggyEffect();

        // [신규] 누락되었던 핵심 기능 추가
        void ClearAllNotes(bool isSuccess); // 성공 여부에 따라 이펙트 다르게 삭제

    

        void PlayGroggyBubbleEffect(Vector3 centerPos, NoteColor theme);


        void SetVisualTimer(float fillAmount, bool isActive); // 5초 타이머 링
        void UpdateComboGauge(float fillAmount); // 오버워치 궁 게이지

        void UpdateSpherePosition(Vector3 pos); // 그로기 때 구체 이동

    }

    public interface IAudioManager
    {
        // [중요] 컨트롤러나 부트스트래퍼가 호출하는 모든 메서드는 여기 있어야 합니다.
        void Initialize();

        void PlaySfx(string name);

        // [신규] 오류 해결: 배경음 테마 변경 메서드 추가
        void SetBgmTheme(NoteColor theme);
    }
}
using UnityEngine;
using System.Collections.Generic;
using TouchIT.Entity;

namespace TouchIT.Control
{
    // 노트 인터페이스 (기존 유지)
    public interface INoteView
    {
        float TailAngle { get; }
        float CurrentAngle { get; }
        NoteColor Color { get; }
        NoteType Type { get; }
        Vector3 Position { get; }
        bool IsHittable { get; }
        void UpdateRotation(float deltaTime);
        void ReturnToPool();
    }

    // 센서 인터페이스 (기존 유지)
    public interface IHitSensor
    {
        INoteView GetBestHitNote(NoteColor currentMode);
        void RemoveNote(INoteView note);
    }

    // [핵심] GameController가 바라보는 뷰 인터페이스
    public interface IGameView
    {
        // === 1. 기본 속성 ===
        float RingRadius { get; }

        // === 2. 노트 관리 (Note Manager) ===
        void SpawnNote(NoteData data);
        List<INoteView> GetActiveNotes();
        void ReturnNote(INoteView note);
        void ClearAllNotes(bool isSuccess);

        // === 3. 시각 효과 (VFX) ===
        void PlayHitEffect(Vector3 position, NoteType type);
        void SetHoldEffect(bool isHolding);

        // [Error Fix] StateIgnition에서 호출함 (그로기 진입 시 연출)
        void TriggerGroggyEffect();

        // === 4. 불꽃 생존 시스템 (Ember System) ===
        // [Error Fix] GameController에서 호출함
        bool IsEmberDead { get; }             // 게임 오버 체크용

  
        void StopEmberDrag();
        // [신규] 차원 이동 & Osu 모드 관련


   
        // 하이퍼 노트 (Osu)
        void SpawnHyperNote(Vector2 position);
    }

    public interface IAudioManager
    {
        void Initialize();
        void PlaySfx(string name, int comboCount = 0);

        // 테마가 하나로 통일되었으므로 BGM 테마 변경은 사실상 필요 없으나,
        // 기존 코드 호환성을 위해 남겨두거나 내부 구현을 비워두면 됩니다.
        void SetBgmTheme(NoteColor theme);
    }
}
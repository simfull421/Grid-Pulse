using UnityEngine;
using System.Collections.Generic;
using TouchIT.Entity;

namespace TouchIT.Control
{
    public interface INoteView
    {
        float CurrentAngle { get; }
        NoteColor Color { get; }
        Vector3 Position { get; }
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
    {
        float RingRadius { get; }
        void SpawnNote(NoteData data);
        IEnumerable<INoteView> GetActiveNotes();
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

        // 오디오
        void PlayBaseSound(bool isKick);
        void PlayHitSound();
    }

    public interface IAudioManager
    {
        void PlaySfx(string name);
    }
}
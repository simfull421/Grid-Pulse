using System;
using TouchIT.Entity;
using UnityEngine;

namespace TouchIT.Boundary
{
    public interface INoteView
    {
        // 데이터 프로퍼티
        NoteType Type { get; }
        double TargetTime { get; }
        float Duration { get; } // ✅ [추가] 홀드 노트 길이

        // 행동 메서드
        void Initialize(NoteInfo data, double dspTime, float approachRate, float ringRadius, Action<INoteView> onMiss);
        void OnUpdate(double currentDspTime);

        // 홀드 노트 전용 명령
        void OnHoldStart(); // ✅ [추가] 머리 숨기고 꼬리만 남겨라

        // 풀링 관련
        void Activate();
        void Deactivate();

        // 유니티 객체 접근용
        Transform Transform { get; }
        GameObject GameObject { get; }
    }
}
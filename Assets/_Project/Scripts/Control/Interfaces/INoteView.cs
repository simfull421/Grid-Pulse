using System;
using TouchIT.Entity;
using UnityEngine; // Vector3 등을 위해 필요할 수 있음

namespace TouchIT.Boundary
{
    public interface INoteView
    {
        // 데이터 프로퍼티
        NoteType Type { get; }
        double TargetTime { get; } // 판정 기준 시간

        // 행동 메서드
        void Initialize(NoteInfo data, double dspTime, float approachRate, float ringRadius, Action<INoteView> onMiss);
        void OnUpdate(double currentDspTime); // 시간 기반 이동

        // 풀링 관련
        void Activate();
        void Deactivate();

        // 유니티 객체 접근용 (필요 시)
        Transform Transform { get; }
        GameObject GameObject { get; }
    }
}
using UnityEngine;
using System;
using TouchIT.Boundary; // INoteView 참조

namespace TouchIT.Control
{
    public interface IOsuNoteFactory
    {
        // 생성과 동시에 초기화 데이터를 넘깁니다.
        INoteView CreateOsuNote(Vector3 position, double targetTime, float approachTime, Action<INoteView> onMiss);
        void ReturnOsuNote(INoteView note);
    }
}
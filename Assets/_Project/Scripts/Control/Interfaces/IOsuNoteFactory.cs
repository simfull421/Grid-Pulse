using System;
using TouchIT.Boundary;
using TouchIT.Entity;
using UnityEngine;

namespace TouchIT.Control // 혹은 Boundary
{
    public interface IOsuNoteFactory
    {
        void Initialize();

        // [수정] double targetTime -> NoteInfo data로 변경
        INoteView CreateOsuNote(Vector3 position, NoteInfo data, float approachTime, Action<INoteView> onMiss);

        void ReturnOsuNote(INoteView note);
    }
}
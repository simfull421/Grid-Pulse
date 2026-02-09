using System;
using TouchIT.Boundary;
using TouchIT.Entity;
using UnityEngine;

namespace TouchIT.Control
{
    public interface IOsuNoteFactory
    {
        void Initialize();

        // ✅ [핵심] NoteInfo 객체를 받도록 정의
        INoteView CreateOsuNote(Vector3 position, NoteInfo data, float approachTime, Action<INoteView> onMiss);

        void ReturnOsuNote(INoteView note);
    }
}
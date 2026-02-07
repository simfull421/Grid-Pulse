using UnityEngine;
using System.Collections.Generic;
using TouchIT.Control;
using TouchIT.Entity; // NoteInfo 사용을 위해 추가
using System;

namespace TouchIT.Boundary
{
    public class OsuNoteFactory : MonoBehaviour, IOsuNoteFactory
    {
        [SerializeField] private OsuNoteView _osuNotePrefab;
        [SerializeField] private int _poolSize = 20;

        private Queue<OsuNoteView> _pool = new Queue<OsuNoteView>();
        private Transform _container;

        public void Initialize()
        {
            _container = transform;
            for (int i = 0; i < _poolSize; i++)
            {
                CreateNewInstance();
            }
        }

        private OsuNoteView CreateNewInstance()
        {
            var note = Instantiate(_osuNotePrefab, _container);
            note.Deactivate();
            _pool.Enqueue(note);
            return note;
        }

        // [수정] 매개변수 타입을 NoteInfo로 변경
        public INoteView CreateOsuNote(Vector3 position, NoteInfo data, float approachTime, Action<INoteView> onMiss)
        {
            if (_pool.Count == 0) CreateNewInstance();

            OsuNoteView note = _pool.Dequeue();

            // 이제 타입이 맞으므로 에러가 사라집니다.
            note.InitializeOsu(position, data, approachTime, onMiss);

            return note;
        }

        public void ReturnOsuNote(INoteView note)
        {
            if (note is OsuNoteView osuNote)
            {
                osuNote.Deactivate();
                _pool.Enqueue(osuNote);
            }
        }
    }
}
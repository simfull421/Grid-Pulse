using UnityEngine;
using System.Collections.Generic;
using TouchIT.Control;
using System;

namespace TouchIT.Boundary
{
    public class OsuNoteFactory : MonoBehaviour, IOsuNoteFactory
    {
        [SerializeField] private OsuNoteView _osuNotePrefab;
        [SerializeField] private int _poolSize = 20;

        private Queue<OsuNoteView> _pool = new Queue<OsuNoteView>(); // 타입 명시
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

        // 인터페이스 구현: 여기서 구체적인 초기화(InitializeOsu)를 수행
        public INoteView CreateOsuNote(Vector3 position, double targetTime, float approachTime, Action<INoteView> onMiss)
        {
            if (_pool.Count == 0) CreateNewInstance();

            OsuNoteView note = _pool.Dequeue();

            // 팩토리가 뷰의 구체적인 메서드를 호출
            note.InitializeOsu(position, targetTime, approachTime, onMiss);

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
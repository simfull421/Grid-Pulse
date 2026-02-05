using UnityEngine;
using System.Collections.Generic;
using TouchIT.Control; // INoteFactory 인터페이스 참조

namespace TouchIT.Boundary
{
    // 실제 프리팹을 들고 있는 공장 (씬에 배치됨)
    public class NoteFactory : MonoBehaviour, INoteFactory
    {
        [SerializeField] private NoteView _notePrefab; // 여기서는 NoteView(구현체) 참조 가능
        [SerializeField] private int _initialPoolSize = 50;
        [SerializeField] private Transform _poolContainer;

        private Queue<INoteView> _pool = new Queue<INoteView>();

        public void Initialize()
        {
            if (_poolContainer == null) _poolContainer = transform;

            for (int i = 0; i < _initialPoolSize; i++)
            {
                CreateNewInstance();
            }
        }

        private INoteView CreateNewInstance()
        {
            NoteView instance = Instantiate(_notePrefab, _poolContainer);
            instance.Deactivate();
            _pool.Enqueue(instance);
            return instance;
        }

        // 인터페이스 구현: 노트 꺼내주기
        public INoteView CreateNote()
        {
            if (_pool.Count == 0) CreateNewInstance();

            INoteView note = _pool.Dequeue();
            note.Activate();
            return note;
        }

        // 인터페이스 구현: 노트 반납받기
        public void ReturnNote(INoteView note)
        {
            note.Deactivate();
            _pool.Enqueue(note);
        }
    }
}
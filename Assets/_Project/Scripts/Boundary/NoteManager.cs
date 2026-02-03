using UnityEngine;
using System.Collections.Generic;
using TouchIT.Entity;
using TouchIT.Control;

namespace TouchIT.Boundary
{
    public class NoteManager : MonoBehaviour
    {
        [Header("Note Settings")]
        [SerializeField] private NoteView _notePrefab;
        [SerializeField] private Transform _noteContainer;
        [SerializeField] private int _initialPoolSize = 30;

        // 상태 관리
        private List<INoteView> _activeNotes = new List<INoteView>();
        private Queue<NoteView> _notePool = new Queue<NoteView>();

        public void Initialize()
        {
            // 풀링 초기화
            for (int i = 0; i < _initialPoolSize; i++)
            {
                CreateNewNoteToPool();
            }
        }

        private NoteView CreateNewNoteToPool()
        {
            NoteView note = Instantiate(_notePrefab, _noteContainer);
            note.gameObject.SetActive(false);
            _notePool.Enqueue(note);
            return note;
        }

        public void Spawn(NoteData data, float radius, GameBinder binder)
        {
            NoteView note = (_notePool.Count > 0) ? _notePool.Dequeue() : CreateNewNoteToPool();

            note.gameObject.SetActive(true);
            // NoteView는 반환처로 Binder를 알고 있어야 함 (인터페이스 의존)
            note.Initialize(data, radius, binder);

            _activeNotes.Add(note);
        }

        public void ReturnNote(INoteView noteInterface)
        {
            NoteView note = noteInterface as NoteView;
            if (note != null && note.gameObject.activeSelf)
            {
                note.gameObject.SetActive(false);
                if (_activeNotes.Contains(note)) _activeNotes.Remove(note);
                _notePool.Enqueue(note);
            }
        }

        public List<INoteView> GetActiveNotes() => _activeNotes;

        public void ClearAll(bool success, System.Action<NoteView> onClearAction)
        {
            for (int i = _activeNotes.Count - 1; i >= 0; i--)
            {
                var note = _activeNotes[i] as NoteView;
                if (note != null)
                {
                    // 성공 시 이펙트 재생 등을 위해 콜백 호출
                    onClearAction?.Invoke(note);
                    ReturnNote(note);
                }
            }
        }
    }
}
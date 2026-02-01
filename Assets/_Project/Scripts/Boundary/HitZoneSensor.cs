using UnityEngine;
using System.Collections.Generic;
using TouchIT.Control;
using TouchIT.Entity;

namespace TouchIT.Boundary
{
    // [중요] Boundary 네임스페이스로 이동
    public class HitZoneSensor : MonoBehaviour, IHitSensor
    {
        private List<INoteView> _notesInZone = new List<INoteView>();

        private void OnTriggerEnter2D(Collider2D other)
        {
            // [디버깅] 충돌 자체가 일어나는지 확인
            Debug.Log($"💥 충돌 감지! 대상: {other.name}");

            INoteView note = other.GetComponent<INoteView>();
            if (note != null)
            {
                _notesInZone.Add(note);
                Debug.Log($"📥 리스트 추가됨! 현재 개수: {_notesInZone.Count}");
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            INoteView note = other.GetComponent<INoteView>();
            if (note != null && _notesInZone.Contains(note))
                _notesInZone.Remove(note);
        }

        // IHitSensor 구현
        public INoteView GetBestHitNote(NoteColor currentMode)
        {
            foreach (var note in _notesInZone)
            {
                if (note.Color == currentMode) return note;
            }
            return null;
        }

        public void RemoveNote(INoteView note)
        {
            if (_notesInZone.Contains(note)) _notesInZone.Remove(note);
        }
    }
}
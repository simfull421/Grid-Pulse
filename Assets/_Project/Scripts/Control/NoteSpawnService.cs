using System.Collections.Generic;
using TouchIT.Entity;
using TouchIT.Boundary;
using UnityEngine;

namespace TouchIT.Control
{
    public class NoteSpawnService : ISpawnService
    {
        private readonly INoteFactory _noteFactory;
        private readonly AudioManager _audioManager;

        private List<NoteInfo> _notePattern;
        private int _headIndex = 0;
        private double _preemptTime = 2.5f;
        private bool _isPlaying = false;
        private const float HIT_THRESHOLD = 0.2f;

        private List<INoteView> _activeNotes = new List<INoteView>();
        private List<INoteView> _holdingNotes = new List<INoteView>(); // 인터페이스 리스트

        public NoteSpawnService(INoteFactory factory, AudioManager audio)
        {
            _noteFactory = factory;
            _audioManager = audio;
        }

        public void LoadPattern(MusicData data)
        {
            if (data == null) return;
            data.Notes.Sort((a, b) => a.Time.CompareTo(b.Time));
            _notePattern = data.Notes;
            _headIndex = 0;
            _isPlaying = true;
            ClearActiveNotes();
            Debug.Log($"🎼 Service: Pattern Loaded ({data.Notes.Count} notes)");
        }

        public void Stop() { _isPlaying = false; ClearActiveNotes(); }

        public void OnUpdate()
        {
            if (!_isPlaying || _notePattern == null) return;

            double currentTime = _audioManager.GetAudioTime();

            // 1. 생성 로직
            while (_headIndex < _notePattern.Count)
            {
                NoteInfo nextNote = _notePattern[_headIndex];
                if (currentTime >= (nextNote.Time - _preemptTime))
                {
                    SpawnNote(nextNote, currentTime);
                    _headIndex++;
                }
                else break;
            }

            // 2. 이동 로직 & 홀드 종료 체크
            for (int i = _activeNotes.Count - 1; i >= 0; i--)
            {
                INoteView note = _activeNotes[i];
                note.OnUpdate(currentTime);

                // 🔥 [수정] 인터페이스 메서드 사용 (형변환 제거)
                if (_holdingNotes.Contains(note))
                {
                    // TargetTime + Duration = 종료 시간
                    if (currentTime >= note.TargetTime + note.Duration)
                    {
                        CompleteHoldNote(note);
                    }
                }
            }
        }

        private void SpawnNote(NoteInfo data, double currentTime)
        {
            INoteView note = _noteFactory.CreateNote();
            note.Initialize(data, currentTime, (float)_preemptTime, 3.0f, OnNoteMiss);
            _activeNotes.Add(note);
        }

        private void OnNoteMiss(INoteView note)
        {
            if (_activeNotes.Contains(note))
            {
                _activeNotes.Remove(note);
                if (_holdingNotes.Contains(note)) _holdingNotes.Remove(note);
                _noteFactory.ReturnNote(note);
            }
        }

        private void ClearActiveNotes()
        {
            foreach (var note in _activeNotes) _noteFactory.ReturnNote(note);
            _activeNotes.Clear();
            _holdingNotes.Clear();
        }

        public void Resume() { _isPlaying = true; }

        public INoteView CheckHitAndGetNote()
        {
            if (!_isPlaying) return null;

            double currentTime = _audioManager.GetAudioTime();
            INoteView target = null;
            double minDiff = double.MaxValue;

            for (int i = 0; i < _activeNotes.Count; i++)
            {
                var note = _activeNotes[i];
                if (_holdingNotes.Contains(note)) continue;

                double diff = System.Math.Abs(note.TargetTime - currentTime);
                if (diff <= HIT_THRESHOLD && diff < minDiff)
                {
                    minDiff = diff;
                    target = note;
                }
            }

            if (target != null)
            {
                OnNoteHit(target);
                return target;
            }
            return null;
        }

        private void OnNoteHit(INoteView note)
        {
            if (note.Type == NoteType.Hold)
            {
                Debug.Log("✨ Hold Start!");
                // ✅ [수정] 인터페이스 메서드 호출
                note.OnHoldStart();
                _holdingNotes.Add(note);
            }
            else
            {
                Debug.Log("✨ Tap HIT!");
                RemoveNote(note);
            }
        }

        private void CompleteHoldNote(INoteView note)
        {
            Debug.Log("✨ Hold Complete!");
            RemoveNote(note);
        }

        private void RemoveNote(INoteView note)
        {
            if (_activeNotes.Contains(note))
            {
                _activeNotes.Remove(note);
                if (_holdingNotes.Contains(note)) _holdingNotes.Remove(note);
                _noteFactory.ReturnNote(note);
            }
        }
    }
}
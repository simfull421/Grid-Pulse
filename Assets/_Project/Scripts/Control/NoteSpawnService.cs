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

        private List<NoteInfo> _notePattern; // 전체 패턴

        // [최적화 핵심] Head Index & Preempt Time
        private int _headIndex = 0;
        private double _preemptTime = 2.5f; // 노트가 날아가는 시간 (Approach Rate)

        private bool _isPlaying = false;

        // 🔥 [중요] 현재 화면에 나와있는 노트들 (매 프레임 움직여줘야 함)
        private List<INoteView> _activeNotes = new List<INoteView>();

        public NoteSpawnService(INoteFactory factory, AudioManager audio)
        {
            _noteFactory = factory;
            _audioManager = audio;
        }

        public void LoadPattern(MusicData data)
        {
            if (data == null) return;

            // 시간순 정렬 (필수)
            data.Notes.Sort((a, b) => a.Time.CompareTo(b.Time));

            _notePattern = data.Notes;
            _headIndex = 0;
            _isPlaying = true;

            // 기존 노트 정리
            ClearActiveNotes();

            Debug.Log($"🎼 Service: Pattern Loaded ({data.Notes.Count} notes)");
        }

        public void Stop()
        {
            _isPlaying = false;
            ClearActiveNotes();
        }

        // 매 프레임 호출 (GameBootstrapper가 호출함)
        public void OnUpdate()
        {
            if (!_isPlaying || _notePattern == null) return;

            double currentTime = _audioManager.GetAudioTime();

            // 1. [생성 로직] Head Index 최적화 적용
            // (사용자님 코드: if (currentAudioTime >= (nextNote.Time - _preemptTime)))
            while (_headIndex < _notePattern.Count)
            {
                NoteInfo nextNote = _notePattern[_headIndex];

                // 지금 시간이 (노트 타임 - 날아오는 시간)보다 컸는가? -> 생성!
                if (currentTime >= (nextNote.Time - _preemptTime))
                {
                    SpawnNote(nextNote, currentTime);
                    _headIndex++; // 다음 노트 검사
                }
                else
                {
                    // 아직 시간 안 됐으면 뒤에껀 볼 필요도 없음 (Break)
                    break;
                }
            }

            // 2. [이동 로직] 🔥 여기가 빠져서 노트가 가만히 있던 겁니다!
            // 화면에 나와있는 모든 노트에게 "현재 시간"을 알려줘서 위치를 갱신시킴
            for (int i = _activeNotes.Count - 1; i >= 0; i--)
            {
                INoteView note = _activeNotes[i];

                // 노트야, 지금 시간이 이거니까 알아서 위치 옮겨라
                note.OnUpdate(currentTime);
            }
        }

        private void SpawnNote(NoteInfo data, double currentTime)
        {
            INoteView note = _noteFactory.CreateNote();

            // 초기화 (데이터, 생성시간, 날아가는 시간, 링 반지름, 미스 콜백)
            note.Initialize(data, currentTime, (float)_preemptTime, 3.0f, OnNoteMiss);

            // 관리 리스트에 추가 (그래야 OnUpdate를 돌릴 수 있음)
            _activeNotes.Add(note);
        }

        private void OnNoteMiss(INoteView note)
        {
            // 리스트에서 제거하고 반납
            if (_activeNotes.Contains(note))
            {
                _activeNotes.Remove(note);
                _noteFactory.ReturnNote(note);
            }
        }

        private void ClearActiveNotes()
        {
            foreach (var note in _activeNotes)
            {
                _noteFactory.ReturnNote(note);
            }
            _activeNotes.Clear();
        }

        public void Resume() { _isPlaying = true; }

        // 판정 로직 (탭 했을 때)
        // 📡 외부(GameController)에서 탭 입력 시 호출
        // 기존 CheckHit 제거하고 이것으로 대체
        public INoteView CheckHitAndGetNote()
        {
            if (!_isPlaying) return null;

            double currentTime = _audioManager.GetAudioTime();
            INoteView target = null;
            double minDiff = double.MaxValue;

            for (int i = 0; i < _activeNotes.Count; i++)
            {
                var note = _activeNotes[i];
                double diff = System.Math.Abs(note.TargetTime - currentTime);

                if (diff < 0.15f && diff < minDiff)
                {
                    minDiff = diff;
                    target = note;
                }
            }

            if (target != null)
            {
                OnNoteHit(target);
                return target; // ✅ 맞춘 노트 반환
            }
            return null; // 실패
        }
        private void OnNoteHit(INoteView note)
        {
            // 성공 이펙트 (여기선 로그만)
            Debug.Log("✨ HIT!");

            // 리스트 제거 및 반납
            if (_activeNotes.Contains(note))
            {
                _activeNotes.Remove(note);
                _noteFactory.ReturnNote(note);
            }
        }
    }
}
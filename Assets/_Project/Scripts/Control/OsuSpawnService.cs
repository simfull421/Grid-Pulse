using UnityEngine;
using TouchIT.Boundary;
using TouchIT.Entity;
using System.Collections.Generic;
using System;

namespace TouchIT.Control
{
    public class OsuSpawnService : ISpawnService
    {
        private readonly IOsuNoteFactory _factory;
        private readonly AudioManager _audioManager;
        private readonly Func<Vector3> _playerPosProvider;

        private List<NoteInfo> _currentPattern;
        private List<IOsuNoteView> _activeNotes = new List<IOsuNoteView>();

        private int _spawnIndex = 0;
        private bool _isPlaying = false;
        private double _preemptTime = 1.0f; // 어프로치 시간

        // 📏 충돌 설정
        // 플레이어 반지름(0.5) + 노트 반지름(0.8) = 1.3 정도지만 약간 여유를 둠
        private const float COLLISION_RADIUS_SUM = 1.2f;
        private const float HIT_WINDOW = 0.2f; // 판정 시간 여유 (초)

        // 위치 겹침 방지용
        private Vector3 _lastSpawnPos = Vector3.zero;

        public OsuSpawnService(IOsuNoteFactory factory, AudioManager audio, Func<Vector3> playerPosProvider)
        {
            _factory = factory;
            _audioManager = audio;
            _playerPosProvider = playerPosProvider;
        }

        public void LoadPattern(MusicData data)
        {
            if (data == null) return;
            _currentPattern = data.Notes;

            // 시간순 정렬 보장
            _currentPattern.Sort((a, b) => a.Time.CompareTo(b.Time));

            double currentTime = _audioManager.GetAudioTime();
            // 이미 지난 노트 스킵
            _spawnIndex = 0;
            while (_spawnIndex < _currentPattern.Count && _currentPattern[_spawnIndex].Time < currentTime)
            {
                _spawnIndex++;
            }

            ClearActiveNotes();
            _isPlaying = true;
            Debug.Log($"⚔️ Osu Service Started! Index: {_spawnIndex}");
        }

        public void OnUpdate()
        {
            if (!_isPlaying || _currentPattern == null) return;

            double currentTime = _audioManager.GetAudioTime();

            // 1. 스폰 로직
            while (_spawnIndex < _currentPattern.Count)
            {
                NoteInfo nextNote = _currentPattern[_spawnIndex];

                if (currentTime >= (nextNote.Time - _preemptTime))
                {
                    SpawnOsuNote(nextNote, currentTime);
                    _spawnIndex++;
                }
                else break;
            }

            // 2. 노트 업데이트 (미스 체크 등)
            for (int i = _activeNotes.Count - 1; i >= 0; i--)
            {
                _activeNotes[i].OnUpdate(currentTime);
            }
        }

        private void SpawnOsuNote(NoteInfo noteData, double currentTime)
        {
            // 🔄 [변환 로직] Hold 노트이거나 특정 조건일 때 Hard(3타) 노트로 변경
            // 여기서 원본 데이터를 바꾸지 않고 복사본을 쓰거나, 런타임에만 타입을 Hard로 취급
            if (noteData.Type == NoteType.Hold)
            {
                noteData.Type = NoteType.Hard; // 3HP
            }
            else
            {
                noteData.Type = NoteType.Tap; // 1HP
            }

            Vector3 spawnPos = CalculateNonOverlappingPosition(noteData.LaneIndex);

            var noteView = _factory.CreateOsuNote(
                spawnPos,
                noteData,
                (float)_preemptTime,
                OnNoteMiss
            ) as IOsuNoteView;

            if (noteView != null) _activeNotes.Add(noteView);
        }

        // 🎲 겹침 방지 위치 계산
        private Vector3 CalculateNonOverlappingPosition(int seed)
        {
            UnityEngine.Random.State oldState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(seed * 777); // 시드 고정

            Vector3 candidate = Vector3.zero;
            int attempts = 0;

            do
            {
                float x = UnityEngine.Random.Range(-2.2f, 2.2f);
                float y = UnityEngine.Random.Range(-3.5f, 3.5f);
                candidate = new Vector3(x, y, 0);
                attempts++;
            }
            while (Vector3.Distance(candidate, _lastSpawnPos) < 1.5f && attempts < 10);

            _lastSpawnPos = candidate;
            UnityEngine.Random.state = oldState;
            return candidate;
        }

        // 💥 [핵심] 충돌 감지 및 처리
        public INoteView CheckHitAndGetNote()
        {
            if (!_isPlaying || _activeNotes.Count == 0) return null;

            // 플레이어 위치 가져오기 (디버깅 필요시 아래 주석 해제)
            Vector3 playerPos = _playerPosProvider.Invoke();
            // Debug.Log($"Player Pos: {playerPos}"); 

            double currentTime = _audioManager.GetAudioTime();

            // 모든 활성 노트를 순회하며 충돌 검사
            for (int i = 0; i < _activeNotes.Count; i++)
            {
                IOsuNoteView note = _activeNotes[i];

                // 1. 시간 판정 (너무 빨리 혹은 너무 늦게 치는 것 방지)
                double timeDiff = Math.Abs(currentTime - note.TargetTime);

                // Hard 노트는 비비기 때문에 판정 시간을 좀 더 후하게 줍니다.
                float currentHitWindow = note.IsHardNote ? HIT_WINDOW * 2.5f : HIT_WINDOW;

                if (timeDiff <= currentHitWindow)
                {
                    // 2. 거리(충돌) 판정
                    // 노트 반지름(Radius) + 플레이어 반지름(0.5f) 정도를 고려
                    float dist = Vector3.Distance(playerPos, note.Position);
                    float collisionThreshold = note.Radius + 0.5f; // 판정 범위 (여유값 포함)

                    if (dist <= collisionThreshold)
                    {
                        // ⭐ [로그 추가] 충돌 감지됨!
                        Debug.Log($"💥 Collision Detected! NoteHP: {note.CurrentHP}");

                        // 3. 데미지 적용 (TakeDamage 내부에서 쿨타임 체크 함)
                        bool isDestroyed = note.TakeDamage(); // 노트 내부에서 시각적 반응(흔들림) 처리

                        if (isDestroyed)
                        {
                            Debug.Log("✨ Note DESTROYED!"); // 파괴 로그
                            _activeNotes.RemoveAt(i);
                            _factory.ReturnOsuNote(note);
                            return note; // 완전히 파괴됨 -> 점수 획득
                        }
                        else
                        {
                            // 파괴되진 않았지만 충돌 중임 (3타 노트 비비기 중)
                            // 시각적 피드백은 이미 TakeDamage() 안에서 처리됨
                            return null;
                        }
                    }
                }
            }
            return null;
        }

        private void OnNoteMiss(INoteView note)
        {
            if (note is IOsuNoteView osuNote && _activeNotes.Contains(osuNote))
            {
                _activeNotes.Remove(osuNote);
                _factory.ReturnOsuNote(osuNote);
            }
        }

        public void Stop() { _isPlaying = false; ClearActiveNotes(); }
        public void Resume() { _isPlaying = true; }

        private void ClearActiveNotes()
        {
            foreach (var n in _activeNotes) _factory.ReturnOsuNote(n);
            _activeNotes.Clear();
        }
    }
}
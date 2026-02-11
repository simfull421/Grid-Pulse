using UnityEngine;
using TouchIT.Boundary;
using TouchIT.Entity;
using System.Collections.Generic;
using System;
using DG.Tweening; // 이동 연출을 위해 필요

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

        // 등장 시간 (화면 밖에서 중앙까지 오는 시간) - 빠를수록 어려움
        private double _flightTime = 1.0f;
        // 🏃‍♂️ 속도 판정용 변수
        private Vector3 _lastPlayerPos;
        private const float MIN_SLASH_SPEED = 0.1f; // 베기 최소 속도 (조절 필요)
        // 충돌 범위 (날아오는 거라 판정이 좀 더 넉넉해야 함)
        private const float HIT_DISTANCE = 1.0f;

        public OsuSpawnService(IOsuNoteFactory factory, AudioManager audio, Func<Vector3> playerPosProvider)
        {
            _factory = factory;
            _audioManager = audio;
            _playerPosProvider = playerPosProvider;
        }

        public void LoadPattern(MusicData data)
        {
            if (data == null) return;
            _currentPattern = new List<NoteInfo>(data.Notes);
            _currentPattern.Sort((a, b) => a.Time.CompareTo(b.Time));

            double currentTime = _audioManager.GetAudioTime();
            _spawnIndex = 0;
            // 이미 지난 노트 스킵
            while (_spawnIndex < _currentPattern.Count && _currentPattern[_spawnIndex].Time < currentTime)
            {
                _spawnIndex++;
            }

            ClearActiveNotes();
            _isPlaying = true;
        }

        public void OnUpdate()
        {
            if (!_isPlaying || _currentPattern == null) return;

            double currentTime = _audioManager.GetAudioTime();

            // 1. 스폰 로직 (날아오는 노트)
            while (_spawnIndex < _currentPattern.Count)
            {
                NoteInfo nextNote = _currentPattern[_spawnIndex];

                // 노트 타격 시간보다 _flightTime만큼 미리 생성해야 함
                if (currentTime >= (nextNote.Time - _flightTime))
                {
                    SpawnFlyingNote(nextNote);
                    _spawnIndex++;
                }
                else break;
            }

            // 2. 업데이트
            for (int i = _activeNotes.Count - 1; i >= 0; i--)
            {
                _activeNotes[i].OnUpdate(currentTime);
            }
        }

        private void SpawnFlyingNote(NoteInfo noteData)
        {
            // 1. 시작 위치 결정 (화면 밖 4방향 중 랜덤)
            Vector3 startPos = GetRandomSpawnPosition();

            // 2. 목표 위치 (화면 중앙) - 구체로 쳐내야 하는 곳
            Vector3 targetPos = Vector3.zero;

            // 뷰 생성 (인터페이스로 받음)
            var noteView = _factory.CreateOsuNote(startPos, noteData, (float)_flightTime, OnNoteMiss) as IOsuNoteView;

            if (noteView != null)
            {
                _activeNotes.Add(noteView);

                // 🚀 [이동 로직] 노트 뷰에게 이동 명령 (DOTween 사용)
                // OsuNoteView.Transform은 Transform을 반환하므로 바로 사용 가능
                noteView.Transform.DOMove(targetPos, (float)_flightTime).SetEase(Ease.Linear);
            }
        }

        private Vector3 GetRandomSpawnPosition()
        {
            // 화면 밖 좌표 (가로 3.5, 세로 6.0 정도면 화면 밖이라고 가정)
            float xRange = 3.5f;
            float yRange = 6.0f;

            int side = UnityEngine.Random.Range(0, 4); // 0:상, 1:하, 2:좌, 3:우

            switch (side)
            {
                case 0: return new Vector3(UnityEngine.Random.Range(-xRange, xRange), yRange, 0); // 위
                case 1: return new Vector3(UnityEngine.Random.Range(-xRange, xRange), -yRange, 0); // 아래
                case 2: return new Vector3(-xRange, UnityEngine.Random.Range(-yRange, yRange), 0); // 왼쪽
                case 3: return new Vector3(xRange, UnityEngine.Random.Range(-yRange, yRange), 0); // 오른쪽
                default: return Vector3.up * 5f;
            }
        }

        // 💥 충돌 판정 (순서 상관 없음, 가까운 놈 때림)
        public INoteView CheckHitAndGetNote()
        {
            if (!_isPlaying || _activeNotes.Count == 0) return null;

            Vector3 playerPos = _playerPosProvider.Invoke();

            // 1. 구체 속도 계산 (이번 프레임 이동 거리 / 시간)
            // 모바일 터치 튀는 현상 방지를 위해 DeltaTime이 너무 작으면 0 처리
            float speed = (Time.deltaTime > 0) ? Vector3.Distance(playerPos, _lastPlayerPos) / Time.deltaTime : 0f;

            _lastPlayerPos = playerPos; // 다음 프레임을 위해 저장 (항상 최신화)

            // 순회하면서 충돌 검사
            for (int i = 0; i < _activeNotes.Count; i++)
            {
                IOsuNoteView note = _activeNotes[i];
                float dist = Vector3.Distance(playerPos, note.Position);

                if (dist <= HIT_DISTANCE)
                {
                    // 🔥 [속도 체크] 
                    // "네온 닌자"니까 휙! 하고 그을 때만 베어지도록 설정 (옵션)
                    // 너무 가만히 대고 있으면(속도 0) 안 베어짐 -> 이게 "액션감"의 핵심
                    if (speed < MIN_SLASH_SPEED)
                    {
                        // 충돌은 했지만 속도가 느려서 안 베어짐 (팅~ 소리 재생 가능)
                        continue;
                    }

                    // 조건 만족 시 파괴
                    note.TakeDamage();
                    note.Transform.DOKill(); // 이동 멈춤

                    _activeNotes.RemoveAt(i);
                    _factory.ReturnOsuNote(note);

                    return note;
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
        private void ClearActiveNotes() { foreach (var n in _activeNotes) _factory.ReturnOsuNote(n); _activeNotes.Clear(); }
    }
}
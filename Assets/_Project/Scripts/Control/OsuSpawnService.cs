using UnityEngine;
using TouchIT.Boundary;
using TouchIT.Entity;
using System.Collections.Generic;

namespace TouchIT.Control
{
    public class OsuSpawnService : ISpawnService
    {
        private readonly IOsuNoteFactory _factory;
        private readonly AudioManager _audioManager;

        private List<NoteInfo> _currentPattern; // 전체 패턴 데이터
        private List<INoteView> _activeNotes = new List<INoteView>();

        private int _spawnIndex = 0;
        private bool _isPlaying = false;

        // 오수 노트는 링 노트보다 조금 더 일찍 보여주는 게 좋음
        private double _preemptTime = 1.0f;

        public OsuSpawnService(IOsuNoteFactory factory, AudioManager audio)
        {
            _factory = factory;
            _audioManager = audio;
        }

        public void LoadPattern(MusicData data)
        {
            if (data == null || data.Notes.Count == 0) return;

            // 1. 데이터 로드
            _currentPattern = data.Notes;

            // 2. [핵심] 현재 노래 시간에 맞춰 스폰 인덱스 '빨리 감기' (Fast Forward)
            double currentTime = _audioManager.GetAudioTime();

            // 현재 시간보다 뒤에 있는(등장해야 할) 노트부터 시작
            // (이미 지나간 노트는 무시)
            _spawnIndex = 0;
            while (_spawnIndex < _currentPattern.Count &&
                  _currentPattern[_spawnIndex].Time < currentTime)
            {
                _spawnIndex++;
            }

            _activeNotes.Clear();
            _isPlaying = true;

            Debug.Log($"⚔️ Osu Service Started! Skipping to note index: {_spawnIndex} (Time: {currentTime:F2})");
        }

        public void OnUpdate()
        {
            if (!_isPlaying || _currentPattern == null) return;

            double currentTime = _audioManager.GetAudioTime();

            // 1. [스폰 로직] 베이커가 구운 데이터를 읽어서 생성
            while (_spawnIndex < _currentPattern.Count)
            {
                NoteInfo nextNote = _currentPattern[_spawnIndex];

                // 생성 시간 도달 체크
                if (currentTime >= (nextNote.Time - _preemptTime))
                {
                    SpawnOsuNote(nextNote, currentTime);
                    _spawnIndex++;
                }
                else
                {
                    break; // 아직 시간 안 됨
                }
            }

            // 2. [이동 로직] 활성 노트 업데이트 (시간 초과 체크 등)
            for (int i = _activeNotes.Count - 1; i >= 0; i--)
            {
                _activeNotes[i].OnUpdate(currentTime);
            }
        }

        private void SpawnOsuNote(NoteInfo noteData, double currentTime)
        {
            // 🗺️ [좌표 변환] (기존 로직 유지)
            Vector3 spawnPos = CalculateDeterministicPosition(noteData.LaneIndex);

            // 팩토리를 통해 생성 (초기화 데이터 전달)
            // 🚨 [수정] noteData.Time -> noteData (객체 통째로 전달!)
            var noteView = _factory.CreateOsuNote(
                spawnPos,
                noteData, // ✅ 여기를 수정했습니다! (double -> NoteInfo)
                (float)_preemptTime,
                OnNoteMiss
            );

            _activeNotes.Add(noteView);
        }

        // 🧮 0~31의 LaneIndex를 화면 내 불규칙한 좌표로 변환하는 함수
        private Vector3 CalculateDeterministicPosition(int seed)
        {
            // 임시 난수 생성기 사용 (Seed 고정)
            Random.State oldState = Random.state;
            Random.InitState(seed * 12345); // 시드값 변형

            // 화면 범위 (대략 -2.5 ~ 2.5)
            float x = Random.Range(-2.2f, 2.2f);
            float y = Random.Range(-3.5f, 3.5f); // 위아래로 좀 더 길게

            Random.state = oldState; // 원래 랜덤 상태 복구

            return new Vector3(x, y, 0);
        }

        private void OnNoteMiss(INoteView note)
        {
            if (_activeNotes.Contains(note))
            {
                _activeNotes.Remove(note);
                _factory.ReturnOsuNote(note);
                // Debug.Log("💔 Osu Miss!"); // 로그 너무 많으면 끔
            }
        }

        public INoteView CheckHitAndGetNote()
        {
            // 가장 오래된 노트(0번)를 가져와서 판정
            // (정교하게 하려면 터치 좌표와 거리 계산을 해야 하지만 일단 이걸로 충분)
            if (_activeNotes.Count > 0)
            {
                var target = _activeNotes[0];

                // 리스트에서 제거 및 반납 (성공 처리)
                _activeNotes.RemoveAt(0);
                _factory.ReturnOsuNote(target);

                return target;
            }
            return null;
        }

        public void Stop()
        {
            _isPlaying = false;
            foreach (var n in _activeNotes) _factory.ReturnOsuNote(n);
            _activeNotes.Clear();
        }
        public void Resume() { }
    }
}
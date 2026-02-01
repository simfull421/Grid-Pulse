using UnityEngine;
using System.Collections.Generic;
using TouchIT.Entity;

namespace TouchIT.Control
{
    public class RhythmEngine
    {
        private float _bpm = 110f;

        // [타이밍 변수]
        // 페이즈 타이머, 비트 타이머 삭제됨 (GameMain 위임 & BGM Loop 사용)
        private double _nextSpawnTime;

        private IGameView _view;
        private BeatLibrary _beatLib;

        private int _noteIdCounter;

        // [핵심] 엔진은 스스로 페이즈를 바꾸지 않고, GameMain이 설정해준 색상을 따릅니다.
        private NoteColor _targetColor = NoteColor.White;

        // 과부하 방지
        private const float MIN_SPAWN_INTERVAL = 0.2f;
        private double _lastSpawnRealTime;

        // 패턴 큐 (시간 간격 & 소리 인덱스)
        private Queue<float> _intervalQueue = new Queue<float>();
        private Queue<int> _soundQueue = new Queue<int>(); // [신규] 펜타토닉 음계 인덱스 큐

        public void Initialize(IGameView view, BeatLibrary lib)
        {
            _view = view;
            _beatLib = lib;

            // 시작 시간 설정
            double startTime = AudioSettings.dspTime + 1.0;
            _nextSpawnTime = startTime;

            // 초기화
            _targetColor = NoteColor.White;
            _intervalQueue.Clear();
            _soundQueue.Clear();

            LoadNewPattern();
        }

        // [신규] GameMain에서 테마가 바뀔 때 호출해주는 함수
        public void SetCurrentPhase(NoteColor color)
        {
            _targetColor = color;
        }

        public void OnUpdate()
        {
            double currentTime = AudioSettings.dspTime;

            // 페이즈 전환 로직 삭제됨 (GameMain이 함)
            // 배경 비트 재생 로직 삭제됨 (AudioSource Loop 사용)

            // 노트 생성 (패턴 데이터 따름)
            if (currentTime >= _nextSpawnTime)
            {
                SpawnLogic();
                CalculateNextSpawn();
            }
        }

        private void CalculateNextSpawn()
        {
            if (_intervalQueue.Count == 0) LoadNewPattern();

            float gap = (_intervalQueue.Count > 0) ? _intervalQueue.Dequeue() : 1.0f;
            double gapTime = (60.0 / _bpm) * gap;
            _nextSpawnTime += gapTime;
        }

        private void LoadNewPattern()
        {
            if (_beatLib == null)
            {
                _intervalQueue.Enqueue(1f);
                _soundQueue.Enqueue(0);
                return;
            }

            BeatPattern pattern = _beatLib.GetRandomPattern();
            if (pattern != null)
            {
                // 시간 간격 큐 채우기
                foreach (float interval in pattern.Intervals)
                    _intervalQueue.Enqueue(interval);

                // [신규] 소리(음계) 인덱스 큐 채우기 (0, 1, 2, 3...)
                // BeatGenerator에서 저장한 멜로디 패턴을 가져옴
                if (pattern.SoundIndices != null && pattern.SoundIndices.Length > 0)
                {
                    foreach (int soundIdx in pattern.SoundIndices)
                        _soundQueue.Enqueue(soundIdx);
                }
                else
                {
                    // 데이터 없으면 기본음(0)으로 채움
                    for (int i = 0; i < pattern.Intervals.Length; i++)
                        _soundQueue.Enqueue(0);
                }
            }
        }

        private void SpawnLogic()
        {
            // 과부하 방지 (너무 빠른 생성 스킵)
            if (AudioSettings.dspTime < _lastSpawnRealTime + MIN_SPAWN_INTERVAL)
            {
                // 싱크 유지를 위해 큐는 비워줌
                if (_soundQueue.Count > 0) _soundQueue.Dequeue();
                return;
            }

            // 20% 확률로 쉼표 (노트 안 나옴)
            if (Random.value < 0.2f)
            {
                if (_soundQueue.Count > 0) _soundQueue.Dequeue();
                return;
            }

            _lastSpawnRealTime = AudioSettings.dspTime;

            // [난이도] 현재 설정된 테마(_targetColor)에 따라 속도 조절
            float speed = 60f; // White (기본)

            if (_targetColor == NoteColor.Black)
                speed = 90f; // Black (빠름)
            else if (_targetColor == NoteColor.Cosmic)
                speed = 80f; // Cosmic (약간 빠르고 몽환적)

            NoteType type = (Random.value < 0.1f) ? NoteType.Hold : NoteType.Normal;
            float duration = (type == NoteType.Hold) ? 0.5f : 0f;

            // 큐에서 펜타토닉 음계 인덱스 꺼내기
            int soundIdx = (_soundQueue.Count > 0) ? _soundQueue.Dequeue() : 0;

            NoteData data = new NoteData(
                _noteIdCounter++,
                (float)AudioSettings.dspTime,
                450f,
                speed,      // 결정된 속도
                soundIdx,   // [중요] 0~3번 인덱스 전달 (GameBinder가 이걸로 악기 연주)
                _targetColor, // GameMain이 시킨 색상
                type,
                duration
            );

            _view.SpawnNote(data);
        }
    }
}
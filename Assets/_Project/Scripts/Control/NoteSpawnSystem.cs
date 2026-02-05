using System.Collections.Generic;
using TouchIT.Entity;
using TouchIT.Boundary; // INoteView 인터페이스 참조
using UnityEngine; // Debug.Log 정도는 허용되지만, 로직용으로만 사용

namespace TouchIT.Control
{
    // MonoBehaviour 제거 -> 순수 C# 클래스
    public class NoteSpawnService
    {
        private readonly INoteFactory _noteFactory; // 팩토리 인터페이스 참조
        private readonly AudioManager _audioManager;

        private List<NoteInfo> _currentPattern;
        private int _spawnIndex = 0;
        private bool _isPlaying = false;

        // 생성자 주입 (Constructor Injection)
        public NoteSpawnService(INoteFactory factory, AudioManager audio)
        {
            _noteFactory = factory;
            _audioManager = audio;
        }

        public void LoadPattern(MusicData data)
        {
            if (data == null) return;
            data.Notes.Sort((a, b) => a.Time.CompareTo(b.Time));
            _currentPattern = data.Notes;
            _spawnIndex = 0;
            _isPlaying = true;
            Debug.Log($"🎼 Service: Pattern Loaded ({data.Notes.Count} notes)");
        }

        public void Stop()
        {
            _isPlaying = false;
        }

        // MonoBehaviour가 아니므로 외부에서 이 함수를 매 프레임 호출해줘야 함
        public void OnUpdate()
        {
            if (!_isPlaying || _currentPattern == null) return;

            double currentTime = _audioManager.GetAudioTime();
            float approachTime = 1.5f; // 난이도 설정값 (나중에 주입 가능)

            // 스폰 로직
            while (_spawnIndex < _currentPattern.Count)
            {
                NoteInfo nextNote = _currentPattern[_spawnIndex];
                if (currentTime + approachTime >= nextNote.Time)
                {
                    SpawnNote(nextNote, currentTime, approachTime);
                    _spawnIndex++;
                }
                else
                {
                    break;
                }
            }
        }

        private void SpawnNote(NoteInfo data, double currentTime, float approachTime)
        {
            // 팩토리에게 "노트 하나 줘" (실제 구현은 Boundary가 함)
            INoteView note = _noteFactory.CreateNote();

            // 초기화
            note.Initialize(data, currentTime, approachTime, 3.0f, OnNoteMiss);
        }

        private void OnNoteMiss(INoteView note)
        {
            Debug.Log("💔 Miss!");
            _noteFactory.ReturnNote(note); // 팩토리로 반납
        }
        // NoteSpawnService 클래스 내부
        public void Resume()
        {
            _isPlaying = true;
            // 오디오 시간과 싱크를 맞추기 위한 추가 로직이 필요할 수 있으나,
            // 일단은 플래그만 켜도 다시 Update가 돌기 시작합니다.
        }
    }
}
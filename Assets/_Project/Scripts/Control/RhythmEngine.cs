using UnityEngine;
using System.Collections.Generic;
using TouchIT.Entity;
// using TouchIT.Boundary; // [삭제] Boundary 참조 제거

namespace TouchIT.Control
{
    public class RhythmEngine
    {
        // 설정값
        private float _bpm = 120f; // 1분당 120박자
        private float _noteSpeed = 180f; // 초당 회전 속도

        // 상태
        private double _nextSpawnTime;
        private double _audioTime;
        private MusicComposer _composer;

        // 인터페이스를 통해 대화함 (구체적인 View 모름)
        private IGameView _view;
        private IAudioManager _audio;

        private int _noteIdCounter = 0;

        public void Initialize(IGameView view, IAudioManager audio)
        {
            _view = view;
            _audio = audio;
            _composer = new MusicComposer();
            _nextSpawnTime = AudioSettings.dspTime + 1.0f; // 1초 뒤 시작
            _noteIdCounter = 0;
        }

        // 매 프레임 호출
        public void OnUpdate()
        {
            _audioTime = AudioSettings.dspTime;

            // 노트 스폰 타이밍 체크
            if (_audioTime >= _nextSpawnTime)
            {
                SpawnBeatNote();
                _nextSpawnTime += 60.0 / _bpm;
            }
        }

        private void SpawnBeatNote()
        {
            // 1. 다음 음계 가져오기
            int soundIdx = _composer.GetNextNoteIndex();

            // 2. 노트 데이터 생성 (Entity 사용)
            NoteData note = new NoteData(
                _noteIdCounter++,
                (float)_audioTime,
                270f,       // 270도(6시)에서 시작
                _noteSpeed, // 회전 속도
                soundIdx,
                NoteType.Normal
            );

            // 3. 뷰에게 생성 요청 (인터페이스 사용)
            _view.SpawnNote(note);
        }
    }
}
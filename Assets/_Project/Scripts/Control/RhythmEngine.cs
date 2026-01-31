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
        private float _noteSpeed = 90f; // 초당 회전 속도

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
            // [추가] 리듬 변칙 로직
            // 20% 확률로 쉼표 (노트 생성 안 함) -> 엇박자 느낌 남
            // [수정 2] 겹침 방지 (변칙/쉼표 추가)
            // 30% 확률로 노트 생성을 건너뜀 (쉼표)
            // 이러면 노트 사이에 공간이 생겨서 덜 겹쳐 보임
            if (UnityEngine.Random.value < 0.3f) return;

            // 1. 다음 음계 가져오기
            int soundIdx = _composer.GetNextNoteIndex();

            // [추가] 가끔 반대 방향(반시계)이나 속도 변화를 주면 더 재밌음
            // 지금은 일단 정직하게 생성

            // 2. 노트 데이터 생성
            NoteData note = new NoteData(
        _noteIdCounter++,
        (float)_audioTime,
        450f,       // [수정] 450도에서 시작
        _noteSpeed,
        soundIdx,
        NoteType.Normal
    );

            _view.SpawnNote(note);
        }
    }
}
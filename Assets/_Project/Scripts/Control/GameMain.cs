using UnityEngine;
using System.Collections.Generic;
using TouchIT.Entity;
// using TouchIT.Boundary; // [삭제] Boundary 절대 참조 금지!

namespace TouchIT.Control
{
    public class GameMain : MonoBehaviour
    {
        // 구체적인 클래스 대신 인터페이스 사용
        private IGameView _view;
        private IAudioManager _audio;
        private RhythmEngine _engine;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            _engine = new RhythmEngine();
        }

        // Binder에 의해 호출됨 (의존성 주입)
        public void Initialize(IGameView view, IAudioManager audio)
        {
            _view = view;
            _audio = audio;
            _engine.Initialize(view, audio);
        }

        private void Update()
        {
            if (_view == null) return; // 초기화 전엔 실행 X

            // 1. 엔진 업데이트
            _engine.OnUpdate();

            // 2. 노트 회전 업데이트
            // GameMain은 구체적인 NoteView를 모르지만, INoteView를 통해 회전 명령을 내림
            float dt = Time.deltaTime;
            foreach (var note in _view.GetActiveNotes())
            {
                note.UpdateRotation(dt);
                // 화면 밖으로 나간 것 처리 로직 등 추가 가능
            }

            // 3. 터치 입력
            if (Input.GetMouseButtonDown(0))
            {
                CheckHit();
            }
        }

        private void CheckHit()
        {
            INoteView closestNode = null;
            float minDiff = float.MaxValue;
            float targetAngle = 90f;

            // 인터페이스를 통해 순회
            foreach (var note in _view.GetActiveNotes())
            {
                float diff = Mathf.Abs(Mathf.DeltaAngle(note.CurrentAngle, targetAngle));
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closestNode = note;
                }
            }

            if (closestNode != null && minDiff <= 15f)
            {
                Debug.Log($"HIT! (Diff: {minDiff:F2})");
                _view.PlayHitEffect();
                _audio.PlayNoteSound(closestNode.SoundIndex, 1);

                // 인터페이스를 넘겨서 반납 요청
                _view.ReturnNote(closestNode);
            }
            else
            {
                Debug.Log("MISS!");
                _view.ReduceLife(1);
            }
        }
    }
}
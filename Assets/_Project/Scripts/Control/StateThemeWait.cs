using UnityEngine;
using TouchIT.Entity;

namespace TouchIT.Control
{
    public class StateThemeWait : GameState
    {
        private float _waitTimer;
        private const float WAIT_DURATION = 5.0f;

        public StateThemeWait(GameController controller) : base(controller) { }

        public override void Enter()
        {
            Debug.Log("[State] 테마 변경 대기");
            _waitTimer = 0f;
            Controller.View.SetVisualTimer(1.0f, true);
        }

        public override void Update()
        {
            // [Fix] foreach 대신 역순 for문 사용 (안전하게 삭제하기 위해)
            var activeNotes = Controller.View.GetActiveNotes();
            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                var note = activeNotes[i];
                note.UpdateRotation(Time.deltaTime);

                // 화면 밖으로 나가면 삭제 (역순이라 에러 안 남)
                if (note.CurrentAngle < 45f)
                {
                    note.ReturnToPool();
                }
            }

            // 타이머 로직
            _waitTimer += Time.deltaTime;
            float progress = 1.0f - (_waitTimer / WAIT_DURATION);

            Controller.View.SetVisualTimer(progress, true);

            if (_waitTimer >= WAIT_DURATION)
            {
                Debug.Log("실패! 강제 변경");
                Controller.Audio.PlaySfx("Miss");
                Controller.View.ReduceLife();
                ApplyThemeChange();
            }
        }

        // ... (나머지 OnSwipe, ApplyThemeChange 등은 기존과 동일)
        public override void OnSwipe(Vector2 dir)
        {
            Debug.Log("성공! 테마 변경");
            Controller.Audio.PlaySfx("Swipe");
            Controller.View.ClearAllNotes(true);
            ApplyThemeChange();
        }

        private void ApplyThemeChange()
        {
            NoteColor nextTheme = Controller.CurrentTheme;
            if (Controller.CurrentTheme == NoteColor.White) nextTheme = NoteColor.Black;
            else if (Controller.CurrentTheme == NoteColor.Black) nextTheme = NoteColor.Cosmic;
            else nextTheme = NoteColor.White;

            Controller.SetTheme(nextTheme);
            Controller.ChangeState(new StateNormal(Controller));
        }

        public override void Exit()
        {
            Controller.View.SetVisualTimer(0f, false);
        }
    }
}
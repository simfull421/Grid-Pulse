using UnityEngine;
using TouchIT.Entity;

namespace TouchIT.Control
{
    public class StateSurvival : GameState
    {
        private INoteView _holdingNote = null;
        private int _touchFrame;

        public StateSurvival(GameController controller) : base(controller) { }

        public override void Enter()
        {
            Debug.Log("[State] Survival Mode (Defend the Flame)");
            _holdingNote = null;
        }

        public override void Update()
        {
            float dt = Time.deltaTime;
            Controller.Engine.OnUpdate(); // 노트 생성
            UpdateNotes(dt);

            // 홀드 노트 로직 (기존 유지)
            if (_holdingNote != null)
            {
                Controller.View.SetHoldEffect(true);

                // 꼬리가 지나갈 때까지 잡고 있으면 성공
                if (_holdingNote.TailAngle < 80f)
                {
                    CompleteHold(true);
                }

                // 터치를 뗐는지 체크 (안전장치)
                if (!Input.GetMouseButton(0))
                {
                    ProcessHoldRelease();
                }
            }
        }

        private void UpdateNotes(float dt)
        {
            var activeNotes = Controller.View.GetActiveNotes();
            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                var note = activeNotes[i];
                if (note == _holdingNote)
                {
                    note.UpdateRotation(dt);
                    continue;
                }

                note.UpdateRotation(dt);

                if (!note.IsHittable) continue;

                // 놓침 판정 (65도 미만)
                if (note.CurrentAngle < 65f)
                {
                    Controller.OnNoteMiss(note);
                }
            }
        }

        public override void OnTouch(Vector2 pos)
        {
            if (_holdingNote != null) return;

            var hitNote = Controller.HitSystem.TryHit(Controller.View.GetActiveNotes());
            if (hitNote != null)
            {
                if (hitNote.Type == NoteType.Hold)
                {
                    _holdingNote = hitNote;
                    _touchFrame = Time.frameCount;
                    Controller.View.SetHoldEffect(true);

                    // [수정] 홀드 시작 이펙트: Color -> Type 전달
                    Controller.View.PlayHitEffect(hitNote.Position, hitNote.Type);
                }
                else
                {
                    Controller.OnNoteHit(hitNote);
                }
            }
        }

        public override void OnTouchUp()
        {
            ProcessHoldRelease();
        }

        private void ProcessHoldRelease()
        {
            if (_holdingNote == null) return;
            if (Time.frameCount <= _touchFrame + 5) return;

            float tailAngle = _holdingNote.TailAngle;
            float diff = Mathf.Abs(tailAngle - 90f);

            if (diff <= 30f) // 성공
            {
                CompleteHold(true);
            }
            else if (tailAngle > 120f) // 너무 빨리 뗌
            {
                CompleteHold(false);
            }
            // 늦게 뗀 건 관대하게 성공 처리하거나 무시
        }

        private void CompleteHold(bool success)
        {
            Controller.View.SetHoldEffect(false);
            if (success) Controller.OnNoteHit(_holdingNote); // 히트 처리
            else Controller.OnNoteMiss(_holdingNote); // 미스 처리

            _holdingNote = null;
        }

        public override void Exit()
        {
            Controller.View.SetHoldEffect(false);
        }
    }
}
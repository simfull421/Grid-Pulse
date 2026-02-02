using UnityEngine;
using System.Collections.Generic;
using TouchIT.Entity;

namespace TouchIT.Control
{
    public class StateNormal : GameState
    {
        private float _themeTimer;
        private const float THEME_CYCLE = 15f;
        private bool _isThemeChangePending = false;

        // [신규] 현재 홀드 중인 노트 (없으면 null)
        private INoteView _holdingNote = null;

        public StateNormal(GameController controller) : base(controller) { }

        public override void Enter()
        {
            _themeTimer = 0f;
            _isThemeChangePending = false;
            _holdingNote = null;
        }

        public override void Update()
        {
            float dt = Time.deltaTime;
            Controller.Engine.OnUpdate();
            UpdateNotes(dt);

            // [홀드 노트 체크]
            if (_holdingNote != null)
            {
                // 꼬리가 다 지나갔는데(0도 미만) 아직 잡고 있다면 -> 미스
                if (_holdingNote.CurrentAngle < 0f)
                {
                    Controller.Audio.PlaySfx("Miss");
                    Controller.ResetCombo();

                    Controller.View.SetHoldEffect(false); // [중요] 이펙트 끄기

                    _holdingNote.ReturnToPool();
                    _holdingNote = null;
                }
            }

            // 테마 변경 로직 (홀드 중엔 테마 변경 안 함)
            if (_holdingNote == null)
            {
                _themeTimer += dt;
                if (_themeTimer >= THEME_CYCLE) _isThemeChangePending = true;
                if (_isThemeChangePending && Controller.Engine.IsIdle)
                {
                    Controller.ChangeState(new StateThemeWait(Controller));
                }
            }
        }

        private void UpdateNotes(float dt)
        {
            var activeNotes = Controller.View.GetActiveNotes();
            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                var note = activeNotes[i];

                // [중요] 홀드 중인 노트는 시스템이 자동 삭제하면 안 됨 (플레이어가 잡고 있으니까)
                if (note == _holdingNote)
                {
                    note.UpdateRotation(dt); // 회전은 계속 함
                    continue;
                }

                note.UpdateRotation(dt);

                if (!note.IsHittable) continue;

                if (note.CurrentAngle < 45f)
                {
                    Controller.Audio.PlaySfx("Miss");
                    Controller.ResetCombo();
                    note.ReturnToPool();
                }
            }
        }

        public override void OnTouch(Vector2 pos)
        {
            // 이미 홀드 중이면 다른 터치 무시 (단일 터치 기준)
            if (_holdingNote != null) return;

            var hitNote = Controller.HitSystem.TryHit(Controller.View.GetActiveNotes());

            if (hitNote != null)
            {
                // 1. 홀드 노트인 경우 -> 잡기 시작!
                if (hitNote.Type == NoteType.Hold)
                {
                    _holdingNote = hitNote; // 홀드 시작
                    Controller.Audio.PlaySfx("Hit"); // 시작 소리
                    // 이펙트는 터뜨리지만 노트는 반납 안 함!
                    Controller.View.PlayHitEffect(hitNote.Position, hitNote.Color);
                    Controller.View.SetHoldEffect(true); // [중요] 루프 이펙트 ON!
                }
                // 2. 일반 노트인 경우 -> 즉시 처리
                else
                {
                    Controller.Audio.PlaySfx("Hit");
                    Controller.View.PlayHitEffect(hitNote.Position, hitNote.Color);
                    hitNote.ReturnToPool();
                    Controller.AddCombo();
                }
            }
        }

        // [신규] 손 뗐을 때 (롱노트 마무리 판정)
        public override void OnTouchUp()
        {// 손 뗐으니 무조건 이펙트 끄기
            Controller.View.SetHoldEffect(false);
            if (_holdingNote != null)
            {
                if (_holdingNote.CurrentAngle > 80f)
                {
                    // 너무 빨리 뗌 -> 미스
                    Controller.Audio.PlaySfx("Miss");
                    Controller.ResetCombo();
                }
                else
                {
                    // 성공!
                    Controller.Audio.PlaySfx("Hit");

                    // [Fix] Color.cyan 대신 NoteColor.Cosmic을 넘김 (뷰에서 이걸 Cyan으로 해석하게 함)
                    Controller.View.PlayHitEffect(_holdingNote.Position, NoteColor.Cosmic);

                    Controller.AddCombo();
                }

                _holdingNote.ReturnToPool();
                _holdingNote = null;
            }
        }
        public override void Exit()
        {
            Controller.View.SetHoldEffect(false); // [안전장치] 나갈 때 끄기
            if (_holdingNote != null)
            {
                _holdingNote.ReturnToPool();
                _holdingNote = null;
            }
        }
    }
}
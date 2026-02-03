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
        private INoteView _holdingNote = null;
        private int _touchFrame;
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

            // [홀드 중일 때]
            if (_holdingNote != null)
            {
                // 1. 손을 떼버렸는지 체크 (터치/마우스 없으면)
                if (!Input.GetMouseButton(0))
                {
                    // 꼬리 판정 (OnTouchUp에서 처리하므로 여기선 패스하거나, 안전장치)
                    // OnTouchUp이 호출 안 되는 예외 케이스(화면 밖 드래그 등) 방지용
                    ProcessHoldRelease();
                    return;
                }

                // 2. 상시 이펙트 위치 업데이트 (노트는 계속 회전하므로 12시 방향 고정)
                // VfxManager는 이미 12시 고정이므로 호출만 유지하면 됨.
                Controller.View.SetHoldEffect(true);

                // 3. 꼬리가 판정선을 완전히 지나갔는데도 계속 잡고 있는 경우 (자동 성공 처리)
                // 꼬리 각도가 90도(12시)보다 훨씬 작아짐 (예: 80도)
                if (_holdingNote.TailAngle < 80f)
                {
                    CompleteHoldNote(true); // 성공 처리
                }
            }
        }
        private void ProcessHoldRelease()
        {
            if (_holdingNote == null) return;

            // 판정 기준: 꼬리(TailAngle)가 12시(90도) 근처인가?
            float diff = Mathf.Abs(_holdingNote.TailAngle - 90f);

            if (diff <= 25f) // Good 범위보다 약간 넉넉하게
            {
                CompleteHoldNote(true);
            }
            else if (_holdingNote.TailAngle > 115f) // 너무 빨리 뗌 (꼬리가 아직 멀었음)
            {
                Debug.Log("❌ Hold Fail: Released too early");
                Controller.Audio.PlaySfx("Miss");
                Controller.ResetCombo();
                CompleteHoldNote(false);
            }
            // 이미 지나간 경우는 Update에서 자동 처리됨
        }

        private void CompleteHoldNote(bool success)
        {
            Controller.View.SetHoldEffect(false); // 이펙트 끄기
            if (success)
            {
                Controller.Audio.PlaySfx("Hit");
                Controller.View.PlayHitEffect(_holdingNote.Position, NoteColor.Cosmic);
                Controller.AddCombo();
            }

            _holdingNote.ReturnToPool();
            _holdingNote = null;
        }
        // 2. 일반 노트 미스 체크 (UpdateNotes 내부)
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

                // [Fix] 미스 판정 타이밍 수정 (45도 -> 65도)
                // 12시(90도)를 지나고, 시각적으로 사라질 때쯤(80도 미만) 바로 미스 처리
                // 65도면 "판정선 지남 + 약간의 여유" 후 바로 아웃
                if (note.CurrentAngle < 65f)
                {
                    Debug.Log($"💢 [Miss] Passed Judgement Line. Angle: {note.CurrentAngle:F2}");
                    Controller.Audio.PlaySfx("Miss");
                    Controller.ResetCombo();
                    note.ReturnToPool();
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
                    // [LOG] 홀드 시작
                    Debug.Log($"✨ [HOLD START] Touch Angle: {hitNote.CurrentAngle:F2} (Target: 90.0)");

                    _holdingNote = hitNote;
                    _touchFrame = Time.frameCount;

                    Controller.Audio.PlaySfx("Hit");
                    Controller.View.PlayHitEffect(hitNote.Position, NoteColor.Cosmic);
                    Controller.View.SetHoldEffect(true);
                }
                else
                {
                    Controller.Audio.PlaySfx("Hit");
                    Controller.View.PlayHitEffect(hitNote.Position, hitNote.Color);
                    hitNote.ReturnToPool();
                    Controller.AddCombo();
                }
            }
        }

        public override void OnTouchUp()
        {
            Controller.View.SetHoldEffect(false);

            if (_holdingNote != null)
            {
                if (Time.frameCount <= _touchFrame + 5) return;

                float tailAngle = _holdingNote.TailAngle; // 꼬리의 현재 위치
                float diff = Mathf.Abs(tailAngle - 90f);  // 판정선(90도)과의 차이

                // [LOG] 릴리즈 판정 정보 상세 출력
                // TailAngle이 90보다 크면 아직 덜 온 것(Early), 작으면 지나간 것(Late)
                string timing = (tailAngle > 90f) ? "EARLY (Before Line)" : "LATE (Passed Line)";

                Debug.Log($"🛑 [HOLD RELEASE] Tail Angle: {tailAngle:F2} | Diff: {diff:F2} | Timing: {timing}");

                // 판정 범위 (30도)
                if (diff <= 30f)
                {
                    Debug.Log($"✅ [HOLD SUCCESS] Perfect Release! (Diff {diff:F2} <= 30)");
                    Controller.Audio.PlaySfx("Hit");
                    Controller.View.PlayHitEffect(_holdingNote.Position, NoteColor.Cosmic);
                    Controller.AddCombo();
                }
                else if (tailAngle > 120f) // 너무 빨리 뗌
                {
                    Debug.Log($"❌ [HOLD FAIL] Released Too Early! (Tail needs to be closer to 90)");
                    Controller.Audio.PlaySfx("Miss");
                    Controller.ResetCombo();
                }
                else
                {
                    // 이미 꼬리가 60도 미만으로 지나감 -> 늦게 뗐지만 보통은 성공 처리 해줌 (관대함)
                    Debug.Log($"⚠️ [HOLD LATE] Released Late but Accepted.");
                    Controller.AddCombo();
                }

                _holdingNote.ReturnToPool();
                _holdingNote = null;
            }
        }

        public override void Exit()
        {
            Controller.View.SetHoldEffect(false);
            if (_holdingNote != null)
            {
                _holdingNote.ReturnToPool();
                _holdingNote = null;
            }
        }
    }
}
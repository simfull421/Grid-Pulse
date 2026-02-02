using UnityEngine;
using System.Collections.Generic;
using TouchIT.Entity;

namespace TouchIT.Control
{
    public class HitJudgeSystem
    {
        private float _targetAngle; // 판정 기준 각도 (90도)

        // 판정 범위 (각도 단위)
        // Perfect: ±10도, Good: ±20도
        private const float PERFECT_WINDOW = 10f;
        private const float GOOD_WINDOW = 20f;

        public HitJudgeSystem(float targetAngle)
        {
            _targetAngle = targetAngle;
        }

        // 활성 노트 리스트를 순회하며 판정
        public INoteView TryHit(List<INoteView> activeNotes)
        {
            // 판정은 순서 상관 없지만, 가장 오래된(각도가 작은) 노트부터 검사하는 게 유리하므로
            // 보통 앞에서부터 검사하거나, 로직에 따라 다름. 여기선 foreach 써도 됨(삭제 안하니까).
            // 하지만 일관성을 위해 for문 권장.

            for (int i = 0; i < activeNotes.Count; i++)
            {
                var note = activeNotes[i];

                // 1. 상태 체크 (큐비트 개념): 절반 안 넘었으면 아예 판정 대상 아님 (무적)
                if (!note.IsHittable) continue;

                // 2. 각도 체크
                float diff = Mathf.Abs(Mathf.DeltaAngle(note.CurrentAngle, _targetAngle));

                if (diff <= GOOD_WINDOW)
                {
                    return note; // 판정 성공
                }
            }
            return null;
        }

        public HitResult GetResult(float noteAngle)
        {
            float diff = Mathf.Abs(Mathf.DeltaAngle(noteAngle, _targetAngle));
            if (diff <= PERFECT_WINDOW) return HitResult.Great;
            if (diff <= GOOD_WINDOW) return HitResult.Good;
            return HitResult.Miss;
        }
    }
}
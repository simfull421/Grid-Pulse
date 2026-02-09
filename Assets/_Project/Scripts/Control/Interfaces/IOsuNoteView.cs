using UnityEngine;
using TouchIT.Boundary;

namespace TouchIT.Control
{
    public interface IOsuNoteView : INoteView
    {
        // 💥 충돌 로직: 데미지를 입힘. 파괴되었으면 true 반환.
        bool TakeDamage();

        // 📍 위치 및 반경 정보 (충돌 판정용)
        Vector3 Position { get; }
        float Radius { get; } // 노트 크기 (충돌 범위)

        // 🛡️ 상태 정보
        bool IsHardNote { get; }
        int CurrentHP { get; }
    }
}
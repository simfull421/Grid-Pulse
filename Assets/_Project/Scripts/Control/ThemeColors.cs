using UnityEngine;

namespace TouchIT.Entity
{
    // [간소화] 불필요한 Enum 삭제, 단일 정적 컬러 관리
    public static class ThemeColors
    {
        // Monster Energy 스타일 (Deep Dark + Neon Green/Gold)
        public static readonly Color Background = new Color(0.05f, 0.05f, 0.07f); // 칠흑 같은 어둠
        public static readonly Color RingLine = new Color(0.2f, 0.2f, 0.2f);      // 꺼진 링 (회색)
        public static readonly Color RingFire = new Color(1.0f, 0.6f, 0.0f);      // 불붙은 링 (주황)

        public static readonly Color EmberCore = new Color(1.0f, 1.0f, 0.8f);     // 불꽃 심지 (밝은 노랑)
        public static readonly Color EmberOuter = new Color(0.2f, 1.0f, 0.2f);    // 불꽃 외곽 (형광 초록)

        public static readonly Color NoteNormal = new Color(0.2f, 1.0f, 0.2f);    // 노트 (초록)
        public static readonly Color NoteHold = new Color(0.0f, 1.0f, 1.0f);      // 홀드 (청록)
    }
}
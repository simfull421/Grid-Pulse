using UnityEngine;

namespace TouchIT.Entity
{
    public static class ThemeColors
    {
        public struct ThemeSet
        {
            public Color Background;
            public Color Foreground;
            public Color Note;
        }

        public static ThemeSet GetColors(NoteColor theme)
        {
            switch (theme)
            {
                case NoteColor.White:
                    return new ThemeSet
                    {
                        Background = new Color(0.95f, 0.95f, 0.95f),
                        Foreground = Color.black,
                        Note = Color.black
                    };

                case NoteColor.Black:
                    return new ThemeSet
                    {
                        Background = new Color(0.1f, 0.1f, 0.12f),
                        Foreground = Color.white,
                        Note = Color.white
                    };

                case NoteColor.Cosmic:
                    // [수정] Cyan -> Gold / Amber (고급스럽고 강렬하게)
                    return new ThemeSet
                    {
                        Background = new Color(0.2f, 0.05f, 0.05f), // 더 어두운 핏빛
                        Foreground = new Color(1.0f, 0.8f, 0.2f),   // 황금색 링
                        Note = new Color(1.0f, 0.5f, 0.0f)          // 불타는 주황 노트
                    };

                default: return GetColors(NoteColor.White);
            }
        }
    }
}
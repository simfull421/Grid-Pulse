using UnityEngine;
using TouchIT.Entity;

public static class ThemeColors
{
    // 테마별 색상 세트 구조체
    public struct ThemeSet
    {
        public Color Background; // 배경색
        public Color Foreground; // 구체, 링, 이펙트 색상
        public Color Note;       // 노트 색상
    }

    public static ThemeSet GetColors(NoteColor theme)
    {
        switch (theme)
        {
            case NoteColor.White:
                // [White 테마] 배경: 흰색 / 링: 검정 / 노트: 검정 (배경과 대비되어야 함)
                return new ThemeSet
                {
                    Background = new Color(0.95f, 0.95f, 0.95f),
                    Foreground = Color.black,
                    Note = Color.black
                };

            case NoteColor.Black:
                // [Black 테마] 배경: 검정 / 링: 흰색 / 노트: 흰색
                return new ThemeSet
                {
                    Background = new Color(0.1f, 0.1f, 0.12f),
                    Foreground = Color.white,
                    Note = Color.white
                };

            case NoteColor.Cosmic:
                // [Cosmic 테마] 배경: 붉은색(위기) / 링: 밝은 노랑 / 노트: 형광 사이안
                // (어려운 패턴 느낌)
                return new ThemeSet
                {
                    Background = new Color(0.3f, 0.0f, 0.0f), // 짙은 빨강 배경
                    Foreground = new Color(1f, 0.9f, 0.5f),   // 레몬색 링
                    Note = Color.cyan                         // 눈에 띄는 사이안 노트
                };

            default:
                return GetColors(NoteColor.White);
        }
    }
}
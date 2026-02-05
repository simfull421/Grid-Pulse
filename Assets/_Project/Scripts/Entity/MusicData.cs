using UnityEngine;
using System.Collections.Generic;

namespace TouchIT.Entity
{
    // 노트의 종류
    public enum NoteType
    {
        Tap = 0,    // 일반 터치
        Hold = 1,   // 꾹 누르기
        Drag = 2,   // 긁기
        Hyper = 3   // Osu 모드 노트
    }

    [System.Serializable]
    public class NoteInfo
    {
        public float Time;      // 재생 시간 (초)
        public NoteType Type;   // 노트 타입
        public int LaneIndex;   // (0~31) 링의 어느 위치에서 나올지 (각도)
    }

    [CreateAssetMenu(fileName = "NewMusicData", menuName = "TouchIT/Music Data")]
    public class MusicData : ScriptableObject
    {
        [Header("Basic Info")]
        public string Title;
        public AudioClip Clip;
        public float BPM = 120f;

        [Header("Theme")]
        public Color ThemeColor = Color.white; // 곡 분위기에 따른 테마 색상

        [Header("Pattern Data")]
        public List<NoteInfo> Notes = new List<NoteInfo>();
    }
}
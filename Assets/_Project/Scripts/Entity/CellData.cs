using System.Collections.Generic;
using UnityEngine;
namespace ReflexPuzzle.Entity
{
    // 게임의 4가지 핵심 모드
    public enum GameMode
    {
        Classic,    // 순수 타임어택 (Cosmic Blue)
        Color,      // 색상 그룹핑 (예: 1~3 노랑, 4~6 녹색)
        Mixed,      // 함정 피하기 (빨간 타일 건너뛰기)
        Memory      // 위치 기억 (타일 색상만 남고 숫자 사라짐)
    }

    // 개별 타일(격자 한 칸)의 데이터
    public struct CellData
    {
        public int Number;
        public int ColorID;
        public bool IsTrap;
        public bool IsHidden;
        public Vector3 WorldPos;

        // [수정] 매개변수 이름 일치 + pos는 기본값(0,0,0) 설정
        public CellData(int num, int color, bool trap, bool hidden, Vector3 pos = default)
        {
            Number = num;      // num -> Number
            ColorID = color;   // color -> ColorID
            IsTrap = trap;     // trap -> IsTrap
            IsHidden = hidden; // hidden -> IsHidden
            WorldPos = pos;
        }
    }

    // 생성된 스테이지 전체 정보
    public class StageInfo
    {
        public int Level;              // 현재 레벨
        public int GridSize;           // N x N (3, 4, 5)
        public float TimeLimit;        // 제한 시간 (초)
        public GameMode Mode;          // 현재 모드
        public List<CellData> Cells;   // 섞여진 타일 리스트

        public StageInfo(int level, int size, float time, GameMode mode)
        {
            Level = level;
            GridSize = size;
            TimeLimit = time;
            Mode = mode;
            Cells = new List<CellData>();
        }
    }
}
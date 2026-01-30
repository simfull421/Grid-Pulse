using System.Collections.Generic;

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
        public int Number;       // 표시될 숫자 (1 ~ N)
        public int ColorID;      // 타일 색상 ID (0:Blue, 1:Red(Trap), 2:Yellow...)
        public bool IsTrap;      // 함정 여부 (Mixed 모드에서 터치 금지)
        public bool IsHidden;    // Memory 모드에서 숫자 숨김 여부

        public CellData(int number, int colorID, bool isTrap = false, bool isHidden = false)
        {
            Number = number;
            ColorID = colorID;
            IsTrap = isTrap;
            IsHidden = isHidden;
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
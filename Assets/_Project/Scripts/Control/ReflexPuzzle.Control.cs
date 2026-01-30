using System;
using System.Collections.Generic;
using System.Linq;
using ReflexPuzzle.Entity;

namespace ReflexPuzzle.Control
{
    public class GridGenerator
    {
        // 시드 기반 난수 생성기 (재현 가능성 확보)
        private Random _rng;

        public GridGenerator()
        {
            _rng = new Random();
        }

        // [핵심] 레벨과 모드만 던져주면 스테이지 데이터를 뱉어냄
        public StageInfo CreateStage(int level, GameMode mode)
        {
            // 1. 난이도 역산 (레벨에 따른 격자 크기 및 시간 계산)
            int gridSize = CalculateGridSize(level);
            float timeLimit = CalculateTimeLimit(level, gridSize);

            // 2. 스테이지 기본 틀 생성
            StageInfo stage = new StageInfo(level, gridSize, timeLimit, mode);

            // 3. 시드 초기화 (레벨이 같으면 항상 같은 배치가 나오도록)
            _rng = new Random(level * 1000 + (int)mode);

            // 4. 셀 데이터 생성 및 모드별 규칙 적용
            int totalCells = gridSize * gridSize;
            List<CellData> rawCells = new List<CellData>();

            for (int i = 1; i <= totalCells; i++)
            {
                rawCells.Add(GenerateCellByMode(i, totalCells, mode));
            }

            // 5. 셔플 (Fisher-Yates Shuffle) - 위치만 섞음
            stage.Cells = Shuffle(rawCells);

            return stage;
        }

        // 레벨별 격자 크기 공식 (3 -> 4 -> 5)
        private int CalculateGridSize(int level)
        {
            // 1~10레벨: 3x3, 11~25레벨: 4x4, 26~: 5x5
            if (level <= 10) return 3;
            if (level <= 25) return 4;
            return 5;
        }

        // 제한 시간 계산 공식 (타이트하게)
        private float CalculateTimeLimit(int level, int gridSize)
        {
            float baseTime = gridSize * gridSize * 0.8f; // 기본: 개당 0.8초
            float penalty = (float)Math.Log10(level) * 1.5f; // 레벨이 오를수록 시간 차감
            return Math.Max(baseTime - penalty, 3.0f); // 최소 3초 보장
        }

        // [모드별 로직] 숫자(i)에 따라 타일 색상과 속성 결정
        private CellData GenerateCellByMode(int number, int totalCount, GameMode mode)
        {
            int colorID = 0; // 0: Default Cosmic Blue
            bool isTrap = false;
            bool isHidden = false;

            switch (mode)
            {
                case GameMode.Classic:
                    // 전원 동일 색상
                    colorID = 0;
                    break;

                case GameMode.Color:
                    // 숫자 구간별 색상 분배 (예: 1/3은 노랑, 1/3은 초록...)
                    // 예: 9개일 때 1~3:Color1, 4~6:Color2, 7~9:Color3
                    int groupSize = (int)Math.Ceiling(totalCount / 3.0);
                    colorID = (number - 1) / groupSize + 1; // 1, 2, 3...
                    break;

                case GameMode.Mixed:
                    // 특정 확률로 함정(빨강) 생성. 단, 1번은 절대 함정이 아니어야 함.
                    // 간단한 규칙: 3의 배수이거나 특정 조건일 때 Trap
                    // 여기서는 난수 대신 고정 규칙을 사용해 '예측 가능성' 부여 (설계 변경 가능)
                    if (number > 1 && _rng.Next(0, 100) < 30) // 30% 확률로 함정 (시드 기반이라 고정됨)
                    {
                        colorID = 99; // 99: Red (Danger)
                        isTrap = true;
                    }
                    else
                    {
                        colorID = 0;
                    }
                    break;

                case GameMode.Memory:
                    // 색상은 랜덤 or 순차, 숫자는 숨김 처리될 예정
                    colorID = 0;
                    isHidden = true; // View에서 이 플래그를 보고 일정 시간 후 숫자를 끕니다.
                    break;
            }

            return new CellData(number, colorID, isTrap, isHidden);
        }

        // 리스트 섞기 (표준 알고리즘)
        private List<T> Shuffle<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
            return list;
        }
    }
}
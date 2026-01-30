using System.Collections.Generic;
using ReflexPuzzle.Entity;

namespace ReflexPuzzle.Control
{
    // 판정 결과 반환용 열거형
    public enum MatchResult
    {
        None,           // 아무 일도 없음 (무시)
        Success,        // 정답 (콤보 유지)
        Fail_Wrong,     // 오답 (순서 틀림 or 함정 터치)
        Fail_TimeOut,   // 시간 초과
        StageClear      // 모든 숫자 클리어
    }

    public class MatchEngine
    {
        private StageInfo _currentStage;
        private Queue<int> _targetQueue; // 눌러야 할 정답 숫자들 (1, 2, 3...)
        private float _currentLifeTime;
        private bool _isGameActive;

        public int CurrentScore { get; private set; }
        public int CurrentCombo { get; private set; }

        public MatchEngine()
        {
            _targetQueue = new Queue<int>();
        }

        // 게임 시작 시 초기화
        public void Initialize(StageInfo stage)
        {
            _currentStage = stage;
            _currentLifeTime = stage.TimeLimit;
            _isGameActive = true;
            CurrentScore = 0;
            CurrentCombo = 0;

            _targetQueue.Clear();

            // 정답 큐 생성 로직
            // Mixed 모드 등에서 '함정'이 아닌 숫자만 순서대로 큐에 넣음
            // (함정 타일은 눌러야 할 대상이 아니므로 큐에 넣지 않음)
            List<CellData> sortedCells = new List<CellData>(stage.Cells);
            sortedCells.Sort((a, b) => a.Number.CompareTo(b.Number));

            foreach (var cell in sortedCells)
            {
                // 함정이 아니고 숨겨진 상태가 아니라면 정답 후보
                // (기획에 따라 Memory 모드는 다를 수 있음, 여기선 Mixed 기준)
                if (!cell.IsTrap)
                {
                    _targetQueue.Enqueue(cell.Number);
                }
            }
        }

        // 프레임마다 시간 감소 (Boundary의 Update에서 호출)
        public MatchResult Tick(float deltaTime)
        {
            if (!_isGameActive) return MatchResult.None;

            _currentLifeTime -= deltaTime;
            if (_currentLifeTime <= 0)
            {
                _isGameActive = false;
                return MatchResult.Fail_TimeOut;
            }
            return MatchResult.None;
        }

        // 유저가 타일을 터치했을 때 호출
        public MatchResult SubmitInput(CellData inputCell)
        {
            if (!_isGameActive) return MatchResult.None;

            // 1. 함정을 건드렸는가?
            if (inputCell.IsTrap)
            {
                _isGameActive = false;
                return MatchResult.Fail_Wrong; // 함정 밟음 -> 즉사
            }

            // 2. 현재 눌러야 할 숫자인가?
            if (_targetQueue.Count > 0)
            {
                int expectedNumber = _targetQueue.Peek();

                if (inputCell.Number == expectedNumber)
                {
                    // 정답!
                    _targetQueue.Dequeue();
                    CurrentCombo++;
                    CurrentScore += (100 * CurrentCombo); // 콤보 보너스

                    // 시간 조금 회복 (옵션)
                    _currentLifeTime += 0.5f;

                    if (_targetQueue.Count == 0)
                    {
                        _isGameActive = false;
                        return MatchResult.StageClear;
                    }

                    return MatchResult.Success;
                }
                else
                {
                    // 순서 틀림 (1번 눌러야 하는데 3번 누름)
                    _isGameActive = false;
                    return MatchResult.Fail_Wrong;
                }
            }

            return MatchResult.None;
        }

        // 헬퍼: 남은 시간 비율 (UI 표시용)
        public float GetTimeRatio()
        {
            if (_currentStage == null || _currentStage.TimeLimit <= 0) return 0f;
            return _currentLifeTime / _currentStage.TimeLimit;
        }
    }
}
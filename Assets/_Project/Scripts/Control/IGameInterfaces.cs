using UnityEngine;
using ReflexPuzzle.Entity;
using System.Threading;

namespace ReflexPuzzle.Control
{
    // 1. 화면(격자) 제어 인터페이스
    public interface IGridView
    {
        void BuildGrid(StageInfo stage);
        void ClearGrid();
        void TriggerRefresh(StageInfo nextStage, System.Action onComplete);
        // [추가] 이펙트 재생 요청
        void PlayTouchEffect(Vector3 position);
    }

    // 2. 입력 제어 인터페이스 (비동기)
    public interface IInputReader
    {
        Awaitable<CellData> WaitForCellInputAsync(CancellationToken token);
        Awaitable WaitForAnyTouchAsync(CancellationToken token);

        bool TryGetCellInput(out CellData data);
    }

    // 3. UI 제어 인터페이스 (로비/게임 패널)
    public interface IGameUI
    {
        void ShowTitle(); // [추가] 타이틀 화면("터치하여 시작")
        void ShowLobby();
        void ShowGameUI();
        // [추가] 모드 설명 텍스트 갱신
        void UpdateModeDescription(string title, string desc);
        // [수정] 모드 선택 대기 (터치 입력까지 포함)
        Awaitable<GameMode> WaitForModeSelectionAsync(System.Threading.CancellationToken token);
        // [추가] 이 줄이 없어서 에러가 났던 겁니다. 추가해주세요!
        void UpdateGameStatus(float time, int level);
    }
}
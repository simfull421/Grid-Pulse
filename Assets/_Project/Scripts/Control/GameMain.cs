using UnityEngine;
using ReflexPuzzle.Entity;
using System.Threading;
// GPGS, AdMob 네임스페이스 추가 (실제 SDK 설치 후 주석 해제)
using GoogleMobileAds.Api;
using GooglePlayGames;
using GooglePlayGames.BasicApi;

namespace ReflexPuzzle.Control
{
    public class GameMain : MonoBehaviour
    {
        private IGridView _view;
        private IInputReader _input;
        private IGameUI _ui;

        private GridGenerator _generator;
        private MatchEngine _matchEngine;
        private CancellationTokenSource _cts;

        public void Initialize(IGridView view, IInputReader input, IGameUI ui)
        {
            _view = view;
            _input = input;
            _ui = ui;
        }

        private void Awake()
        {
            _generator = new GridGenerator();
            _matchEngine = new MatchEngine();
            _cts = new CancellationTokenSource();

            // [배터리 최적화]
            Application.targetFrameRate = 60;
        }

        public void RunGameLoop()
        {
            _ = MainLoop(_cts.Token);
        }

        private void OnDestroy()
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        private async Awaitable MainLoop(CancellationToken token)
        {
            // 1. [SYSTEM LOADING] GPGS / AdMob 초기화
            // Debug.Log("Initializing Services...");
            // await InitializeServices(token); 

            while (!token.IsCancellationRequested)
            {
                // 2. [LOBBY]
                Debug.Log("State: Lobby");
                _ui.ShowLobby();
                _ui.UpdateModeDescription("", "모드를 선택해주세요."); // 초기 문구

                // Attract Mode (배경용 랜덤 패턴)
                StageInfo lobbyStage = _generator.CreateStage(99, GameMode.Mixed);
                _view.BuildGrid(lobbyStage);

                // 3. [MODE SELECTION - HYBRID]
                GameMode selectedMode = GameMode.Classic;
                bool isModeConfirmed = false;

                while (!isModeConfirmed)
                {
                    // UI에서 모드 버튼을 누를 때까지 대기
                    selectedMode = await _ui.WaitForModeSelectionAsync(token);

                    // 모드가 선택됨 -> 설명 보여주고 확대 연출 (UI쪽에서 처리)
                    // 여기서 바로 시작 안 하고, "한 번 더 터치" 혹은 "START 버튼" 대기 로직 필요
                    // 간단하게: UI가 '선택됨' 상태에서 다시 누르면 WaitForModeSelectionAsync가 리턴되도록 구현

                    isModeConfirmed = true; // 지금은 선택하면 바로 시작 (원하면 여기서 확인 단계 추가 가능)
                }

                // 4. [GAME START]
                Debug.Log($"State: Game Start ({selectedMode})");
                _ui.ShowGameUI();

                int currentLevel = 1;
                bool isGameOver = false;

                while (!isGameOver && !token.IsCancellationRequested)
                {
                    // 안드로이드 백버튼 처리 (로비로 나가기)
                    if (Input.GetKeyDown(KeyCode.Escape))
                    {
                        isGameOver = true;
                        break;
                    }

                    StageInfo stage = _generator.CreateStage(currentLevel, selectedMode);
                    _matchEngine.Initialize(stage);

                    // 연출 분기
                    if (currentLevel == 1) _view.BuildGrid(stage);
                    else await PlayRefreshAnim(stage, token);

                    // 플레이 루프
                    bool stageClear = false;
                    while (!stageClear)
                    {
                        // 안드로이드 백버튼 (게임 중 일시정지/나가기)
                        if (Input.GetKeyDown(KeyCode.Escape))
                        {
                            isGameOver = true;
                            stageClear = true;
                            break;
                        }

                        // 입력 대기
                        CellData inputData = await _input.WaitForCellInputAsync(token);

                        // [로그 확인용] 터치 반응 체크
                        if (inputData.Number != 0)
                            Debug.Log($"Touch Input: Num {inputData.Number}, Trap {inputData.IsTrap}");

                        MatchResult result = _matchEngine.SubmitInput(inputData);

                        if (result == MatchResult.Success)
                        {
                            _view.PlayTouchEffect(inputData.WorldPos);
                        }
                        else if (result == MatchResult.StageClear)
                        {
                            stageClear = true;
                        }
                        else if (result == MatchResult.Fail_Wrong || result == MatchResult.Fail_TimeOut)
                        {
                            Debug.Log("Game Over!");
                            isGameOver = true;
                            stageClear = true;
                            await Awaitable.WaitForSecondsAsync(1.0f, token);
                        }
                    }

                    if (!isGameOver) currentLevel++;
                }
            }
        }

        // 헬퍼: 백덤블링 대기
        private async Awaitable PlayRefreshAnim(StageInfo stage, CancellationToken token)
        {
            bool animDone = false;
            _view.TriggerRefresh(stage, () => animDone = true);
            while (!animDone && !token.IsCancellationRequested) await Awaitable.NextFrameAsync(token);
        }

        private async Awaitable InitializeServices(CancellationToken token)
        {
            // GPGS Init
            // PlayGamesPlatform.Activate();
            // await Social.localUser.Authenticate((bool success) => { });
            
            // AdMob Init
            // MobileAds.Initialize(initStatus => { });
            
            // 가짜 로딩 시간 (로고 보여줄 시간)
            await Awaitable.WaitForSecondsAsync(2.0f, token);
        }
        
    }
}
using UnityEngine;
using ReflexPuzzle.Entity;
using System.Threading;
// GPGS, AdMob 네임스페이스 추가 (실제 SDK 설치 후 주석 해제)
// using GoogleMobileAds.Api;
// using GooglePlayGames;
// using GooglePlayGames.BasicApi;

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
            // 1. [SYSTEM LOADING] (필요시 주석 해제)
            // await InitializeServices(token);

            while (!token.IsCancellationRequested)
            {
                // ----------------------------------------------------
                // 0. [TITLE SCREEN]
                // ----------------------------------------------------
                Debug.Log("State: Title");
                _ui.ShowTitle();

                // 배경용 격자 보여주기
                StageInfo lobbyStage = _generator.CreateStage(99, GameMode.Mixed);
                _view.BuildGrid(lobbyStage);

                // 아무 터치나 기다림
                await _input.WaitForAnyTouchAsync(token);

                // ----------------------------------------------------
                // 1. [LOBBY] - 모드 선택
                // ----------------------------------------------------
                Debug.Log("State: Lobby");
                _ui.ShowLobby();

                // 모드 선택 대기
                GameMode selectedMode = await _ui.WaitForModeSelectionAsync(token);

                // ----------------------------------------------------
                // 2. [GAME START]
                // ----------------------------------------------------
                Debug.Log($"State: Game Start ({selectedMode})");
                _ui.ShowGameUI();

                int currentLevel = 1;
                bool isGameOver = false;

                // [시간 설정] 초기 30초
                float remainingTime = 30.0f;

                while (!isGameOver && !token.IsCancellationRequested)
                {
                    // 안드로이드 백버튼 (로비로 나가기)
                    if (Input.GetKeyDown(KeyCode.Escape))
                    {
                        isGameOver = true;
                        break;
                    }

                    // 스테이지 생성
                    StageInfo stage = _generator.CreateStage(currentLevel, selectedMode);

                    // [레벨업 보너스] 2레벨부터 시간 조금 추가 (난이도 조절용)
                    if (currentLevel > 1) remainingTime += 5.0f;

                    _matchEngine.Initialize(stage);

                    // 연출 분기
                    if (currentLevel == 1) _view.BuildGrid(stage);
                    else await PlayRefreshAnim(stage, token);

                    // ------------------------------------------------
                    // 3. [PLAY LOOP] - 실시간 프레임 루프
                    // ------------------------------------------------
                    bool stageClear = false;

                    while (!stageClear && !isGameOver)
                    {
                        // A. 백버튼 체크
                        if (Input.GetKeyDown(KeyCode.Escape))
                        {
                            isGameOver = true;
                            stageClear = true;
                            break;
                        }

                        // B. 시간 감소 및 타임오버 체크
                        remainingTime -= Time.deltaTime;
                        if (remainingTime <= 0f)
                        {
                            remainingTime = 0f;
                            Debug.Log("Time Over!");
                            isGameOver = true;
                            stageClear = true;
                            // 타임오버 UI 갱신 (0.00초 보여주기 위해)
                            _ui.UpdateGameStatus(remainingTime, currentLevel);
                            break;
                        }

                        // C. UI 갱신 (매 프레임)
                        _ui.UpdateGameStatus(remainingTime, currentLevel);

                        // D. 입력 체크 (기다리지 않음! TryGet 사용)
                        // [수정됨] await WaitForCellInputAsync 대신 TryGetCellInput 사용
                        if (_input.TryGetCellInput(out CellData inputData))
                        {
                            // [로그 확인용]
                            // Debug.Log($"Touch Input: {inputData.Number}");

                            MatchResult result = _matchEngine.SubmitInput(inputData);

                            if (result == MatchResult.Success)
                            {
                                _view.PlayTouchEffect(inputData.WorldPos);
                            }
                            else if (result == MatchResult.StageClear)
                            {
                                stageClear = true;
                            }
                            else if (result == MatchResult.Fail_Wrong)
                            {
                                Debug.Log("Wrong Touch!");
                                isGameOver = true; // 오답 시 즉시 사망
                                stageClear = true;
                            }
                        }

                        // E. 프레임 대기 (필수: 루프가 너무 빨리 돌지 않게 함)
                        await Awaitable.NextFrameAsync(token);
                    }

                    // 스테이지 종료 후 처리
                    if (!isGameOver)
                    {
                        currentLevel++;
                    }
                    else
                    {
                        // 게임오버 시 잠시 대기 후 로비로
                        await Awaitable.WaitForSecondsAsync(1.0f, token);
                    }
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
            // GPGS & AdMob Init (나중에 활성화)
            await Awaitable.WaitForSecondsAsync(1.0f, token);
        }
    }
}
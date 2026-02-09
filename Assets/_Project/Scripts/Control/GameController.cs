using UnityEngine;
using UniRx;
using TouchIT.Boundary;
using TouchIT.Entity;
using System.Collections.Generic;
using System;

namespace TouchIT.Control
{
    public class GameController : IDisposable
    {
        private readonly IMainView _mainView;
        private readonly IInputAnalyzer _inputAnalyzer;
        private readonly AudioManager _audioManager;

        // 🚨 [수정] 구체적인 NoteSpawnService 대신 인터페이스 사용
        private readonly ISpawnService _ringSpawner;
        private readonly ISpawnService _osuSpawner;

        // 현재 활성화된 스포너
        private ISpawnService _currentSpawner;

        private readonly FireService _fireService;
        private readonly SaveDataService _saveService;
        private readonly AdManager _adManager;
        private readonly VFXService _vfxService;
        private readonly List<MusicData> _albumList;

        private int _currentAlbumIndex = 0;
        private GameState _currentState;
        private bool _isPreviewPlaying = false;
        private bool _isRevivedOnce = false;
        private GamePhase _currentPhase = GamePhase.RingMode;
        private IDisposable _osuTimerDisposable; // 오수 모드 지속 시간 타이머

        private bool _isOsuReady = false;      // 불 꽉 참 (진입 대기)
        private bool _isOsuEnding = false;     // 오수 모드 끝남 (복귀 대기)
        private bool _isOsuModeEnding = false;
        private CompositeDisposable _disposables = new CompositeDisposable();

        // 생성자
        public GameController(
             IMainView view,
             IInputAnalyzer input,
             AudioManager audio,
             ISpawnService ringSpawner, // ✅ 인터페이스 주입
             ISpawnService osuSpawner,  // ✅ 인터페이스 주입
             FireService fireService,
             SaveDataService saveService,
             AdManager adManager,
             VFXService vfxService,
             List<MusicData> albums)
        {
            _mainView = view;
            _inputAnalyzer = input;
            _audioManager = audio;

            _ringSpawner = ringSpawner;
            _osuSpawner = osuSpawner;

            _fireService = fireService;
            _saveService = saveService;
            _adManager = adManager;
            _vfxService = vfxService;
            _albumList = albums;

            // 기본 스포너 설정
            _currentSpawner = _ringSpawner;

            _currentState = GameState.MainMenu;
            _mainView.Initialize();

            RefreshAlbumVisual();
            BindInputs();
            BindGameEvents();

            Debug.Log("🎮 Game Ready: Dual Spawn System Loaded");
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        // ✅ [추가됨] Bootstrapper에서 호출할 Update 메서드
        public void OnUpdate()
        {
            // 스포너 업데이트 (노트 생성/이동 등)
            _currentSpawner?.OnUpdate();

            // 💥 [핵심 수정] 오수 모드일 때는 매 프레임 충돌 검사 수행! (드래그 중 충돌)
            if (_currentState == GameState.InGame && _currentPhase == GamePhase.OsuMode)
            {
                // 지속적으로 충돌 체크
                var hitNote = _osuSpawner.CheckHitAndGetNote();

                if (hitNote != null)
                {
                    // 충돌 성공 (파괴됨)
                    _fireService.AddFuel();
                    _vfxService.PlayHitEffect(hitNote.Transform.position);

                    // 타격감 연출
                    float punchScale = 0.5f + _fireService.CurrentFireSize;
                    _mainView.OnNoteHitSuccess(punchScale);
                }
            }
        }
        private void BindInputs()
        {
            // 1. 핀치 입력 (확대/축소)
            _inputAnalyzer.OnPinch
                .ThrottleFirst(System.TimeSpan.FromSeconds(0.1f))
                .Subscribe(delta =>
                {
                    // 🔒 [1차 잠금] 뷰가 연출 중이거나, 로직이 전환 중이면 무시
                    if (_mainView.IsTransitioning || _currentPhase == GamePhase.Transitioning) return;

                    // CASE A: 링 모드 -> 오수 모드 (조건: 불 꽉 참 + 확대)
                    if (_currentPhase == GamePhase.RingMode && _isOsuReady)
                    {
                        if (delta > 0.02f)
                        {
                            EnterOsuModeLogic(); // 진입!
                            return;
                        }
                    }

                    // CASE B: 오수 모드 -> 링 모드 (조건: 시간 다 됨 + 축소 입력)
                    // 🚨 _isOsuEnding이 true일 때만 작동하므로, 타이머 전에는 절대 작동 안 함
                    if (_currentPhase == GamePhase.OsuMode && _isOsuEnding)
                    {
                        if (delta < -0.02f) // 축소 제스처
                        {
                            Debug.Log("🤏 Pinch In Detected! Exiting Osu Mode.");
                            ExitOsuModeLogic(); // 여기서만 복귀 로직 실행
                            return;
                        }
                    }

                    // CASE C: 그 외 일반적인 상황 (UI 조작 등)
                    HandleImmediatePinch(delta);
                })
                .AddTo(_disposables);
            // 2. 탭 입력 수정
            _inputAnalyzer.OnTap
                .Subscribe(_ =>
                {
                    if (_mainView.IsTransitioning || _currentPhase == GamePhase.Transitioning) return;

                    if (_currentState == GameState.StageSelect) TogglePreview();
                    else if (_currentState == GameState.InGame)
                    {
                        // 현재 스포너(링 or 오수)에게 판정 위임
                        INoteView hitNote = _currentSpawner.CheckHitAndGetNote();
                        if (hitNote != null)
                        {
                            _fireService.AddFuel();
                            _vfxService.PlayHitEffect(hitNote.Transform.position);

                            // 타격감 (링 모드일 때만 링 반응 등)
                            float punchScale = 0.5f + _fireService.CurrentFireSize;
                            _mainView.OnNoteHitSuccess(punchScale);
                        }
                    }
                })
                .AddTo(_disposables);


            // 3. 드래그 (앨범 넘기기 - Delta 사용)
            _inputAnalyzer.OnDrag
                .Where(_ => _currentState == GameState.StageSelect && !_isPreviewPlaying)
                .Subscribe(delta =>
                {
                    if (_mainView.IsTransitioning) return;
                    if (delta.x < -5f) NextAlbum();
                    else if (delta.x > 5f) PrevAlbum();
                })
                .AddTo(_disposables);

            // 4. ✅ [수정] 오수 모드 이동 (절대 좌표 - Position 사용)
            // InputAnalyzer에 새로 만든 OnDragPos를 구독합니다.
            _inputAnalyzer.OnDragPos // (인터페이스에 추가 필요)
                .Where(_ => _currentState == GameState.InGame && _currentPhase == GamePhase.OsuMode && !_isOsuEnding)
                .Subscribe(screenPos =>
                {
                    _mainView.OnDragStart(); // "나 잡았다" 신호
                    _mainView.MoveSphereDirectly(screenPos); // 1:1 이동 명령
                })
                .AddTo(_disposables);

            // 5. 드래그 끝 (손 뗌) -> 복귀 시작
            _inputAnalyzer.OnDragEnd.Subscribe(_ => _mainView.OnDragEnd()).AddTo(_disposables);
        }
        // ⚔️ 오수 모드 진입 로직
        private void EnterOsuModeLogic()
        {
            Debug.Log("⚔️ [System] Entering Osu Mode...");

            // 1. 상태 잠금 (중복 실행 방지)
            _currentPhase = GamePhase.Transitioning;
            _isOsuReady = false;

            // 2. 기존(링) 시스템 정지
            _ringSpawner.Stop();
            _mainView.ShowRing(false); // 링 즉시 숨김

            // 3. 뷰 전환 연출 (White Out)
            _mainView.AnimateEnterOsuMode();

            // 4. 로직 교체 (잠시 대기 후 실행 - 연출 싱크 맞춤)
            Observable.Timer(TimeSpan.FromSeconds(0.5f)) // 연출 시간만큼 대기
                .Subscribe(_ =>
                {
                    _currentPhase = GamePhase.OsuMode; // 상태 변경

                    _currentSpawner = _osuSpawner;
                    _currentSpawner.LoadPattern(_albumList[_currentAlbumIndex]); // 오수 패턴 시작

                    StartOsuTimer(); // 15초 카운트다운 시작
                })
                .AddTo(_disposables);
        }
        // ⏳ 오수 모드 타이머
        private void StartOsuTimer()
        {
            _isOsuEnding = false;
            _osuTimerDisposable?.Dispose();

            // 15초(예시) 뒤 복귀 준비
            _osuTimerDisposable = Observable.Timer(TimeSpan.FromSeconds(15.0f))
                .Subscribe(_ =>
                {
                    if (_currentPhase != GamePhase.OsuMode) return;

                    Debug.Log("🕳️ [System] Time Up! Waiting for Pinch In...");

                    // 🚨 여기서 바로 Exit 로직을 타거나 화면을 확 바꾸면 안 됨!
                    _isOsuEnding = true; // 이제부터 핀치 인 허용

                    // 뷰에게 "이제 줄일 수 있어"라는 신호만 보냄 (예: 구체가 붉게 깜빡임 or UI 표시)
                    _mainView.AnimatePortalClosingReady();
                })
                .AddTo(_disposables);
        }
        // 🌌 링 모드 복귀 로직
        private void ExitOsuModeLogic()
        {
            Debug.Log("🌌 [System] Collapsing to Ring Mode...");

            // 1. 상태 잠금
            _currentPhase = GamePhase.Transitioning;
            _isOsuEnding = false;
            _osuTimerDisposable?.Dispose(); // 타이머 정리

            // 2. 오수 시스템 정지
            _osuSpawner.Stop();

            // 3. 뷰 전환 (Implosion)
            // _mainView.AnimateExitOsuMode(); // 구현하신 함수

            // 4. 로직 복구
            Observable.Timer(TimeSpan.FromSeconds(0.5f))
                .Subscribe(_ =>
                {
                    _currentPhase = GamePhase.RingMode;

                    _mainView.ShowRing(true); // 링 다시 보이기
                    _currentSpawner = _ringSpawner;

                    // 링 모드는 노래 시간에 맞춰서 계속 진행 중이었으므로 그냥 Resume/Load
                    // (NoteSpawnService는 Time 기반이라 끊기지 않음)
                    _currentSpawner.LoadPattern(_albumList[_currentAlbumIndex]);
                })
                .AddTo(_disposables);
        }
       
        private void StartGame()
        {
            _currentState = GameState.InGame;
            _isPreviewPlaying = false;
            _isRevivedOnce = false;
            _isOsuReady = false;

            _mainView.AnimateGameStart();

            var currentData = _albumList[_currentAlbumIndex];
            _audioManager.PlayBGM(currentData.Clip);

            _fireService.SetupGame(currentData.Notes.Count);

            // ✅ 게임 시작 시엔 링 모드로 초기화
            _currentSpawner = _ringSpawner;
            _currentSpawner.LoadPattern(currentData);

            Debug.Log("🚀 GAME START!");
        }

        private void BackToMainMenu()
        {
            _currentState = GameState.MainMenu;

            // ✅ 스포너 정지
            _currentSpawner.Stop();
            _audioManager.StopBGM();

            _mainView.AnimateStageToMain(Color.clear);
            Debug.Log("🔄 Back to Main Menu");
        }

        // ... (나머지 HandleGameOver, Revive, TogglePreview 등은 기존 유지) ...

        // (지면 관계상 생략된 메서드들은 기존 코드 그대로 두세요)
        private void BindGameEvents()
        {
            _fireService.OnFireExtinguished += HandleGameOver;

            _fireService.OnFireFull += () =>
            {
                // 이미 오수 모드거나 전환 중이면 무시
                if (_currentPhase != GamePhase.RingMode || _isOsuReady) return;

                _isOsuReady = true;
                Debug.Log("🔥 [System] Fire Max! Ready for Transition.");

                _mainView.AnimateOsuReady(); // 꿀렁임
                // (여기서 시간 초과 게임오버 타이머를 걸 수도 있음)
            };
        }
        private void HandleImmediatePinch(float delta)
        {
            if (_currentState == GameState.MainMenu && delta > 0.01f) GoToStageSelect();
            else if (_currentState == GameState.StageSelect)
            {
                if (_isPreviewPlaying && delta > 0.01f) StartGame();
                else if (!_isPreviewPlaying && delta < -0.01f) BackToMainMenu();
            }
        }
        private void GoToStageSelect()
        {
            _currentState = GameState.StageSelect;
            _mainView.AnimateMainToStage(Color.clear);
        }
        private void RefreshAlbumVisual()
        {
            var data = _albumList[_currentAlbumIndex];
            float best = _saveService.GetBestRate(data.Title);
            _mainView.UpdateAlbumVisual(data);
        }
        private void TogglePreview()
        {
            _isPreviewPlaying = !_isPreviewPlaying;
            _mainView.AnimatePreviewMode(_isPreviewPlaying);
            if (_isPreviewPlaying) _audioManager.PlayBGM(_albumList[_currentAlbumIndex].Clip);
            else _audioManager.StopBGM();
        }
        private void HandleGameOver()
        {
            _currentSpawner.Stop(); // Stop Current
            _audioManager.StopBGM();
            if (!_isRevivedOnce) Debug.Log("💀 Show Revive"); // 임시
            else GiveUpGame();
        }
        public void OnUserSelectRevive()
        {
            _adManager.ShowRewardAd(() => {
                _isRevivedOnce = true;
                _fireService.Revive();
                // _currentSpawner.Resume(); // 인터페이스에 Resume 추가 필요하거나 캐스팅 필요
                _audioManager.ResumeBGM();
            }, null);
        }
        public void GiveUpGame()
        {
            _saveService.SaveRecord(_albumList[_currentAlbumIndex].Title, _fireService.CompletionRate);
            _adManager.CheckAndShowInterstitial();
            BackToMainMenu();
        }
        private void NextAlbum()
        {
            if (_albumList.Count == 0) return;
            _currentAlbumIndex = (_currentAlbumIndex + 1) % _albumList.Count;
            ChangeAlbum();
        }
        private void PrevAlbum()
        {
            if (_albumList.Count == 0) return;
            _currentAlbumIndex--;
            if (_currentAlbumIndex < 0) _currentAlbumIndex = _albumList.Count - 1;
            ChangeAlbum();
        }
        private void ChangeAlbum()
        {
            RefreshAlbumVisual();
        }
    }
}
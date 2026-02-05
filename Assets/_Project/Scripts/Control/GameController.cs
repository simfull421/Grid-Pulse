using UnityEngine;
using UniRx;
using TouchIT.Boundary;
using TouchIT.Entity;
using System.Collections.Generic;
using System; // IDisposable

namespace TouchIT.Control
{
    // MonoBehaviour 제거, IDisposable 추가 (메모리 정리용)
    public class GameController : IDisposable
    {
        private readonly IMainView _mainView;
        private readonly IInputAnalyzer _inputAnalyzer;
        private readonly AudioManager _audioManager;
        private readonly NoteSpawnService _spawnService;
        private readonly List<MusicData> _albumList;

        // 추가된 서비스들
        private readonly FireService _fireService;
        private readonly SaveDataService _saveService;
        private readonly AdManager _adManager;

        private int _currentAlbumIndex = 0;
        private GameState _currentState;
        private bool _isPreviewPlaying = false;
        private bool _isRevivedOnce = false; // 한 판에 부활은 1번만
        private CompositeDisposable _disposables = new CompositeDisposable();
        // 생성자 주입 (Constructor Injection)
        public GameController(
             IMainView view,
             IInputAnalyzer input,
             AudioManager audio,
             NoteSpawnService spawnService,
             FireService fireService,     // 추가
             SaveDataService saveService, // 추가
             AdManager adManager,         // 추가
             List<MusicData> albums)
        {
            _mainView = view;
            _inputAnalyzer = input;
            _audioManager = audio;
            _spawnService = spawnService;
            _fireService = fireService;
            _saveService = saveService;
            _adManager = adManager;
            _albumList = albums;

            _currentState = GameState.MainMenu;
            _mainView.Initialize();

            // 초기 앨범 데이터 로드 (저장된 달성률 표시용)
            RefreshAlbumVisual();

            BindInputs();
            BindGameEvents(); // 게임 이벤트 연결

            Debug.Log("🎮 Game Ready: Fire & Save System Loaded");
        }

        // 메모리 해제 (게임 종료 시 호출)
        public void Dispose()
        {
            _disposables.Dispose();
        }

        private void BindInputs()
        {
            // 1. 핀치/휠 입력 (즉시 반응 로직)
            _inputAnalyzer.OnPinch
                .ThrottleFirst(System.TimeSpan.FromSeconds(0.5f))
                .Subscribe(delta => HandleImmediatePinch(delta))
                .AddTo(_disposables); // .AddTo(this) 대신 _disposables 사용

            // 2. 탭 (재생/일시정지)
            _inputAnalyzer.OnTap
                .Where(_ => _currentState == GameState.StageSelect)
                .Subscribe(_ => TogglePreview())
                .AddTo(_disposables);

            // 3. 드래그 (앨범 교체)
            _inputAnalyzer.OnDrag
                .Where(_ => _currentState == GameState.StageSelect && !_isPreviewPlaying)
                .ThrottleFirst(System.TimeSpan.FromSeconds(0.2f))
                .Subscribe(delta =>
                {
                    if (delta.x < -20f) NextAlbum();
                    else if (delta.x > 20f) PrevAlbum();
                })
                .AddTo(_disposables);
        }
        private void BindGameEvents()
        {
            // 🔥 불 꺼짐 (게임 오버) 감지
            _fireService.OnFireExtinguished += HandleGameOver;

            // 🔥 불 꽉 참 (Osu 모드 준비) 감지
            _fireService.OnFireFull += () => {
                Debug.Log("🔥 Fire Max! Ready for Osu Mode (Zoom Effect)");
                // TODO: 뷰에게 껌뻑껌뻑 효과 명령
            };
        }
        private void HandleImmediatePinch(float delta)
        {
            if (_currentState == GameState.MainMenu)
            {
                if (delta > 0.01f) GoToStageSelect();
            }
            else if (_currentState == GameState.StageSelect)
            {
                if (_isPreviewPlaying && delta > 0.01f) StartGame();
                else if (!_isPreviewPlaying && delta < -0.01f) BackToMainMenu();
            }
        }

        private void GoToStageSelect()
        {
            _currentState = GameState.StageSelect;
            _mainView.CommitTransition(true);
            Debug.Log("🔄 Commit: Enter Stage Select");
        }

        private void BackToMainMenu()
        {
            _currentState = GameState.MainMenu; // 사실상 StageSelect 화면으로 가는 것

            // 뷰: 배경에서 다시 구체로 복귀 + 재생성 연출
            _mainView.CommitTransition(false);

            // 앨범 정보 갱신 (달성률 표시 등)
            RefreshAlbumVisual();

            _spawnService.Stop();
            _audioManager.StopBGM();
        }
        private void RefreshAlbumVisual()
        {
            var data = _albumList[_currentAlbumIndex];

            // 저장된 최고 기록 가져오기
            float bestRate = _saveService.GetBestRate(data.Title);

            // 뷰 업데이트 (달성률 텍스트 같은게 있다면 여기서 전달)
            _mainView.UpdateAlbumVisual(data); // + bestRate 전달

            Debug.Log($"💿 Album: {data.Title} | Best: {bestRate:F1}%");
        }
        private void TogglePreview()
        {
            _isPreviewPlaying = !_isPreviewPlaying;
            _mainView.AnimatePreviewMode(_isPreviewPlaying);

            if (_isPreviewPlaying)
            {
                var clip = _albumList[_currentAlbumIndex].Clip;
                Debug.Log($"🎵 Play Preview: {_albumList[_currentAlbumIndex].Title}");
                _audioManager.PlayBGM(clip);
            }
            else
            {
                Debug.Log("⏸ Pause Preview");
                _audioManager.StopBGM();
            }
        }

        private void StartGame()
        {
            _currentState = GameState.InGame;
            _isPreviewPlaying = false;
            _isRevivedOnce = false; // 부활 기회 초기화

            _mainView.AnimateGameStart();

            var currentData = _albumList[_currentAlbumIndex];
            _audioManager.PlayBGM(currentData.Clip);

            // 불씨 초기화
            _fireService.SetupGame(currentData.Notes.Count);

            // 노트 스폰 시작
            _spawnService.LoadPattern(currentData);

            Debug.Log("🚀 GAME START!");
        }
        private void HandleGameOver()
        {
            _spawnService.Stop();
            _audioManager.StopBGM();

            // 구체 파괴 이펙트 (뷰 호출)
            // _mainView.PlayExplosionEffect(); 

            // 부활 기회가 남았는가?
            if (!_isRevivedOnce)
            {
                // UI 띄우기: "광고 보고 부활할래?" (간단한 GUI나 다이에제틱 UI)
                // 여기서는 예시로 바로 팝업 로직 호출한다고 가정
                // 실제로는 뷰에 버튼을 띄우고 입력을 기다려야 함.
                Debug.Log("💀 Game Over. Show Revive Popup.");

                // (임시) 자동 포기 처리 혹은 UI 콜백 대기
                // ShowReviveUI(); 
            }
            else
            {
                // 이미 부활했으면 얄짤없이 종료
                GiveUpGame();
            }
        }
        // UI에서 '광고 보고 부활' 선택 시 호출
        public void OnUserSelectRevive()
        {
            _adManager.ShowRewardAd(
                onRewarded: () => {
                    _isRevivedOnce = true;
                    _fireService.Revive(); // 불씨 살리기
                    _spawnService.Resume(); // 노트 다시 진행 (구현 필요)
                    _audioManager.ResumeBGM(); // 음악 다시 재생
                    Debug.Log("😇 Revived!");
                },
                onFailed: () => {
                    // 광고 로드 실패 시 -> 그냥 부활시켜주기 (우아한 실패)
                    _isRevivedOnce = true;
                    _fireService.Revive();
                    _spawnService.Resume();
                    _audioManager.ResumeBGM();
                }
            );
        }
        // UI에서 '포기' 선택 시 호출
        public void GiveUpGame()
        {
            Debug.Log("🏳️ Give Up.");

            // 달성률 저장 (죽었더라도 기록은 남김)
            float rate = _fireService.CompletionRate;
            _saveService.SaveRecord(_albumList[_currentAlbumIndex].Title, rate);

            // 전면 광고 체크 (3판마다 1번)
            _adManager.CheckAndShowInterstitial();

            // 메인으로 복귀 (구체 재생성 연출 포함)
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
            var data = _albumList[_currentAlbumIndex];
            _mainView.UpdateAlbumVisual(data);
            Debug.Log($"💿 Album Changed: {data.Title}");
        }
    }
}
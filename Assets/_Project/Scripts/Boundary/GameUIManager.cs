using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 필수
using ReflexPuzzle.Control;
using ReflexPuzzle.Entity;
using System.Threading;

namespace ReflexPuzzle.Boundary
{
    public class GameUIManager : MonoBehaviour, IGameUI
    {
        [Header("Panels")]
        [SerializeField] private GameObject _titlePanel;
        [SerializeField] private GameObject _lobbyPanel;
        [SerializeField] private GameObject _gamePanel;

        [Header("Lobby Elements")]
        [SerializeField] private TextMeshProUGUI _modeTitleText;
        [SerializeField] private TextMeshProUGUI _modeDescText;
        [SerializeField] private RectTransform[] _modeCards;

        // [추가됨] 게임 UI 요소 (시간, 레벨)
        [Header("Game Elements")]
        [SerializeField] private TextMeshProUGUI _timeText;
        [SerializeField] private TextMeshProUGUI _levelText;

        private GameMode? _selectedMode = null;
        private bool _isConfirmed = false;

        private void Awake()
        {
            if (_titlePanel) _titlePanel.SetActive(false);
            if (_lobbyPanel) _lobbyPanel.SetActive(false);
            if (_gamePanel) _gamePanel.SetActive(false);
        }

        public void ShowTitle()
        {
            _titlePanel.SetActive(true);
            _lobbyPanel.SetActive(false);
            _gamePanel.SetActive(false);
        }

        public void ShowLobby()
        {
            _titlePanel.SetActive(false);
            _lobbyPanel.SetActive(true);
            _gamePanel.SetActive(false);

            _selectedMode = null;
            _isConfirmed = false;
            ResetCards();
            UpdateModeDescription("", "모드 카드를 터치하여 선택하세요.");
        }

        public void ShowGameUI()
        {
            _titlePanel.SetActive(false);
            _lobbyPanel.SetActive(false);
            _gamePanel.SetActive(true);

            // 시작 시 초기화
            UpdateGameStatus(30f, 1);
        }

        public void OnModeCardClicked(int modeIndex)
        {
            GameMode clicked = (GameMode)modeIndex;

            if (_selectedMode == clicked)
            {
                _isConfirmed = true;
                return;
            }

            _selectedMode = clicked;
            _isConfirmed = false;

            for (int i = 0; i < _modeCards.Length; i++)
            {
                float targetScale = (i == modeIndex) ? 1.2f : 0.9f;
                _modeCards[i].localScale = Vector3.one * targetScale;
            }

            UpdateModeDescription(clicked.ToString(), GetModeDescription(clicked));
        }

        public async Awaitable<GameMode> WaitForModeSelectionAsync(CancellationToken token)
        {
            while (!_isConfirmed && !token.IsCancellationRequested)
            {
                await Awaitable.NextFrameAsync(token);
            }
            return _selectedMode.Value;
        }

        private void ResetCards()
        {
            foreach (var card in _modeCards) card.localScale = Vector3.one;
        }

        public void UpdateModeDescription(string title, string desc)
        {
            if (_modeTitleText) _modeTitleText.text = title;
            if (_modeDescText) _modeDescText.text = desc;
        }

        // [에러 해결] 인터페이스 구현: 시간과 레벨 갱신
        public void UpdateGameStatus(float time, int level)
        {
            if (_timeText != null)
            {
                _timeText.text = $"{time:F2}"; // 30.00 형식
                _timeText.color = (time <= 5.0f) ? Color.red : Color.white; // 5초 남으면 빨강
            }

            if (_levelText != null)
            {
                _levelText.text = $"Lv.{level}";
            }
        }

        private string GetModeDescription(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.Classic: return "1부터 순서대로! 가장 기본적인 모드입니다.";
                case GameMode.Color: return "같은 색깔끼리 묶어서 터치하세요.";
                case GameMode.Mixed: return "빨간색 함정을 피해 순서대로 누르세요!";
                case GameMode.Memory: return "숫자가 사라집니다. 위치를 기억하세요!";
                default: return "";
            }
        }
    }
}
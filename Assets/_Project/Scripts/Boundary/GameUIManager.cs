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
        [SerializeField] private GameObject _lobbyPanel;
        [SerializeField] private GameObject _gamePanel;

        [Header("Lobby UI Elements")]
        [SerializeField] private TextMeshProUGUI _modeTitleText; // 모드 이름
        [SerializeField] private TextMeshProUGUI _modeDescText;  // 설명
        [SerializeField] private RectTransform[] _modeCards;     // 슬라이드 카드들

        // 내부 상태
        private GameMode? _clickedMode = null; // 유저가 클릭한 모드

        public void ShowLobby()
        {
            _lobbyPanel.SetActive(true);
            _gamePanel.SetActive(false);
            _clickedMode = null;
            UpdateModeDescription("", "카드를 터치하여 모드를 선택하세요.");
        }

        public void ShowGameUI()
        {
            _lobbyPanel.SetActive(false);
            _gamePanel.SetActive(true);
        }

        public void UpdateModeDescription(string title, string desc)
        {
            if (_modeTitleText) _modeTitleText.text = title;
            if (_modeDescText) _modeDescText.text = desc;
        }

        // 버튼(카드) 클릭 시 호출 (인스펙터 연결)
        public void OnModeCardClicked(int modeIndex)
        {
            // 1. 선택된 모드 저장
            _clickedMode = (GameMode)modeIndex;

            // 2. 설명 업데이트
            string title = _clickedMode.ToString();
            string desc = "";
            switch (_clickedMode)
            {
                case GameMode.Classic: desc = "1부터 순서대로 빠르게 터치하세요."; break;
                case GameMode.Color: desc = "같은 색상끼리 그룹지어 터치하세요."; break;
                case GameMode.Mixed: desc = "빨간색 함정을 피해서 순서대로 누르세요."; break;
                case GameMode.Memory: desc = "사라진 숫자의 위치를 기억하세요."; break;
            }
            UpdateModeDescription(title, desc);

            // 3. 카드 확대 연출 (간단히)
            for (int i = 0; i < _modeCards.Length; i++)
            {
                if (i == modeIndex) _modeCards[i].localScale = Vector3.one * 1.2f; // 선택된 놈 커짐
                else _modeCards[i].localScale = Vector3.one; // 나머지 원래대로
            }
        }

        // GameMain이 호출: 유저가 "확정"할 때까지 대기
        public async Awaitable<GameMode> WaitForModeSelectionAsync(CancellationToken token)
        {
            // 여기서는 "두 번 터치" 로직을 간단히 구현
            // 1. 아무것도 선택 안 된 상태에서 대기
            while (_clickedMode == null && !token.IsCancellationRequested)
            {
                await Awaitable.NextFrameAsync(token);
            }

            // 2. 선택된 상태. 여기서 바로 리턴하면 "원터치 시작"
            // "투터치 시작"을 원하면, 여기서 _isStartButtonClicked 같은 플래그를 한 번 더 기다리면 됨.

            // 현재는 카드 누르면 바로 시작 (유저 경험상 이게 더 빠름)
            // 하이브리드(확대 후 한번 더 터치)를 원하면 별도 START 버튼을 만들어서 그게 눌릴 때까지 기다려야 함.

            return _clickedMode.Value;
        }
    }
}
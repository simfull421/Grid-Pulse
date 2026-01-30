using UnityEngine;
using ReflexPuzzle.Control;

namespace ReflexPuzzle.Boundary
{
    public class GameBinder : MonoBehaviour
    {
        // Boundary의 구체적인 구현체들을 인스펙터에 연결
        [Header("Implementations (Boundary)")]
        [SerializeField] private GridView _gridView;
        [SerializeField] private InputReader _inputReader;
        [SerializeField] private GameUIManager _uiManager;

        // Control의 핵심 로직
        [Header("Core (Control)")]
        [SerializeField] private GameMain _gameMain;

        private void Awake()
        {
            // [의존성 주입]
            // Control(GameMain)은 인터페이스만 알고, 
            // 여기서 실제 구현체(Boundary)를 밀어넣어줌.
            _gameMain.Initialize(_gridView, _inputReader, _uiManager);
        }

        private void Start()
        {
            // 조립이 끝났으니 게임 시작
            _gameMain.RunGameLoop();
        }
    }
}
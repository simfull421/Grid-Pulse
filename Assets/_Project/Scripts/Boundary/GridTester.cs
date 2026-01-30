using UnityEngine;
using ReflexPuzzle.Control;
using ReflexPuzzle.Entity;

namespace ReflexPuzzle.Boundary
{
    public class GridTester : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridView _gridView;

        [Header("Test Parameters")]
        [SerializeField] private int _startLevel = 1;
        [SerializeField] private GameMode _testMode = GameMode.Classic;

        private GridGenerator _generator;
        private int _currentLevel;
        private bool _isInputLocked = false; // 애니메이션 중 입력 방지용

        private void Start()
        {
            _generator = new GridGenerator();
            _currentLevel = _startLevel;

            // 초기 스테이지 생성 (애니메이션 없이 바로 배치)
            StageInfo firstStage = _generator.CreateStage(_currentLevel, _testMode);
            _gridView.BuildGrid(firstStage);
        }

        private void Update()
        {
            // 스페이스바: 다음 스테이지로 백덤블링 전환
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // 입력 잠금 (원한다면 연타 허용을 위해 제거 가능)
                // if (_isInputLocked) return; 

                NextStage();
            }
        }

        private void NextStage()
        {
            _currentLevel++;
            Debug.Log($"Generating Level {_currentLevel}...");

            // 1. 다음 스테이지 데이터 생성
            StageInfo nextStage = _generator.CreateStage(_currentLevel, _testMode);

            // 2. 애니메이션 실행 요청
            _isInputLocked = true;
            _gridView.TriggerRefresh(nextStage, () =>
            {
                Debug.Log("Transition Complete! Input Unlocked.");
                _isInputLocked = false;
            });
        }
    }
}
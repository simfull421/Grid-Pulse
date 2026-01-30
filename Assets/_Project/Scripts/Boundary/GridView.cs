using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ReflexPuzzle.Entity;
using ReflexPuzzle.Control;

namespace ReflexPuzzle.Boundary
{
    public class GridView : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private CellView _cellPrefab;
        [SerializeField] private Transform _gridContainer;
        [SerializeField] private float _cellSize = 1.1f;
        [SerializeField] private int _maxPoolSize = 25;

        [Header("Theme")]
        [SerializeField] private ThemeData _currentTheme;

        // ECS 스타일: 뷰 객체 풀
        private List<CellView> _cellPool = new List<CellView>();

        // 애니메이션 상태 관리
        private Coroutine _animationRoutine;

        private void Awake()
        {
            InitializePool();
        }

        private void InitializePool()
        {
            for (int i = 0; i < _maxPoolSize; i++)
            {
                CellView cell = Instantiate(_cellPrefab, _gridContainer);
                cell.gameObject.SetActive(false);
                _cellPool.Add(cell);
            }
        }

        public void BuildGrid(StageInfo stage)
        {
            // [중요] 배치 전 컨테이너 정렬 상태 강제 초기화 (드리프트 방지)
            if (_animationRoutine != null) StopCoroutine(_animationRoutine);
            _gridContainer.localRotation = Quaternion.identity;

            float offset = (stage.GridSize - 1) * 0.5f;

            if (Camera.main != null && _currentTheme != null)
                Camera.main.backgroundColor = _currentTheme.BackgroundColor;

            int neededCount = stage.GridSize * stage.GridSize;

            for (int i = 0; i < _maxPoolSize; i++)
            {
                CellView cell = _cellPool[i];

                if (i < neededCount)
                {
                    cell.gameObject.SetActive(true);

                    // [풀링 초기화] 트랜스폼 오염 제거
                    cell.transform.localRotation = Quaternion.identity;
                    cell.transform.localScale = Vector3.one;

                    // 위치 잡기
                    int x = i % stage.GridSize;
                    int y = i / stage.GridSize;
                    Vector3 position = new Vector3((x - offset) * _cellSize, (y - offset) * _cellSize, 0);
                    cell.transform.localPosition = position;

                    // 데이터 주입
                    if (i < stage.Cells.Count)
                    {
                        cell.Initialize(stage.Cells[i], _currentTheme);
                    }
                }
                else
                {
                    cell.gameObject.SetActive(false);
                }
            }
        }

        // 완전한 360도 백덤블링 트랜지션
        public void TriggerRefresh(StageInfo nextStage, System.Action onComplete)
        {
            // 기존 애니메이션 즉시 중단 (연타 대응)
            if (_animationRoutine != null) StopCoroutine(_animationRoutine);
            _animationRoutine = StartCoroutine(FlipRoutine(nextStage, onComplete));
        }

        private IEnumerator FlipRoutine(StageInfo nextStage, System.Action onComplete)
        {
            // 1. 시작: 0도 보장
            _gridContainer.localRotation = Quaternion.identity;

            float duration = 0.4f;
            float halfDuration = duration * 0.5f;

            // Phase 1: 0도 -> 90도 (뒤로 눕기)
            Quaternion startRot = Quaternion.identity;
            Quaternion midRot = Quaternion.Euler(90f, 0f, 0f);

            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                t = t * t; // EaseIn
                _gridContainer.localRotation = Quaternion.Lerp(startRot, midRot, t);
                yield return null;
            }
            _gridContainer.localRotation = midRot;

            // [데이터 교체] - 판이 수직이라 안 보일 때 교체
            BuildGrid(nextStage);

            // Phase 2: -90도(270도) -> 0도 (아래에서 올라오기)
            Quaternion bottomRot = Quaternion.Euler(-90f, 0f, 0f);

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                t = 1f - Mathf.Pow(1f - t, 3); // EaseOut
                _gridContainer.localRotation = Quaternion.Lerp(bottomRot, startRot, t);
                yield return null;
            }

            // [중요] 종료 시 오차 제거
            _gridContainer.localRotation = Quaternion.identity;

            _animationRoutine = null;
            onComplete?.Invoke();
        }
    }
}
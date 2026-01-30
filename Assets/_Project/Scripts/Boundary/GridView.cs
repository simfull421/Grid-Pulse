using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ReflexPuzzle.Entity;
using ReflexPuzzle.Control;

namespace ReflexPuzzle.Boundary
{
    public class GridView : MonoBehaviour, IGridView
    {
        [Header("Settings")]
        [SerializeField] private CellView _cellPrefab;
        [SerializeField] private Transform _gridContainer;
        [SerializeField] private float _cellSize = 1.1f;
        [SerializeField] private int _maxPoolSize = 25;

        [Header("Theme")]
        [SerializeField] private ThemeData _currentTheme;

        [Header("VFX Settings")]
        [SerializeField] private ParticleSystem _touchEffectPrefab; // 파티클 프리팹
        [SerializeField] private Transform _vfxContainer;           // 파티클 모아둘 부모
        [SerializeField] private int _maxVfxPoolSize = 20;          // 20개 제한

        // 파티클 풀 리스트
        private List<ParticleSystem> _vfxPool = new List<ParticleSystem>();

        // ECS 스타일: 뷰 객체 풀
        private List<CellView> _cellPool = new List<CellView>();

        // 애니메이션 상태 관리
        private Coroutine _animationRoutine;

        private void Awake()
        {
            InitializePool();
            InitializeVfxPool(); // [추가]
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

        // 1. 파티클 20개 미리 생성
        private void InitializeVfxPool()
        {
            // 만약 컨테이너 안 넣었으면 GridContainer 자식으로 (혹은 새로 생성)
            if (_vfxContainer == null) _vfxContainer = transform;

            for (int i = 0; i < _maxVfxPoolSize; i++)
            {
                ParticleSystem vfx = Instantiate(_touchEffectPrefab, _vfxContainer);
                var main = vfx.main;
                main.stopAction = ParticleSystemStopAction.None; // 중요: Destroy 방지

                vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // 일단 정지
                vfx.gameObject.SetActive(false); // 꺼두기
                _vfxPool.Add(vfx);
            }
        }

        // 2. 인터페이스 구현: 이펙트 재생 (GC 0)
        public void PlayTouchEffect(Vector3 position)
        {
            ParticleSystem targetVfx = null;

            // 놀고 있는 놈 찾기 (isPlaying이 false인 녀석)
            for (int i = 0; i < _vfxPool.Count; i++)
            {
                if (!_vfxPool[i].isPlaying)
                {
                    targetVfx = _vfxPool[i];
                    break;
                }
            }

            // 만약 20개가 동시에 다 터지고 있다면? -> 가장 오래된 놈(0번) 강제 재사용
            if (targetVfx == null)
            {
                targetVfx = _vfxPool[0];
                // 리스트 맨 뒤로 보내서 순환구조 유지
                _vfxPool.RemoveAt(0);
                _vfxPool.Add(targetVfx);
            }

            // 위치 이동 및 재생
            targetVfx.gameObject.SetActive(true);
            targetVfx.transform.position = position;

            // z값을 앞으로 살짝 당겨서 타일 뒤에 가려지지 않게 함
            Vector3 pos = targetVfx.transform.position;
            pos.z = -1.0f;
            targetVfx.transform.position = pos;

            targetVfx.Play();
        }
        // [수정] 여기서 클래스를 닫아버리는 '}'를 삭제했습니다. 이제 아래 함수들도 클래스 멤버가 됩니다.

        // [에러 해결] 인터페이스 구현: 모든 셀 비활성화 (초기화)
        public void ClearGrid()
        {
            foreach (var cell in _cellPool)
            {
                if (cell != null) cell.gameObject.SetActive(false);
            }
            _gridContainer.localRotation = Quaternion.identity;
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
            if (_animationRoutine != null) StopCoroutine(_animationRoutine);
            _animationRoutine = StartCoroutine(FlipRoutine(nextStage, onComplete));
        }

        private IEnumerator FlipRoutine(StageInfo nextStage, System.Action onComplete)
        {
            _gridContainer.localRotation = Quaternion.identity;

            float duration = 0.4f;
            float halfDuration = duration * 0.5f;

            // Phase 1: 0도 -> 90도
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

            // [데이터 교체]
            BuildGrid(nextStage);

            // Phase 2: -90도 -> 0도
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

            _gridContainer.localRotation = Quaternion.identity;

            _animationRoutine = null;
            onComplete?.Invoke();
        }
    }
}
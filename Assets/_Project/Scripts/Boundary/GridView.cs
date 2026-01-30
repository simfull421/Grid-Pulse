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
        [SerializeField] private ParticleSystem _touchEffectPrefab;
        [SerializeField] private Transform _vfxContainer;
        [SerializeField] private int _maxVfxPoolSize = 20;

        private List<ParticleSystem> _vfxPool = new List<ParticleSystem>();
        private List<CellView> _cellPool = new List<CellView>();
        private Coroutine _animationRoutine;

        private void Awake()
        {
            InitializePool();
            InitializeVfxPool();
        }

        private void InitializePool()
        {
            // (기존 코드와 동일)
            for (int i = 0; i < _maxPoolSize; i++)
            {
                CellView cell = Instantiate(_cellPrefab, _gridContainer);
                cell.gameObject.SetActive(false);
                _cellPool.Add(cell);
            }
        }

        private void InitializeVfxPool()
        {
            // (기존 코드와 동일)
            if (_vfxContainer == null) _vfxContainer = transform;

            for (int i = 0; i < _maxVfxPoolSize; i++)
            {
                ParticleSystem vfx = Instantiate(_touchEffectPrefab, _vfxContainer);
                var main = vfx.main;
                main.stopAction = ParticleSystemStopAction.None;
                vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                vfx.gameObject.SetActive(false);
                _vfxPool.Add(vfx);
            }
        }

        public void PlayTouchEffect(Vector3 position)
        {
            // (기존 코드와 동일)
            ParticleSystem targetVfx = null;
            for (int i = 0; i < _vfxPool.Count; i++)
            {
                if (!_vfxPool[i].isPlaying) { targetVfx = _vfxPool[i]; break; }
            }
            if (targetVfx == null)
            {
                targetVfx = _vfxPool[0];
                _vfxPool.RemoveAt(0);
                _vfxPool.Add(targetVfx);
            }
            targetVfx.gameObject.SetActive(true);
            targetVfx.transform.position = position;
            Vector3 pos = targetVfx.transform.position;
            pos.z = -1.0f;
            targetVfx.transform.position = pos;
            targetVfx.Play();
        }

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
            // [삭제] 이 줄이 애니메이션을 죽이고 있었습니다! 지우세요.
            // if (_animationRoutine != null) StopCoroutine(_animationRoutine); 

            // [수정] 회전 초기화는 애니메이션 중이 아닐 때만 하는 게 안전하지만,
            // 지금 구조상 그냥 둬도 애니메이션 루프가 다시 덮어쓰므로 괜찮습니다.
            // 다만, 더 안전하게 하려면 아래처럼 조건부를 넣을 수 있습니다.

            // 애니메이션이 안 돌고 있을 때만 정렬 초기화
            if (_animationRoutine == null)
            {
                _gridContainer.localRotation = Quaternion.identity;
            }

            float offset = (stage.GridSize - 1) * 0.5f;

            if (Camera.main != null && _currentTheme != null)
                Camera.main.backgroundColor = _currentTheme.BackgroundColor;

            int neededCount = stage.GridSize * stage.GridSize;

            for (int i = 0; i < _maxPoolSize; i++)
            {
                // ... (이하 for문 내용은 기존과 동일) ...
                CellView cell = _cellPool[i];

                if (i < neededCount)
                {
                    cell.gameObject.SetActive(true);
                    cell.transform.localRotation = Quaternion.identity;
                    cell.transform.localScale = Vector3.one;

                    int x = i % stage.GridSize;
                    int y = i / stage.GridSize;
                    Vector3 position = new Vector3((x - offset) * _cellSize, (y - offset) * _cellSize, 0);
                    cell.transform.localPosition = position;

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

        public void TriggerRefresh(StageInfo nextStage, System.Action onComplete)
        {
            if (_animationRoutine != null) StopCoroutine(_animationRoutine);
            _animationRoutine = StartCoroutine(FlipRoutine(nextStage, onComplete));
        }

        // [핵심 수정] 360도 회전 및 안전장치 추가
        private IEnumerator FlipRoutine(StageInfo nextStage, System.Action onComplete)
        {
            float duration = 0.5f; // 회전 속도 (0.5초)
            float elapsed = 0f;
            bool dataSwapped = false;

            // 시작 각도 (0도)
            Quaternion startRot = Quaternion.Euler(0, 0, 0);

            // 목표 각도 (360도 - 한 바퀴 뺑)
            // 쿼터니언은 360도와 0도가 같아서 Lerp가 안 먹힐 수 있으니,
            // 오일러 각도를 직접 계산해서 넣습니다.

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // EaseInOut 곡선 (부드러운 가감속)
                float curve = t * t * (3f - 2f * t);

                // 0 -> 360도로 X축 회전
                float currentAngle = Mathf.Lerp(0f, 360f, curve);
                _gridContainer.localRotation = Quaternion.Euler(currentAngle, 0f, 0f);

                // [중요] 절반(90도~270도 사이) 쯤 돌았을 때 데이터를 샥 바꿈
                // 90도가 넘어가면 화면에서 안보이거나 뒤집혀 있으므로 이때 교체
                if (!dataSwapped && t >= 0.5f)
                {
                    try
                    {
                        // 여기서 에러나도 멈추지 않게 try-catch
                        BuildGrid(nextStage);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"BuildGrid Error: {e.Message}");
                    }
                    dataSwapped = true;
                }

                yield return null;
            }

            // [안전장치] 애니메이션 끝난 후 확실하게 정리
            _gridContainer.localRotation = Quaternion.identity;

            // 혹시라도 위에서 스왑 안됐으면 여기서라도 함
            if (!dataSwapped) BuildGrid(nextStage);

            _animationRoutine = null;

            // [필수] GameMain에게 "다 끝났어, 진행해!"라고 신호 보냄
            onComplete?.Invoke();
        }
    }
}
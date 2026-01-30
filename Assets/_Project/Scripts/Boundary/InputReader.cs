using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using System.Threading;
using ReflexPuzzle.Control; // IInputReader
using ReflexPuzzle.Entity;  // CellData

namespace ReflexPuzzle.Boundary
{
    public class InputReader : MonoBehaviour, IInputReader
    {
        [SerializeField] private LayerMask _targetLayer;
        [SerializeField] private Camera _targetCamera;

        private void Awake()
        {
            if (_targetCamera == null) _targetCamera = Camera.main;
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            TouchSimulation.Enable();
        }

        private void OnDisable()
        {
            TouchSimulation.Disable();
            EnhancedTouchSupport.Disable();
        }

        // =================================================================
        // [1] 인터페이스 구현: 게임 플레이용 (CellData 반환)
        // =================================================================
        public async Awaitable<CellData> WaitForCellInputAsync(CancellationToken token)
        {
            CellView view = await WaitForCellTouchInternalAsync(token);
            if (view != null)
            {
                return new CellData(
                    view.CurrentNumber,
                    view.CurrentColorID,
                    view.IsTrap,
                    false,
                    view.transform.position // 좌표 담아서 보냄
                );
            }
            return default;
        }

        // =================================================================
        // [2] 인터페이스 구현: 로비용 (아무거나 터치하면 진행) - 이게 없어서 에러났음
        // =================================================================
        public async Awaitable WaitForAnyTouchAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (Touch.activeTouches.Count > 0)
                {
                    var touch = Touch.activeTouches[0];
                    if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                    {
                        return; // 무엇이든 터치되면 즉시 리턴
                    }
                }
                await Awaitable.NextFrameAsync(token);
            }
        }

        // =================================================================
        // [3] 내부 헬퍼 함수: 실제 Raycast 로직 (CellView 반환)
        // =================================================================
        private async Awaitable<CellView> WaitForCellTouchInternalAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (Touch.activeTouches.Count > 0)
                {
                    var touch = Touch.activeTouches[0];
                    if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                    {
                        // UI가 아닌 게임 오브젝트(타일)만 검출
                        if (DetectObject(touch.screenPosition, out CellView cell))
                        {
                            return cell;
                        }
                    }
                }
                await Awaitable.NextFrameAsync(token);
            }
            return null;
        }

        private bool DetectObject(Vector2 screenPos, out CellView cell)
        {
            cell = null;
            Ray ray = _targetCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, _targetLayer))
            {
                return hit.collider.TryGetComponent(out cell);
            }
            return false;
        }
    }
}
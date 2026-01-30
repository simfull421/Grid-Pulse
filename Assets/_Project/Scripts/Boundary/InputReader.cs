using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using System.Threading;
using ReflexPuzzle.Control;
using ReflexPuzzle.Entity;

namespace ReflexPuzzle.Boundary
{
    public class InputReader : MonoBehaviour, IInputReader
    {
        [SerializeField] private LayerMask _targetLayer;
        [SerializeField] private Camera _targetCamera;

        private float _lastInputTime = 0f;
        private const float INPUT_COOLDOWN = 0.1f; // 0.1초 쿨타임

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

        // 1. 비동기 대기 (로비용)
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
                    view.transform.position
                );
            }
            return default;
        }

        // 2. 아무 터치나 대기 (타이틀용)
        public async Awaitable WaitForAnyTouchAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // 모바일
                if (Touch.activeTouches.Count > 0 &&
                    Touch.activeTouches[0].phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    return;
                }
                // PC
                if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                {
                    return;
                }
                await Awaitable.NextFrameAsync(token);
            }
        }

        // 3. 내부 로직
        private async Awaitable<CellView> WaitForCellTouchInternalAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (Time.time < _lastInputTime + INPUT_COOLDOWN)
                {
                    await Awaitable.NextFrameAsync(token);
                    continue;
                }

                CellView detectedCell = null;

                // 모바일
                if (Touch.activeTouches.Count > 0)
                {
                    var touch = Touch.activeTouches[0];
                    if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                    {
                        DetectObject(touch.screenPosition, out detectedCell);
                    }
                }
                // PC (else if)
                else if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                    DetectObject(mousePos, out detectedCell);
                }

                if (detectedCell != null)
                {
                    _lastInputTime = Time.time;
                    return detectedCell;
                }

                await Awaitable.NextFrameAsync(token);
            }
            return null;
        }

        private bool DetectObject(Vector2 screenPos, out CellView cell)
        {
            cell = null;
            Ray ray = _targetCamera.ScreenPointToRay(screenPos);

            // [디버그] 빨간 선 확인
            Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 1.0f);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, _targetLayer))
            {
                return hit.collider.TryGetComponent(out cell);
            }
            return false;
        }

        // [에러 해결] 이 함수가 클래스 밖으로 나가 있어서 문제였습니다. 안으로 가져왔습니다.
        // 4. 즉시 확인 (게임용 - Non Blocking)
        public bool TryGetCellInput(out CellData data)
        {
            data = default;

            if (Time.time < _lastInputTime + INPUT_COOLDOWN) return false;

            CellView detectedCell = null;

            // 1. 터치
            if (Touch.activeTouches.Count > 0)
            {
                var touch = Touch.activeTouches[0];
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    DetectObject(touch.screenPosition, out detectedCell);
                }
            }
            // 2. 마우스
            else if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                DetectObject(mousePos, out detectedCell);
            }

            if (detectedCell != null)
            {
                _lastInputTime = Time.time;
                data = new CellData(
                    detectedCell.CurrentNumber,
                    detectedCell.CurrentColorID,
                    detectedCell.IsTrap,
                    false,
                    detectedCell.transform.position
                );
                return true;
            }

            return false;
        } // 클래스 끝
    }
} // 네임스페이스 끝
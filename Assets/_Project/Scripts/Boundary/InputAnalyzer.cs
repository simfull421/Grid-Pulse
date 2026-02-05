using System;
using UniRx;
using UnityEngine;

namespace TouchIT.Boundary
{
    public class InputAnalyzer : MonoBehaviour, IInputAnalyzer
    {
        [Header("Settings")]
        [SerializeField] private float _tapTimeThreshold = 0.15f;
        [SerializeField] private float _tapDragThreshold = 50f;
        [SerializeField] private float _pinchMinDistPercent = 0.05f;

        // 📡 인터페이스 구현 (Streams)
        private Subject<Vector2> _onTapSubject = new Subject<Vector2>();
        public IObservable<Vector2> OnTap => _onTapSubject;

        private Subject<float> _onPinchSubject = new Subject<float>();
        public IObservable<float> OnPinch => _onPinchSubject;

        private Subject<Vector2> _onDragSubject = new Subject<Vector2>();
        public IObservable<Vector2> OnDrag => _onDragSubject;
        private Subject<Unit> _onPinchEndSubject = new Subject<Unit>();
        public IObservable<Unit> OnPinchEnd => _onPinchEndSubject;
        // 내부 상태
        private float _touchStartTime;
        private Vector2 _touchStartPos;
        private Vector2 _lastDragPos;
        private bool _isPinching = false;
        private float _prevPinchDist = 0f;
        private float _screenDiagonal;

        private void Start()
        {
            float w = Screen.width;
            float h = Screen.height;
            _screenDiagonal = Mathf.Sqrt(w * w + h * h);
        }

        private void Update()
        {
            if (Application.isEditor) ProcessEditorInput();
            ProcessMobileInput();
        }

        private void ProcessMobileInput()
        {
            if (Input.touchCount == 0)
            {
                _isPinching = false;
                return;
            }
            // 👇 핀치 종료 감지 로직 추가
            if (_isPinching && Input.touchCount < 2)
            {
                _isPinching = false;
                _onPinchEndSubject.OnNext(Unit.Default); // 손 뗐음 알림
            }
            // 👇 1. 한 손가락 (Tap or Drag)
            if (Input.touchCount == 1)
            {
                if (_isPinching) return;

                Touch t = Input.GetTouch(0);

                switch (t.phase)
                {
                    case TouchPhase.Began:
                        _touchStartTime = Time.time;
                        _touchStartPos = t.position;
                        _lastDragPos = t.position;
                        break;

                    case TouchPhase.Moved:
                        // 드래그 이벤트 발행
                        Vector2 delta = t.position - _lastDragPos;
                        _onDragSubject.OnNext(delta); // ⬅️ 추가됨
                        _lastDragPos = t.position;
                        break;

                    case TouchPhase.Ended:
                        float timeDt = Time.time - _touchStartTime;
                        float dist = Vector2.Distance(t.position, _touchStartPos);

                        // 짧게 누르고 움직임이 적으면 -> TAP
                        if (timeDt <= _tapTimeThreshold && dist < _tapDragThreshold)
                        {
                            _onTapSubject.OnNext(t.position);
                        }
                        break;
                }
            }
            // 👇 2. 두 손가락 (Pinch)
            else if (Input.touchCount == 2)
            {
                Touch t1 = Input.GetTouch(0);
                Touch t2 = Input.GetTouch(1);

                float currDist = Vector2.Distance(t1.position, t2.position);

                if (t1.phase == TouchPhase.Began || t2.phase == TouchPhase.Began)
                {
                    _prevPinchDist = currDist;
                    _isPinching = true;
                }
                else if (t1.phase == TouchPhase.Moved || t2.phase == TouchPhase.Moved)
                {
                    float delta = currDist - _prevPinchDist;
                    float threshold = _screenDiagonal * _pinchMinDistPercent;

                    if (Mathf.Abs(delta) > threshold)
                    {
                        float normalizedDelta = delta / Screen.height * 2.0f;
                        _onPinchSubject.OnNext(normalizedDelta);
                        _prevPinchDist = currDist;
                    }
                }
            }
        }

        private void ProcessEditorInput()
        {
            // 마우스 드래그 & 탭 시뮬레이션
            if (Input.GetMouseButtonDown(0))
            {
                _touchStartTime = Time.time;
                _touchStartPos = Input.mousePosition;
                _lastDragPos = Input.mousePosition;
            }
            else if (Input.GetMouseButton(0)) // 누른 상태로 이동
            {
                Vector2 currentPos = Input.mousePosition;
                Vector2 delta = currentPos - _lastDragPos;
                if (delta.sqrMagnitude > 1f) // 미세 떨림 방지
                {
                    _onDragSubject.OnNext(delta);
                }
                _lastDragPos = currentPos;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                float timeDt = Time.time - _touchStartTime;
                float dist = Vector2.Distance((Vector2)Input.mousePosition, _touchStartPos);
                if (timeDt <= _tapTimeThreshold && dist < _tapDragThreshold)
                {
                    _onTapSubject.OnNext(Input.mousePosition);
                }
            }

            // 휠 핀치
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _onPinchSubject.OnNext(scroll * 2.0f);
            }
        }
    }
}
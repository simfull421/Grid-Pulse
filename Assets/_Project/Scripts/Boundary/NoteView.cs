using UnityEngine;
using TouchIT.Entity;
using TouchIT.Control;

namespace TouchIT.Boundary
{
    public class NoteView : MonoBehaviour, INoteView
    {
        [Header("Components")]
        [SerializeField] private SpriteRenderer _headSr;
        [SerializeField] private SpriteRenderer _tailSr;
        [SerializeField] private LineRenderer _connector;
        [SerializeField] private Transform _visualRoot; // 회전시킬 부모 객체

        private NoteData _data;
        private GameBinder _binder;
        private float _currentAngle;
        private float _radius;

        // Interface Properties
        public NoteColor Color => _data.Color;
        public NoteType Type => _data.Type;
        public float CurrentAngle => _currentAngle;

        // [수정] TailAngle은 논리적 위치 (판정용)
        public float TailAngle => _currentAngle + (_data.HoldDuration * _data.Speed);

        public Vector3 Position => _headSr.transform.position;

        // 180도(6시)를 지나면 히트 불가 (단, 홀드 중이면 예외 처리는 로직에서 함)
        public bool IsHittable => _currentAngle <= 180f;

        public void Initialize(NoteData data, float radius, GameBinder binder)
        {
            _data = data;
            _binder = binder;
            _currentAngle = data.StartAngle;
            _radius = radius;

            // [수정] Z를 -1.0f로 설정하여 카메라쪽으로 당김 (링보다 무조건 앞에 보임)
            transform.localPosition = new Vector3(0, 0, -1.0f);
            transform.localRotation = Quaternion.identity;

            // 시각적 루트(VisualRoot)만 회전시킴
            if (_visualRoot == null) _visualRoot = transform;
            _visualRoot.localRotation = Quaternion.Euler(0, 0, _currentAngle - 90f);

            // 2. 머리 위치 (로컬 좌표계) - 미세하게 더 앞으로(-0.1)
            _headSr.transform.localPosition = new Vector3(0, _radius, -0.1f);
            _headSr.transform.localRotation = Quaternion.identity;
            _headSr.sortingOrder = 20; // [수정] SortingOrder도 넉넉하게 올림

            // 3. 색상 적용
            var colors = ThemeColors.GetColors(data.Color);
            Color mainColor = (data.Type == NoteType.Hold) ? UnityEngine.Color.cyan : colors.Note;

            _headSr.color = mainColor;
            _headSr.enabled = true;

            // 4. 홀드 노트 설정
            if (data.Type == NoteType.Hold)
            {
                SetupHoldVisuals(mainColor);
            }
            else
            {
                if (_tailSr) _tailSr.enabled = false;
                if (_connector) _connector.enabled = false;
            }

            gameObject.SetActive(true);
        }

        private void SetupHoldVisuals(Color color)
        {
            // 홀드 길이 (각도)
            float lengthAngle = _data.HoldDuration * _data.Speed;

            // A. 꼬리 설정
            if (_tailSr == null)
            {
                _tailSr = Instantiate(_headSr, _visualRoot);
                _tailSr.name = "Tail";
            }
            _tailSr.enabled = true;
            _tailSr.color = color;
            _tailSr.sortingOrder = 10;

            // 꼬리 위치: 머리(0도)를 기준으로 lengthAngle만큼 '뒤(반시계)'에 있음
            // 로컬 좌표계에서 각도로 위치 구하기
            float rad = lengthAngle * Mathf.Deg2Rad;
            // 유니티 2D에서 각도는 반시계가 양수 (0도 = 12시 기준이라 가정시 보정 필요)
            // 여기선 _visualRoot가 돌기 때문에, 로컬에선 머리(0) -> 꼬리(length)로 배치
            // Sin, Cos 좌표 변환 (12시가 0도라 치고, 각도만큼 회전)
            // 12시 = (0, r). 각도 theta만큼 회전 = (-sin(t)*r, cos(t)*r) 
            // *주의: 회전 방향에 따라 부호가 다름. 
            // 노트가 시계방향(450->90)으로 오므로, 뒤쪽은 각도가 더 큼 -> 로컬에선 반시계 방향.

            float tailX = -Mathf.Sin(rad) * _radius;
            float tailY = Mathf.Cos(rad) * _radius;

            _tailSr.transform.localPosition = new Vector3(tailX, tailY, -0.1f);
            // 꼬리 스프라이트 자체 회전 (접선 방향)
            _tailSr.transform.localRotation = Quaternion.Euler(0, 0, lengthAngle);


            // B. 커넥터(라인) 설정
            if (_connector)
            {
                _connector.enabled = true;
                _connector.useWorldSpace = false; // [핵심] 로컬 좌표 사용
                _connector.sortingOrder = 5;      // 노트 뒤

                // 색상 (반투명)
                Color lineCol = color;
                lineCol.a = 0.6f;
                _connector.startColor = lineCol;
                _connector.endColor = lineCol;
                _connector.startWidth = 0.15f;
                _connector.endWidth = 0.15f;

                DrawLocalArc(lengthAngle);
            }
        }

        private void DrawLocalArc(float totalAngle)
        {
            int segments = 15;
            _connector.positionCount = segments + 1;

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float currentDeg = totalAngle * t; // 0도(머리) ~ total(꼬리)
                float rad = currentDeg * Mathf.Deg2Rad;

                // 머리(12시)를 기준으로 반시계 방향으로 그려나감
                float x = -Mathf.Sin(rad) * _radius;
                float y = Mathf.Cos(rad) * _radius;

                _connector.SetPosition(i, new Vector3(x, y, 0));
            }
        }

        public void UpdateRotation(float deltaTime)
        {
            // 각도 감소 (450 -> 90)
            _currentAngle -= _data.Speed * deltaTime;

            // [핵심] 비주얼 루트만 돌리면 자식들(머리,꼬리,라인) 다 같이 돔
            if (_visualRoot)
            {
                _visualRoot.localRotation = Quaternion.Euler(0, 0, _currentAngle - 90f);
            }

            // 페이드 인 처리 (생략 가능하거나 기존 유지)
            float alpha = 1f;
            if (_currentAngle > 360f) alpha = Mathf.Clamp01((450f - _currentAngle) / 90f);

            Color c = _headSr.color; c.a = alpha; _headSr.color = c;
            if (_tailSr && _tailSr.enabled) { Color tc = _tailSr.color; tc.a = alpha; _tailSr.color = tc; }
            if (_connector && _connector.enabled)
            {
                Color lc = _connector.startColor;
                lc.a = 0.6f * alpha;
                _connector.startColor = lc; _connector.endColor = lc;
            }
        }

        public void ReturnToPool() => _binder.ReturnNote(this);
    }
}
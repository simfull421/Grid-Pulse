using UnityEngine;
using TouchIT.Control;
using TouchIT.Entity;

namespace TouchIT.Boundary
{
    public class NoteView : MonoBehaviour, INoteView
    {
        [SerializeField] private Transform _visual;
        [SerializeField] private LineRenderer _holdTail;
        [SerializeField] private SpriteRenderer _bodySr;

        private readonly Vector3 BODY_SCALE = new Vector3(0.2f, 0.6f, 1f);
        private const float ANGLE_OFFSET = -90f;

        private NoteData _data;
        private GameBinder _binder;
        private float _currentAngle;

        // 인터페이스 구현
        public NoteColor Color => _data.Color;
        public NoteType Type => _data.Type;
        public float CurrentAngle => _currentAngle;
        public Vector3 Position => _visual.position;
        public bool IsHittable => _currentAngle <= 180f;

        public void Initialize(NoteData data, float radius, GameBinder binder)
        {
            _data = data;
            _binder = binder;
            _currentAngle = data.StartAngle;

            _bodySr.transform.localScale = BODY_SCALE;
            // [중요] Z축 정렬 (-1f로 링보다 앞으로)
            _visual.localPosition = new Vector3(0, radius, -1f);
            _visual.localRotation = Quaternion.identity;

            // 풀 재사용 시 다시 켜주기
            _bodySr.enabled = true;

            // --- [색상 설정 통합 로직] ---
            UnityEngine.Color noteColor;

            if (data.Type == NoteType.Hold)
            {
                // 홀드 노트는 무조건 Cyan
                noteColor = UnityEngine.Color.cyan;
            }
            else
            {
                // 일반 노트는 테마 반전 (흰 배경엔 검은 노트)
                noteColor = (data.Color == NoteColor.White)
                    ? new UnityEngine.Color(0.1f, 0.1f, 0.1f) // Black
                    : new UnityEngine.Color(0.95f, 0.95f, 0.95f); // White
            }

            _bodySr.color = noteColor;

            // --- [홀드 노트 꼬리 설정] ---
            if (data.Type == NoteType.Hold)
            {
                _holdTail.enabled = true;

                // [Fix] 핑크색 해결: 머터리얼이 없으면 Sprites/Default 강제 할당
                if (_holdTail.sharedMaterial == null)
                {
                    _holdTail.material = new Material(Shader.Find("Sprites/Default"));
                }

                // 꼬리 색상 설정 (Cyan + 투명도)
                UnityEngine.Color tailStart = UnityEngine.Color.cyan;
                UnityEngine.Color tailEnd = new UnityEngine.Color(0f, 1f, 1f, 0f); // 투명

                _holdTail.startColor = tailStart;
                _holdTail.endColor = tailEnd;

                // [Fix] 꼬리 위치 정렬 (-0.5f로 몸통보다는 뒤, 링보다는 앞)
                _holdTail.transform.localPosition = new Vector3(0, 0, -0.5f);

                DrawHoldTail(radius, data.HoldDuration * data.Speed);
            }
            else
            {
                if (_holdTail) _holdTail.enabled = false;
            }

            UpdateTransform();
            gameObject.SetActive(true);
        }

        private void DrawHoldTail(float radius, float lengthAngle)
        {
            _holdTail.positionCount = 20;
            _holdTail.useWorldSpace = false;
            _holdTail.startWidth = 0.4f;
            _holdTail.endWidth = 0.0f;

            for (int i = 0; i < 20; i++)
            {
                float t = (float)i / 19f;
                float angleDeg = 90f + (lengthAngle * t);
                float rad = angleDeg * Mathf.Deg2Rad;
                float x = Mathf.Cos(rad) * radius;
                float y = Mathf.Sin(rad) * radius;
                _holdTail.SetPosition(i, new Vector3(x, y, 0)); // Z는 로컬 포지션으로 제어
            }
        }

        public void UpdateRotation(float deltaTime)
        {
            _currentAngle -= _data.Speed * deltaTime;
            UpdateTransform();
            CheckVisualVisibility();
        }

        private void UpdateTransform()
        {
            transform.localRotation = Quaternion.Euler(0, 0, _currentAngle + ANGLE_OFFSET);
        }

        private void CheckVisualVisibility()
        {
            // 80도 미만이면 시각적으로 숨김 (판정선 지남)
            if (_currentAngle < 80f)
            {
                if (_bodySr.enabled)
                {
                    _bodySr.enabled = false;
                    if (_holdTail) _holdTail.enabled = false;
                }
            }
        }

        public void ReturnToPool()
        {
            _binder.ReturnNote(this);
        }
    }
}
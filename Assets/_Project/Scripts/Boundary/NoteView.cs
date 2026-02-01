using UnityEngine;
using TouchIT.Entity;
using TouchIT.Control;

namespace TouchIT.Boundary
{
    public class NoteView : MonoBehaviour, INoteView
    {
        [SerializeField] private Transform _visual;
        [SerializeField] private LineRenderer _holdTail;
        [SerializeField] private SpriteRenderer _bodySr;
        // [삭제] _outlineSr 변수 및 관련 로직 제거됨

        // [설정] 코드로 제어하는 디자인 수치들
        private readonly Vector3 BODY_SCALE = new Vector3(0.2f, 0.6f, 1f);

        // [위치 보정] 9시(180도) -> 12시(90도) 보정값
        private const float ANGLE_OFFSET = -90f;

        private NoteData _data;
        private GameBinder _binder;
        private float _currentAngle;

        // =========================================================
        // [인터페이스 구현] 여기가 핵심입니다
        // =========================================================
        public NoteColor Color => _data.Color;
        public NoteType Type => _data.Type;
        public float CurrentAngle => _currentAngle;

        // [신규] 이 코드가 없어서 에러가 났던 겁니다.
        // 이펙트가 터질 위치를 알려줍니다 (회전하는 부모가 아니라, 실제 눈에 보이는 자식의 위치)
        public Vector3 Position => _visual.position;
        // =========================================================

        public void Initialize(NoteData data, float radius, GameBinder binder)
        {
            _data = data;
            _binder = binder;
            _currentAngle = data.StartAngle;

            // 1. 비주얼 크기 설정
            _bodySr.transform.localScale = BODY_SCALE;

            // 2. 위치 잡기 (반지름만큼 떨어진 곳)
            _visual.localPosition = new Vector3(0, radius, 0);
            _visual.localRotation = Quaternion.identity;

            // 3. 색상 설정 (테두리 삭제됨)
            Color whiteTheme = new Color(0.95f, 0.95f, 0.95f);
            Color blackTheme = new Color(0.1f, 0.1f, 0.1f);

            if (data.Color == NoteColor.White)
            {
                _bodySr.color = whiteTheme;
            }
            else
            {
                _bodySr.color = blackTheme;
            }

            // 4. 홀드 노트 꼬리 그리기
            if (data.Type == NoteType.Hold)
            {
                _holdTail.enabled = true;

                if (_holdTail.sharedMaterial == null)
                    _holdTail.material = new Material(Shader.Find("Sprites/Default"));

                _holdTail.startColor = (data.Color == NoteColor.White) ? whiteTheme : blackTheme;
                _holdTail.endColor = (data.Color == NoteColor.White) ? new Color(1, 1, 1, 0) : new Color(0, 0, 0, 0);

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
                // [위치 보정] 12시 기준으로 꼬리 그리기
                float angleDeg = 90f + (lengthAngle * t);
                float rad = angleDeg * Mathf.Deg2Rad;

                float x = Mathf.Cos(rad) * radius;
                float y = Mathf.Sin(rad) * radius;

                _holdTail.SetPosition(i, new Vector3(x, y, 0));
            }
        }

        public void UpdateRotation(float deltaTime)
        {
            _currentAngle -= _data.Speed * deltaTime;
            UpdateTransform();
        }

        private void UpdateTransform()
        {
            transform.localRotation = Quaternion.Euler(0, 0, _currentAngle + ANGLE_OFFSET);
        }

        public void ReturnToPool()
        {
            _binder.ReturnNote(this);
        }
    }
}
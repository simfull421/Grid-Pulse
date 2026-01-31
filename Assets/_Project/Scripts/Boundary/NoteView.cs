using UnityEngine;
using TouchIT.Entity;
using TouchIT.Control;

namespace TouchIT.Boundary
{
    public class NoteView : MonoBehaviour, INoteView
    {
        [SerializeField] private Transform _visualPivot; // 자식 오브젝트(Visual)

        private NoteData _data;
        private float _currentAngle;
        private bool _isActive;

        public float CurrentAngle => _currentAngle;
        public int SoundIndex => _data.SoundIndex;

        public void Initialize(NoteData data, float ringRadius)
        {
            _data = data;
            _currentAngle = data.StartAngle;
            _isActive = true;
            gameObject.SetActive(true);

            // [수정] 주석 해제 및 코드 적용!
            // 이 코드가 자식을 강제로 밖으로 밀어냅니다.
            if (_visualPivot != null)
            {
                // 1. 위치 이동: 반지름만큼 위로 올림 (중심에서 멀어짐)
                // ringRadius가 3.0이면 Y를 2.5 정도로 살짝 안쪽에 두는 게 이쁨
                float offset = ringRadius - 0.5f;
                _visualPivot.localPosition = new Vector3(0, offset, 0);

                // 2. 회전 초기화: 자식은 회전하지 않음 (항상 위를 보거나, 부모 따라 돌거나)
                _visualPivot.localRotation = Quaternion.identity;

                // 3. (선택사항) 노트 크기가 너무 크면 여기서 줄여버림
                // _visualPivot.localScale = new Vector3(0.2f, 1.0f, 1.0f); 
            }

            UpdateTransform();
        }

        public void UpdateRotation(float deltaTime)
        {
            if (!_isActive) return;
            _currentAngle -= _data.Speed * deltaTime;
            UpdateTransform();
        }

        private void UpdateTransform()
        {
            // 부모를 Z축으로 회전시킴 -> 자식은 떨어져 있으니 공전하게 됨
            transform.localRotation = Quaternion.Euler(0, 0, _currentAngle);
        }

        public void Deactivate()
        {
            _isActive = false;
            gameObject.SetActive(false);
        }
    }
}
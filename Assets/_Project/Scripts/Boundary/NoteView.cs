using UnityEngine;
using TouchIT.Entity;
using TouchIT.Control; // 인터페이스 참조

namespace TouchIT.Boundary
{
    // MonoBehaviour이면서 INoteView 인터페이스를 구현
    public class NoteView : MonoBehaviour, INoteView
    {
        [SerializeField] private Transform _visualPivot;

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

            // 비주얼 길이 조정 로직 (필요시 주석 해제)
            /*
            if (_visualPivot != null)
            {
                Vector3 scale = _visualPivot.localScale;
                scale.y = ringRadius; 
                _visualPivot.localScale = scale;
                _visualPivot.localPosition = new Vector3(0, ringRadius * 0.5f, 0);
            }
            */
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
            transform.localRotation = Quaternion.Euler(0, 0, _currentAngle);
        }

        public void Deactivate()
        {
            _isActive = false;
            gameObject.SetActive(false);
        }
    }
}
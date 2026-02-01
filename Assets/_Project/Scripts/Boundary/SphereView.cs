using UnityEngine;
using System.Collections;
using TouchIT.Entity;

namespace TouchIT.Boundary
{
    public class SphereView : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private Coroutine _spinCoroutine;

        public void Initialize()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        public void SetColor(NoteColor mode)
        {
            if (_sr == null) return;

            // 기존 코루틴 중단 (연타 방지)
            if (_spinCoroutine != null) StopCoroutine(_spinCoroutine);

            // 회전 연출 시작
            _spinCoroutine = StartCoroutine(SpinAndChangeColor(mode));
        }

        private IEnumerator SpinAndChangeColor(NoteColor targetMode)
        {
            float duration = 0.25f; // 0.25초 동안 회전
            float elapsed = 0f;

            // 회전 시작 전 현재 상태
            Quaternion startRot = transform.localRotation;
            // Z축으로 180도 돌리기 (휙!)
            Quaternion endRot = startRot * Quaternion.Euler(0, 0, 180f);

            Color startColor = _sr.color;
            Color endColor = (targetMode == NoteColor.White) ? Color.white : Color.black;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // EaseOutBack: 살짝 오버했다가 돌아오는 탄력적인 느낌
                // (직접 구현하기 복잡하니 부드러운 SmoothStep 사용)
                t = t * t * (3f - 2f * t);

                // 1. 회전 적용
                transform.localRotation = Quaternion.Lerp(startRot, endRot, t);

                // 2. 색상 변경 (중간 지점에서 자연스럽게 섞임)
                _sr.color = Color.Lerp(startColor, endColor, t);

                // 3. [Juice] 스케일 펀치 (동전 뒤집기 느낌)
                // 중간(0.5)일 때 X축이 0.2까지 줄어들었다가 다시 1로 복귀
                // 이러면 입체적으로 회전하는 느낌이 남
                float scaleX = Mathf.Lerp(1f, 0.2f, Mathf.Sin(t * Mathf.PI));
                transform.localScale = new Vector3(scaleX, 1f, 1f);

                yield return null;
            }

            // 확실한 마무리
            transform.localRotation = endRot;
            transform.localScale = Vector3.one;
            _sr.color = endColor;
        }

        // 피격 시 줌아웃 효과 (기존 코드 유지)
        public void PlayHitEffect()
        {
            // 여기에 보스 피격 펀치 로직 추가 가능
        }

        public void ReduceLife(int amount) { } // 사용 안함 (LifeRing이 담당)
    }
}
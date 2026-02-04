using UnityEngine;

namespace TouchIT.Boundary
{
    public class TouchInputHelper : MonoBehaviour
    {
        public float GetPinchDelta()
        {
            // 에디터: 휠
            if (Application.isEditor)
            {
                return Input.GetAxis("Mouse ScrollWheel") * 5.0f;
            }

            // 모바일: 두 손가락
            if (Input.touchCount == 2)
            {
                Touch t1 = Input.GetTouch(0);
                Touch t2 = Input.GetTouch(1);

                Vector2 t1Prev = t1.position - t1.deltaPosition;
                Vector2 t2Prev = t2.position - t2.deltaPosition;

                float prevDist = (t1Prev - t2Prev).magnitude;
                float currDist = (t1.position - t2.position).magnitude;

                // 화면 해상도 대응을 위해 정규화 필요하지만, 일단 단순 차이 반환
                return (currDist - prevDist) * 0.01f;
            }

            return 0f;
        }
    }
}
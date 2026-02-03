using UnityEngine;

// [사업성 보고서 기반] 2.7G 임계값 적용 쉐이크 감지기
public class ShakeDetector
{
    private const float SHAKE_THRESHOLD_G = 2.7f; // 보고서 추천값 (2.7G)
    private const float MIN_SHAKE_INTERVAL = 0.1f; // 너무 빠른 연속 감지 방지

    private float _lastShakeTime;
    private Vector3 _lowPassValue; // 저역 통과 필터 (중력 제거용)

    public bool IsShaking()
    {
        Vector3 acceleration = Input.acceleration;

        // 1. 저역 통과 필터로 중력(1G) 성분 제거 (순수 움직임만 추출)
        const float alpha = 0.8f;
        _lowPassValue = Vector3.Lerp(_lowPassValue, acceleration, alpha);
        Vector3 deltaAcc = acceleration - _lowPassValue;

        // 2. 가속도 크기(G) 계산
        float gForce = deltaAcc.magnitude / 9.8f; // 유니티 Input.acceleration은 이미 G단위일 수 있으나 확인 필요. 
                                                  // 보통 Input.acceleration의 magnitude가 1.0 = 1G 임.

        // 유니티 문법상: Input.acceleration.magnitude가 1.0이면 정지상태(중력만 받음)
        // 따라서 deltaAcc.magnitude가 순수 흔들림 강도임.
        float shakeStrength = deltaAcc.magnitude * 2f; // 감도 보정

        // 3. 임계값 체크
        if (shakeStrength > SHAKE_THRESHOLD_G && (Time.time - _lastShakeTime) > MIN_SHAKE_INTERVAL)
        {
            _lastShakeTime = Time.time;
            return true;
        }

        return false;
    }
}
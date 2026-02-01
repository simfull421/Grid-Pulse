using UnityEngine;

// 정적(Static) 클래스로 만들어서 어디서든 Vibration.Vibrate()로 호출 가능하게 함
public static class Vibration
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
    private static AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
    private static AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
#endif

    public static void Vibrate(long milliseconds)
    {
        // 에디터에서는 진동 안 함 (오류 방지)
#if UNITY_EDITOR
        return;
#elif UNITY_ANDROID
        if (vibrator != null)
        {
            vibrator.Call("vibrate", milliseconds);
        }
#elif UNITY_IOS
        // iOS는 햅틱 엔진 사용 (짧은 진동)
        // Handheld.Vibrate()는 길지만, Taptic 엔진 연동은 플러그인이 필요하므로
        // 일단 기본 진동을 쓰되, 너무 자주는 안 울리게 로직에서 제어해야 함.
        Handheld.Vibrate(); 
#endif
    }
}
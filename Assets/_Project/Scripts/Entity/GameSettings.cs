namespace TouchIT.Entity
{
    [System.Serializable]
    public class GameSettings
    {
        public bool IsVibrationOn = true;
        public bool IsSoundOn = true;
        public bool IsDarkMode = true; // 테마 (True: 검정배경, False: 흰배경)
    }
}
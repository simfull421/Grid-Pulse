namespace TouchIT.Entity
{
    public enum NoteColor
    {
        White,
        Black,
        Cosmic // [신규] 우주 테마
    }
    public enum NoteType
    {
        Normal, // 단타
        Hold    // 꾹 누르기 (수학적 꼬리 필요)
    }
    public enum HitResult
    {
        None,       // 판정 전
        Miss,       // 놓침
        NotBad,     // 살짝 어긋남
        Good,       // 적절함
        Great,      // 완벽함 (카타르시스)
        GroggyTick  // 그로기 상태 점수 획득
    }
}
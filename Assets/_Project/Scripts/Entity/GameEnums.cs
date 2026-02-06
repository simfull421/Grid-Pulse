namespace TouchIT.Entity
{
    public enum GameState
    {
        MainMenu,       // 구체 둥둥 떠있는 상태
        StageSelect,    // 구체 안으로 들어온 상태 (앨범 고르기)
        InGame,         // 리듬 게임 진행 중
        Result          // 결과 화면
    }

    // 게임의 현재 페이즈 정의
    public enum GamePhase
    {
        RingMode,       // 일반 링 모드
        OsuMode,        // 오수 모드
        Transitioning   // 전환 중 (입력 차단)
    }
}
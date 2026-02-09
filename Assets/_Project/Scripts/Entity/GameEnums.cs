namespace TouchIT.Entity
{
    public enum GameState
    {
        MainMenu,       // 구체 둥둥 떠있는 상태
        StageSelect,    // 구체 안으로 들어온 상태 (앨범 고르기)
        InGame,
        GameOver,   // 👈 [추가] 실패 (HP 0) -> 광고 보고 부활 or 재시작
        Result      // 클리어 성공 (점수 집계)
    }

    // 게임의 현재 페이즈 정의
    public enum GamePhase
    {
        RingMode,       // 일반 링 모드
        OsuMode,        // 오수 모드
        Transitioning   // 전환 중 (입력 차단)
    }
}
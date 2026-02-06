using TouchIT.Entity;

namespace TouchIT.Control
{
    // 두 모드(Ring, Osu)가 공통으로 가져야 할 기능 정의
    public interface ISpawnService
    {
        void LoadPattern(MusicData data);
        void OnUpdate();
        void Stop();

        void Resume();

        // 판정 로직 (각 모드마다 판정 방식이 다름)
        // 히트한 노트를 반환하거나 null 반환
        TouchIT.Boundary.INoteView CheckHitAndGetNote();
    }
}
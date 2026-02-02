using UnityEngine;
using TouchIT.Control;
using TouchIT.Boundary;
using TouchIT.Entity;

namespace TouchIT.App
{
    // [순수 DI] 라이브러리 없이 직접 조립하는 공장
    // 가장 먼저 실행되어야 함
    [DefaultExecutionOrder(-9999)]
    public class GameBootstrapper : MonoBehaviour
    {
        private void Awake()
        {
            Debug.Log("🚀 [Bootstrapper] System Start...");

            // 1. Data Load (Resources 폴더 자동 로드)
            var beatLib = Resources.Load<BeatLibrary>("Data/MainBeats");
            if (beatLib == null) Debug.LogError("❌ BeatLibrary 못 찾음! 경로 확인: Resources/Data/MainBeats");

            // 2. View 찾기 (씬에 있는거 자동 검색 - 드래그 앤 드롭 X)
            // 2023 이후 버전은 FindFirstObjectByType, 이전은 FindObjectOfType
            var binder = FindFirstObjectByType<GameBinder>();
            if (binder == null) Debug.LogError("❌ 씬에 GameBinder 프리팹이 없습니다!");

            // 3. Audio 찾기 (없으면 자동 생성)
            var audio = FindFirstObjectByType<AudioManager>();
            if (audio == null)
            {
                var audioObj = new GameObject("AudioManager");
                audio = audioObj.AddComponent<AudioManager>();
            }

            // 4. Controller 찾기 (없으면 자동 생성)
            var controller = FindFirstObjectByType<GameController>();
            if (controller == null)
            {
                var ctrlObj = new GameObject("GameController");
                controller = ctrlObj.AddComponent<GameController>();
            }

            // ====================================================
            // 5. [핵심] 의존성 주입 (Dependency Injection)
            // VContainer가 해주던 걸 그냥 수동으로 한 줄 적으면 됨
            // ====================================================

            // 바운더리 초기화
            binder.Initialize();
            audio.Initialize();

            // 컨트롤러에 꽂아넣기
            controller.Initialize(binder, audio, beatLib);

            Debug.Log("✅ [Bootstrapper] All Systems Wired & Ready!");
        }
    }
}
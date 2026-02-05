using System.Collections.Generic;
using UnityEngine;
using TouchIT.Boundary;
using TouchIT.Control;
using TouchIT.Entity;
using System.Linq;
using UniRx;

namespace TouchIT.App
{
    public class GameBootstrapper : MonoBehaviour
    {
        // POCO 클래스들은 가비지 컬렉터에 수집되지 않도록 멤버 변수로 유지
        private NoteSpawnService _spawnService;
        private GameController _gameController;

        private void Awake()
        {
            Debug.Log("🚀 Bootstrapper: Initializing...");

            // 1. Scene Components 찾기
            var input = FindFirstObjectByType<InputAnalyzer>();
            var mainView = FindFirstObjectByType<MainView>();
            var noteFactory = FindFirstObjectByType<NoteFactory>();
            var audio = FindFirstObjectByType<AudioManager>();

            // 안전장치
            if (noteFactory == null) Debug.LogError("❌ NoteFactory가 없습니다!");
            if (audio == null) Debug.LogError("❌ AudioManager가 없습니다!");
            if (input == null) Debug.LogError("❌ InputAnalyzer가 없습니다!");
            if (mainView == null) Debug.LogError("❌ MainView가 없습니다!");

            // 초기화 호출
            noteFactory.Initialize();
            audio.Initialize();

            // 2. Data Load
            var loadedAlbums = Resources.LoadAll<MusicData>("MusicData").ToList();
            if (loadedAlbums.Count == 0)
            {
                Debug.LogWarning("⚠️ No MusicData found in Resources/MusicData!");
                var dummy = ScriptableObject.CreateInstance<MusicData>();
                dummy.Title = "Dummy Track";
                dummy.ThemeColor = Color.gray;
                loadedAlbums.Add(dummy);
            }

            // 3. Service Instantiation (POCO 생성)
            var fireService = new FireService(mainView);
            var saveDataService = new SaveDataService();
            var adManager = FindFirstObjectByType<AdManager>();
            if (adManager == null) adManager = new GameObject("AdManager").AddComponent<AdManager>();
            adManager.Initialize();

            // 🚨 [수정된 부분] var spawnService 가 아니라 _spawnService 멤버 변수에 직접 할당해야 합니다!
            _spawnService = new NoteSpawnService(noteFactory, audio);

            // 4. Controller 생성 (모두 주입)
            // 여기서는 _spawnService 멤버 변수를 넘겨줍니다.
            _gameController = new GameController(
                mainView,
                input,
                audio,
                _spawnService,
                fireService,
                saveDataService,
                adManager,
                loadedAlbums
            );

            // 5. Update Loop 연결 (Service Tick)
            Observable.EveryUpdate()
                .Subscribe(_ =>
                {
                    // 이제 _spawnService가 null이 아니므로 정상 작동합니다.
                    _spawnService.OnUpdate();
                })
                .AddTo(this);

            Debug.Log("✅ Bootstrapper: All Systems Go!");
        }

        private void OnDestroy()
        {
            _gameController?.Dispose();
        }
    }
}
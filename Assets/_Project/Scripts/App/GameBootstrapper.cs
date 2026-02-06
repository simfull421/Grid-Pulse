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
        // Controller 하나만 가지고 있으면 됨 (Update는 Controller가 돌림)
        private GameController _gameController;

        private void Awake()
        {
            Debug.Log("🚀 Bootstrapper: Initializing...");

            // 1. Scene Components
            var input = FindFirstObjectByType<InputAnalyzer>();
            var mainView = FindFirstObjectByType<MainView>();
            var noteFactory = FindFirstObjectByType<NoteFactory>(); // 링 팩토리
            var osuFactory = FindFirstObjectByType<OsuNoteFactory>(); // 오수 팩토리 (추가 필요!)
            var audio = FindFirstObjectByType<AudioManager>();
            var vfxFactory = FindFirstObjectByType<VFXFactory>();

            // 안전장치
            if (noteFactory == null) Debug.LogError("❌ NoteFactory Missing!");
            if (osuFactory == null) Debug.LogError("❌ OsuNoteFactory Missing!"); // 체크

            noteFactory.Initialize();
            if (osuFactory != null) osuFactory.Initialize(); // 초기화
            audio.Initialize();
            if (vfxFactory != null) vfxFactory.Initialize();

            // 2. Data Load
            var loadedAlbums = Resources.LoadAll<MusicData>("MusicData").ToList();
            if (loadedAlbums.Count == 0) loadedAlbums.Add(ScriptableObject.CreateInstance<MusicData>());

            // 3. Service Instantiation
            var fireService = new FireService(mainView);
            var saveDataService = new SaveDataService();
            var vfxService = new VFXService(vfxFactory);

            var adManager = FindFirstObjectByType<AdManager>();
            if (adManager == null) adManager = new GameObject("AdManager").AddComponent<AdManager>();
            adManager.Initialize();

            // 🚨 [핵심] 두 가지 스폰 서비스 생성 (인터페이스 타입으로)
            ISpawnService ringSpawner = new NoteSpawnService(noteFactory, audio);
            ISpawnService osuSpawner = new OsuSpawnService(osuFactory, audio);

            // 4. Controller 생성 (두 서비스 모두 주입)
            _gameController = new GameController(
                mainView, input, audio,
                ringSpawner, osuSpawner, // 👈 여기 변경됨!
                fireService, saveDataService, adManager, vfxService,
                loadedAlbums
            );

            // 5. Update Loop 연결 (Controller에게 위임)
            Observable.EveryUpdate()
                .Subscribe(_ =>
                {
                    _gameController.OnUpdate(); // Controller가 현재 활성 스포너를 돌림
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
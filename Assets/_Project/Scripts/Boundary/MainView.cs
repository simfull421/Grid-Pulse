using UnityEngine;
using DG.Tweening;
using TouchIT.Control;
using TouchIT.Entity;

namespace TouchIT.Boundary
{
    public class MainView : MonoBehaviour, IMainView
    {
        [Header("Scene Objects")]
        [SerializeField] private Transform _sphereObj;
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private SpriteRenderer _sphereRenderer;
        [SerializeField] private ParticleSystem _portalEffect; // ✨ 비눗방울 파티클 연결
        // ✅ [추가] 링 뷰 연결 슬롯
        [SerializeField] private LifeRingView _lifeRingView;
        [SerializeField] private float _punchPower = 0.3f;   // 타격 시 튕기는 강도
        [Header("Animation Settings")]
        [SerializeField] private float _breathingScale = 1.1f;
        [SerializeField] private float _previewZoomDist = 7.0f;

        private Tweener _breathingTweener;
        private Material _sphereMat;
        private Vector3 _baseScale;
        private Vector3 _originalCamPos;

        private bool _isTransitioning = false;
        private float _currentManualScale = 1.0f;

        public bool IsTransitioning => _isTransitioning;

        public void Initialize()
        {
            if (_sphereObj == null) _sphereObj = transform;
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_sphereRenderer == null) _sphereRenderer = _sphereObj.GetComponent<SpriteRenderer>();

            _sphereMat = _sphereRenderer.material;
            _baseScale = Vector3.one;
            _originalCamPos = _mainCamera.transform.position;

            // [고정 테마] 1. 메인 화면: 하얀 배경 & 검은 구체
            _mainCamera.backgroundColor = Color.white;
            _sphereMat.color = Color.black;

            StartBreathing();

            // ✅ [추가] 여기서 링을 초기화해줘야 에러가 안 납니다!
            if (_lifeRingView != null)
            {
                _lifeRingView.Initialize();
                ShowRing(false); // 🔕 [수정] 처음엔 링 끄기!
            }
            else
            {
                Debug.LogError("❌ MainView: LifeRingView가 연결되지 않았습니다! 인스펙터를 확인하세요.");
            }
        }
        // ✅ [추가] 인터페이스 구현: 링 끄고 켜기
        public void ShowRing(bool show)
        {
            if (_lifeRingView != null)
                _lifeRingView.gameObject.SetActive(show);
        }
        // 💥 [핵심] 타격감 함수 (GameController가 호출)
        // Hit 성공 시 호출됨
        public void OnNoteHitSuccess(float fuelRatio)
        {
            // 1. 구체 탄성 효과 (Punch)
            // 현재 크기에서 순간적으로 띠용! 하고 커졌다가 돌아옴
            _sphereObj.DOKill(true); // 기존 트윈 즉시 완료 처리 (중첩 방지)
            _sphereObj.DOPunchScale(Vector3.one * _punchPower, 0.2f, 10, 1);

            // 2. 생명력 업데이트 (기준 크기 변경)
            _baseScale = Vector3.one * fuelRatio;

            // 3. 링 반응
            if (_lifeRingView != null) _lifeRingView.OnHitEffect();

            // 4. 카메라 쉐이크 (약하게)
            _mainCamera.transform.DOShakePosition(0.1f, 0.1f, 10);
        }
        // 🚨 오수 모드 준비 (발광)
        // 🫧 [수정됨] 오수 모드 준비 (포탈 열림)
        public void AnimateOsuReady()
        {
            _breathingTweener?.Kill();

            // 1. 비눗방울 파티클 재생
            if (_portalEffect != null)
            {
                _portalEffect.gameObject.SetActive(true);
                _portalEffect.Play();
            }

            // 2. 꿀렁거림 (Wobbly) - 형태가 불안정해짐
            _sphereObj.DOPunchScale(Vector3.one * 0.1f, 1.0f, 2, 0.5f).SetLoops(-1, LoopType.Restart);

            // 3. 색상은 앨범 테마색이나 은은한 무지개빛 (여기선 일단 유지)
            // _sphereMat.DOColor(Color.cyan, 1.0f).SetLoops(-1, LoopType.Yoyo); 
        }
        // ⏳ [추가] 포탈 닫힘 (시간 초과 시 연출) -> 게임 오버로 이어짐
        public void AnimatePortalClosing(float duration, System.Action onClosed)
        {
            // 천천히 작아짐 (0이 되면 끝)
            _sphereObj.DOScale(Vector3.zero, duration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    // 파티클도 끄기
                    if (_portalEffect != null) _portalEffect.Stop();
                    onClosed?.Invoke(); // 콜백 호출 (GameController에게 알림)
                });
        }
        // 블랙홀 등장 (복귀 준비)
        public void AnimatePortalClosingReady()
        {
            // 중앙에 검은 구체(블랙홀) 생성 혹은 기존 구체를 검게 변형
            _sphereObj.localScale = Vector3.zero;
            _sphereMat.color = Color.black;
            _sphereObj.DOScale(Vector3.one * 2.0f, 1.0f).SetEase(Ease.OutBack);
            // 진동하며 유저에게 "줄여라!" 신호
        }
        // 링 모드로 복귀 (축소)
        public void AnimateExitOsuMode()
        {
            Sequence seq = DOTween.Sequence();

            // 1. 화면이 중앙 블랙홀로 빨려들어감 (카메라 줌아웃 or 배경 축소)
            // 여기선 심플하게 배경색 반전 + 카메라 복귀
            seq.Append(_mainCamera.transform.DOMoveZ(_originalCamPos.z, 0.5f).SetEase(Ease.OutExpo));

            seq.AppendCallback(() =>
            {
                _mainCamera.backgroundColor = Color.black; // 다시 링 모드 배경(검정)
                _sphereMat.color = Color.white;            // 구체(하양)
            });

            seq.OnComplete(() =>
            {
                _baseScale = Vector3.one;
                StartBreathing();
            });
        }
        // ⚔️ 오수 모드 진입 확정
        public void AnimateEnterOsuMode()
        {
            _sphereObj.DOKill();
            _breathingTweener?.Kill();

            Sequence seq = DOTween.Sequence();

            // 1. 줌인 연출
            seq.Append(_sphereObj.DOScale(Vector3.one * 100f, 0.4f).SetEase(Ease.InExpo));
            seq.Append(_mainCamera.transform.DOMoveZ(_originalCamPos.z + 5f, 0.4f).SetEase(Ease.InExpo));

            // 2. 화이트 아웃 & 색상 반전 (확실하게 설정)
            seq.AppendCallback(() =>
            {
                // 배경을 검정(Black)으로 유지하고 싶으시다면:
                _mainCamera.backgroundColor = Color.black;

                // 구체는 눈에 띄는 색(Cyan 등)으로 변경 + 크기 재설정
                _sphereMat.color = Color.cyan;
                _sphereObj.localScale = Vector3.one * 0.8f; // 플레이하기 좋은 크기로 초기화

                if (_portalEffect != null) _portalEffect.Stop();
            });

            seq.OnComplete(() => {
                Debug.Log("⚔️ View: Osu Mode Visuals Ready (Sphere Visible)");
            });
        }
        // 🕹️ [추가] 구체 이동 함수
        public void MoveSphere(Vector2 screenDelta)
        {
            if (_isTransitioning) return;

            // 화면 델타값을 월드 좌표로 변환 (감도 조절)
            // Orthographic Size에 비례하여 이동 속도 보정
            float sensitivity = _mainCamera.orthographicSize * 2.0f / Screen.height;

            Vector3 moveAmount = new Vector3(screenDelta.x, screenDelta.y, 0) * sensitivity;

            // 현재 위치에 더하기
            Vector3 newPos = _sphereObj.position + moveAmount;

            // (선택사항) 화면 밖으로 못 나가게 가두기 (Clamp)
            float xLimit = 2.5f;
            float yLimit = 4.5f;
            newPos.x = Mathf.Clamp(newPos.x, -xLimit, xLimit);
            newPos.y = Mathf.Clamp(newPos.y, -yLimit, yLimit);

            _sphereObj.position = newPos;
        }
        // 🔄 Main -> Stage
        public void AnimateMainToStage(Color ignoredColor)
        {
            if (_isTransitioning) return;
            _isTransitioning = true;
            _breathingTweener?.Pause();

            Sequence seq = DOTween.Sequence();
            seq.Append(_sphereObj.DOScale(Vector3.one * 60f, 0.6f).SetEase(Ease.InExpo));
            seq.AppendCallback(() =>
            {
                _mainCamera.backgroundColor = Color.black;
                _sphereObj.localScale = Vector3.zero;
                _sphereMat.color = Color.white;
            });
            seq.Append(_sphereObj.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack));
            seq.OnComplete(() => {
                _baseScale = Vector3.one;
                StartBreathing();
                _isTransitioning = false;
            });
        }

        // 🔙 Stage -> Main
        public void AnimateStageToMain(Color ignoredColor)
        {
            if (_isTransitioning) return;
            _isTransitioning = true;
            _breathingTweener?.Pause();

            Sequence seq = DOTween.Sequence();
            seq.Append(_sphereObj.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack));
            seq.AppendCallback(() =>
            {
                _sphereMat.color = Color.black;
                _mainCamera.backgroundColor = Color.white;
                _sphereObj.localScale = Vector3.one * 50f;
            });
            seq.Append(_sphereObj.DOScale(Vector3.one, 0.6f).SetEase(Ease.OutExpo));
            seq.OnComplete(() => {
                _baseScale = Vector3.one;
                StartBreathing();
                _isTransitioning = false;
            });
        }

        // 🚀 Game Start
        public void AnimateGameStart()
        {
            if (_isTransitioning) return;
            _isTransitioning = true;
            _breathingTweener?.Pause();

            Sequence seq = DOTween.Sequence();
            seq.Append(_sphereObj.DOScale(Vector3.one * 100f, 0.6f).SetEase(Ease.InExpo));
            seq.AppendCallback(() =>
            {
                _mainCamera.backgroundColor = Color.white;
                _sphereObj.localScale = Vector3.zero;
                _sphereMat.color = Color.black;
            });
            _baseScale = Vector3.one * 0.5f;
            seq.Append(_sphereObj.DOScale(_baseScale, 0.4f).SetEase(Ease.OutBack));
            seq.OnComplete(() =>
            {
                _breathingTweener = _sphereObj.DOScale(_baseScale * 1.2f, 0.5f)
                    .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);

                ShowRing(true); // 🔔 [추가] 게임 시작 완료 후 링 켜기!
                _isTransitioning = false;
            });
            _mainCamera.transform.DOShakePosition(0.5f, 0.5f, 20);
        }

        public void SetInteractiveScale(float delta)
        {
            if (_isTransitioning) return;
            _breathingTweener?.Pause();
            _currentManualScale += delta * 2.0f;
            _currentManualScale = Mathf.Clamp(_currentManualScale, 0.5f, 50.0f);
            _sphereObj.localScale = Vector3.one * _currentManualScale;
        }

        public void ResetScale()
        {
            if (_isTransitioning) return;
            _sphereObj.DOScale(_baseScale, 0.3f).SetEase(Ease.OutBack)
                .OnComplete(() => {
                    _currentManualScale = 1.0f;
                    StartBreathing();
                });
        }

        public void SetLifeScale(float ratio)
        {
            if (_isTransitioning) return;
            _baseScale = Vector3.one * ratio;
            _breathingTweener?.Kill();
            _sphereObj.DOScale(_baseScale, 0.3f).SetEase(Ease.OutBack)
                .OnComplete(() => StartBreathing());
        }

        private void StartBreathing()
        {
            _breathingTweener?.Kill();
            _breathingTweener = _sphereObj.DOScale(_baseScale * _breathingScale, 1.0f)
                .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }

        public void UpdateAlbumVisual(MusicData data)
        {
            _sphereObj.DOPunchScale(Vector3.one * 0.15f, 0.3f, 10, 1);
        }

        public void AnimatePreviewMode(bool isPlaying)
        {
            if (isPlaying)
            {
                _mainCamera.transform.DOMoveZ(_originalCamPos.z - _previewZoomDist, 0.6f).SetEase(Ease.OutBack);
                _breathingTweener.timeScale = 2.5f;
            }
            else
            {
                _mainCamera.transform.DOMoveZ(_originalCamPos.z, 0.5f).SetEase(Ease.OutQuad);
                _breathingTweener.timeScale = 1.0f;
            }
        }
        // 구체(Player)의 실시간 위치 반환
        public Vector3 GetSpherePosition()
        {
            // _sphereObj는 MainView가 가지고 있는 구체 Transform 변수명
            return _sphereObj.position;
        }
        public void AnimateGameEnd()
        {
            ShowRing(false); // 🔕 [추가] 게임 끝나면 링 끄기
            _baseScale = Vector3.one;
            _sphereMat.DOColor(Color.white, 0.5f);
            _mainCamera.DOColor(Color.black, 0.5f);
            _sphereObj.DOScale(_baseScale, 0.8f).SetEase(Ease.OutBack)
                .OnComplete(() => {
                    AnimatePreviewMode(false);
                    StartBreathing();
                });
        }
    }
}
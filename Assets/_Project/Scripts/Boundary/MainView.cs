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

        private Vector3 _velocity;
        private bool _isDragging = false;
        private Vector3 _minBounds;
        private Vector3 _maxBounds;
        private float _sphereRadius = 0.5f; // 구체 반지름
        private bool _isTransitioning = false;
        private float _currentManualScale = 1.0f;
        // 🚨 [추가] 핀치 유도 애니메이션을 제어할 변수
        private Sequence _guideSeq;
        public bool IsTransitioning => _isTransitioning;
     
        private Vector3 _centerPos = Vector3.zero; // 복귀할 위치 (0,0,0)
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
            // 화면 경계 계산 (벽 튕기기용)
            float vertExtent = _mainCamera.orthographicSize;
            float horzExtent = vertExtent * Screen.width / Screen.height;
            _minBounds = new Vector3(-horzExtent + _sphereRadius, -vertExtent + _sphereRadius, 0);
            _maxBounds = new Vector3(horzExtent - _sphereRadius, vertExtent - _sphereRadius, 0);
        }
        // ♻️ 물리 연산 (Update에 추가)
        private void Update()
        {
            // 드래그 중이 아니고, 게임 중이라면 -> 중앙으로 복귀
            if (!_isDragging && !_isTransitioning)
            {
                // Lerp로 부드럽게 (재조정 개념)
                // Time.deltaTime * 5f : 숫자가 클수록 빨리 복귀
                _sphereObj.position = Vector3.Lerp(_sphereObj.position, Vector3.zero, Time.deltaTime * 5.0f);
            }
        }
        // 🕹️ 1:1 절대 좌표 이동 (GameController가 호출)
        public void MoveSphereDirectly(Vector2 screenPos)
        {
            if (_isTransitioning) return;

            // 화면 좌표(Screen) -> 월드 좌표(World) 변환
            Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f)); // z=10 (카메라 앞)
            worldPos.z = 0f; // 2D 평면 고정

            _sphereObj.position = worldPos;
        }
        // 🕹️ GameController에서 호출할 함수들
        public void OnDragStart()
        {
            _isDragging = true;
            _sphereObj.DOKill(); // 복귀 중이었다면 즉시 중단 (사용자 우선)
        }

        public void OnDragEnd()
        {
            _isDragging = false;
        }
        // ✅ [추가] 인터페이스 구현: 링 끄고 켜기
        public void ShowRing(bool show)
        {
            if (_lifeRingView != null)
                _lifeRingView.gameObject.SetActive(show);
        }
        // 💥 [핵심] 타격감 함수 (GameController가 호출)
        // Hit 성공 시 호출됨
        // 💥 타격감 함수 (GameController가 호출)
        // MainView.cs

        public void OnNoteHitSuccess(float fuelRatio)
        {
            // 1. 오수 모드 확인
            bool isOsuMode = (_lifeRingView != null && !_lifeRingView.gameObject.activeSelf);

            if (isOsuMode)
            {
                // ⚔️ [오수 모드]
                _sphereObj.DOKill(true);

                // 1. 미세 진동
                _sphereObj.DOPunchScale(Vector3.one * 0.05f, 0.1f, 10, 1);

                // 2. 하얗게 번쩍! (수동 설정)
                _sphereRenderer.color = Color.white;

                // 🚨 [수정] 확장 메서드 오류 해결 -> DOTween.To 사용 (무조건 작동)
                // 문법: DOTween.To(getter, setter, 목표값, 시간)
                DOTween.To(() => _sphereRenderer.color, x => _sphereRenderer.color = x, Color.cyan, 0.2f);

                // 3. 카메라 쉐이크
                _mainCamera.transform.DOShakePosition(0.1f, 0.2f, 20);
            }
            else
            {
                // ⭕ [링 모드]
                _sphereObj.DOKill(true);
                _sphereObj.DOPunchScale(Vector3.one * _punchPower, 0.2f, 10, 1);

                if (_lifeRingView != null) _lifeRingView.OnHitEffect();
                _mainCamera.transform.DOShakePosition(0.1f, 0.1f, 10);
            }
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
        // 1. 🕳️ 오수 모드 끝 (핀치 유도 시작)
        public void AnimatePortalClosingReady()
        {
            _sphereObj.DOKill();
            _sphereMat.DOColor(Color.red, 0.5f);
            _sphereObj.DOMove(Vector3.zero, 0.5f).SetEase(Ease.OutExpo);

            // 🚨 [수정] 시퀀스를 변수(_guideSeq)에 저장해야 나중에 끌 수 있음
            _guideSeq?.Kill(); // 혹시 켜져 있으면 끄고
            _guideSeq = DOTween.Sequence();

            // 커졌다가(2.0) -> 작아지는(0.0) 연출 반복
            _guideSeq.Append(_sphereObj.DOScale(Vector3.one * 2.0f, 0f));
            _guideSeq.Append(_sphereObj.DOScale(Vector3.zero, 1.0f).SetEase(Ease.OutQuad));
            _guideSeq.SetLoops(-1);
        }
        // 링 모드로 복귀 (축소)
        // 2. 🌌 링 모드로 복귀 (핀치 유도 종료 & 크기 복구)
        public void AnimateExitOsuMode()
        {
            // 🚨 [핵심 수정] 핀치 유도 애니메이션 즉시 사살
            _guideSeq?.Kill();
            _sphereObj.DOKill(); // 그 외 구체에 걸린 모든 트윈 정지

            Sequence seq = DOTween.Sequence();

            // A. 색상 복구 (배경 하양, 구체 검정)
            seq.Append(_mainCamera.DOColor(Color.white, 0.5f));
            seq.Join(_sphereMat.DOColor(Color.black, 0.5f)); // DOTween.To 안 써도 머티리얼엔 잘 먹힘

            // B. 크기 복구 (중요!)
            // 오수 모드에서 0.15f로 줄였던 _baseScale을 다시 링 모드 크기(0.5f ~ 1.0f)로 되돌려야 함
            // 여기선 기본값 0.5f로 설정 (나중에 FireService가 업데이트해줌)
            _baseScale = Vector3.one * 0.5f;

            // C. 구체를 원래 크기(_baseScale)로 부드럽게 키움
            seq.Join(_sphereObj.DOScale(_baseScale, 0.5f).SetEase(Ease.OutBack));

            seq.OnComplete(() =>
            {
                Debug.Log("🌌 Back to Ring Mode");
                StartBreathing(); // 링 모드 숨쉬기 시작
            });
        }
        // ⚔️ 오수 모드 진입 확정
        public void AnimateEnterOsuMode()
        {
            _sphereObj.DOKill();
            _breathingTweener?.Kill();

            Sequence seq = DOTween.Sequence();

            // 1. 줌인 연출 (기존 유지)
            seq.Append(_sphereObj.DOScale(Vector3.one * 50f, 0.4f).SetEase(Ease.InExpo)); // 잠깐 커지는 연출
            seq.Append(_mainCamera.transform.DOMoveZ(_originalCamPos.z + 5f, 0.4f).SetEase(Ease.InExpo));

            seq.AppendCallback(() =>
            {
                _mainCamera.backgroundColor = Color.black;
                _sphereMat.color = Color.cyan; // 네온 색상

                // 🚨 [여기 수정!] 구체 크기 조절
                // 0.25f -> 0.15f (더 작게! 마우스 커서 느낌)
                _baseScale = Vector3.one * 0.15f;
                _sphereObj.localScale = _baseScale;

                if (_portalEffect != null) _portalEffect.Stop();
            });

            seq.OnComplete(() => {
                Debug.Log("⚔️ View: Osu Mode Visuals Ready");
            });
        }
        // 🕹️ [추가] 구체 이동 함수
        public void MoveSphere(Vector2 screenDelta)
        {
            if (_isTransitioning) return;

            // 1:1 이동을 위해 ScreenDelta를 WorldDelta로 변환
            Vector3 worldDelta = _mainCamera.ScreenToWorldPoint(new Vector3(screenDelta.x, screenDelta.y, 0))
                                 - _mainCamera.ScreenToWorldPoint(Vector3.zero);
            worldDelta.z = 0; // 2D게임이므로 Z축 고정

            // 위치 이동
            _sphereObj.position += worldDelta;

            // 드래그 중에는 속도를 계속 계산 (놓았을 때 날아가기 위해)
            // 프레임 보정을 위해 Time.deltaTime으로 나눔 (단, 너무 크면 제한)
            _velocity = worldDelta / Time.deltaTime;
            _velocity = Vector3.ClampMagnitude(_velocity, 20f); // 최대 속도 제한
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
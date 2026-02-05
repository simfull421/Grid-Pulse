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
        [SerializeField] private MeshRenderer _sphereRenderer;

        [Header("Animation Settings")]
        [SerializeField] private float _breathingScale = 1.1f;

        // 🔍 [수정] 2.0 -> 7.0으로 대폭 증가 (확실한 차이)
        [SerializeField] private float _previewZoomDist = 7.0f;

        private Tweener _breathingTweener;
        private Material _sphereMat;
        private Vector3 _originalSphereScale;
        private Vector3 _originalCamPos;

        // 🎨 초기 테마 색상 (앱 켤 때 정해짐)
        private Color _startThemeColor;


        private bool _isTransitioning = false; // 🔒 상태 잠금 (중복 실행 방지)
        private float _currentManualScale = 1.0f; // 현재 수동 스케일
        public void Initialize()
        {
            if (_sphereObj == null) _sphereObj = transform;
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_sphereRenderer == null) _sphereRenderer = _sphereObj.GetComponent<MeshRenderer>();

            _sphereMat = _sphereRenderer.material;
            _originalSphereScale = Vector3.one; // 구체 기본 크기 1
            _originalCamPos = _mainCamera.transform.position;

            // 🎲 [로직] 시작 시 흑/백 테마 랜덤 결정
            // 0이면 블랙 테마(배경 검정, 구체 흰색), 1이면 화이트 테마(배경 흰색, 구체 검정)
            bool startWithBlack = Random.Range(0, 2) == 0;

            _startThemeColor = startWithBlack ? Color.white : Color.black; // 구체의 색
            _mainCamera.backgroundColor = startWithBlack ? Color.black : Color.white; // 배경의 색

            _sphereMat.color = _startThemeColor;

            // 숨쉬기 시작
            StartBreathing();
        }

        // 🔄 Main -> Stage (배경화 & 앨범 등장)
        public void AnimateMainToStage(Color firstAlbumColor)
        {
            _breathingTweener?.Pause();

            Sequence seq = DOTween.Sequence();

            // 1. 구체가 커지면서 화면을 덮음
            seq.Append(_sphereObj.DOScale(Vector3.one * 50f, 0.6f).SetEase(Ease.InExpo));

            // 2. 덮었을 때 배경색 변경 및 구체 리셋
            seq.AppendCallback(() =>
            {
                // 배경을 현재 구체 색(StartThemeColor)으로 변경
                _mainCamera.backgroundColor = _sphereMat.color;

                // 구체는 작아진 상태에서 앨범 색상으로 옷을 갈아입음
                _sphereObj.localScale = Vector3.zero;
                _sphereMat.color = firstAlbumColor;
            });

            // 3. 앨범 구체 등장 (Pop!)
            seq.Append(_sphereObj.DOScale(_originalSphereScale, 0.4f).SetEase(Ease.OutBack));

            seq.OnComplete(() => StartBreathing());
        }

        // 🔙 Stage -> Main (복귀)
        public void AnimateStageToMain(Color mainThemeColor)
        {
            _breathingTweener?.Pause();

            Sequence seq = DOTween.Sequence();

            // 1. 앨범 구체 축소
            seq.Append(_sphereObj.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack));

            // 2. 배경과 구체 색상을 초기 테마로 복구
            seq.AppendCallback(() =>
            {
                // 초기 상태로 복구 (배경 <-> 구체 색상 반전 관계 유지)
                Color bgCol = _mainCamera.backgroundColor; // 현재 배경색(이게 곧 돌아올 구체색)

                // 배경을 다시 원래 반대색으로 돌리기 위해 계산이 복잡하니,
                // 그냥 Initialize 때 정했던 색상 조합으로 강제 복구합니다.
                _sphereMat.color = _startThemeColor;
                _mainCamera.backgroundColor = (_startThemeColor == Color.white) ? Color.black : Color.white;

                // 구체를 배경 크기만큼 키워둠 (줄어들 준비)
                _sphereObj.localScale = Vector3.one * 50f;
            });

            // 3. 구체가 작아지며 원래 자리로
            seq.Append(_sphereObj.DOScale(_originalSphereScale, 0.6f).SetEase(Ease.OutExpo));

            seq.OnComplete(() => StartBreathing());
        }

        public void UpdateAlbumVisual(MusicData data)
        {
            _sphereMat.DOColor(data.ThemeColor, 0.3f);
            _sphereObj.DOPunchScale(Vector3.one * 0.15f, 0.3f, 10, 1);
        }

        // ⏯️ [수정] 프리뷰 모드 차이 극대화
        public void AnimatePreviewMode(bool isPlaying)
        {
            if (isPlaying)
            {
                // 1. 카메라가 훨씬 뒤로 빠짐 (7.0f)
                _mainCamera.transform.DOMoveZ(_originalCamPos.z - _previewZoomDist, 0.6f)
                    .SetEase(Ease.OutBack);

                // 2. 구체가 음악에 맞춰 더 빠르게 숨쉼
                _breathingTweener.timeScale = 2.5f;
            }
            else
            {
                // 원상 복귀
                _mainCamera.transform.DOMoveZ(_originalCamPos.z, 0.5f)
                    .SetEase(Ease.OutQuad);

                _breathingTweener.timeScale = 1.0f;
            }
        }

        public void AnimateGameStart()
        {
            _breathingTweener?.Pause();
            Sequence seq = DOTween.Sequence();
            // 게임 시작 시 구체가 배경을 덮음
            seq.Append(_sphereObj.DOScale(Vector3.one * 100f, 0.8f).SetEase(Ease.InExpo));
            seq.Join(_mainCamera.transform.DOShakePosition(0.5f, 0.5f, 20));
        }

        public void AnimateGameEnd()
        {
            _sphereObj.DOScale(_originalSphereScale, 0.8f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => {
                    _breathingTweener?.Play();
                    AnimatePreviewMode(false);
                });
        }

        private void StartBreathing()
        {
            _breathingTweener?.Kill();
            _breathingTweener = _sphereObj
                .DOScale(_originalSphereScale * _breathingScale, 1.0f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
        // ✋ 수동 조절 (InputAnalyzer가 Delta를 줄 때마다 호출)
        public void SetInteractiveScale(float delta)
        {
            if (_isTransitioning) return; // 변신 중엔 조작 금지

            _breathingTweener?.Pause(); // 숨쉬기 멈춤

            // 현재 스케일에 델타를 더함 (감도 조절)
            _currentManualScale += delta * 2.0f;

            // 🛑 [버그 수정] 스케일이 음수가 되면 마름모/뒤집힘 현상 발생 -> Clamp 필수
            // 최소 0.5배 ~ 최대 50배
            _currentManualScale = Mathf.Clamp(_currentManualScale, 0.5f, 50.0f);

            _sphereObj.localScale = Vector3.one * _currentManualScale;
        }

        // 👌 손을 뗐을 때 (결정)
        public void CommitTransition(bool isZoomIn)
        {
            if (_isTransitioning) return;
            _isTransitioning = true; // 🔒 잠금 (중복 실행 방지)

            // 1. 확대 진입 (Main -> StageSelect or GameStart)
            if (isZoomIn)
            {
                Sequence seq = DOTween.Sequence();
                // 이미 커져있는 상태(_currentManualScale)에서 60까지 확실하게 확대
                seq.Append(_sphereObj.DOScale(Vector3.one * 60f, 0.5f).SetEase(Ease.OutExpo));

                seq.AppendCallback(() =>
                {
                    // 배경을 현재 구체 색으로 덮음
                    _mainCamera.backgroundColor = _sphereMat.color;

                    // 구체는 작아져서 앨범 색상으로 준비
                    _sphereObj.localScale = Vector3.zero;

                    // (주의: 여기서 앨범 색상은 Controller가 관리하므로, 일단 회색이나 기본값으로 둠)
                    // 실제 색상은 Controller가 UpdateAlbumVisual로 갱신해줄 것임
                    _sphereMat.color = Color.gray;
                });

                // 앨범 구체 등장
                seq.Append(_sphereObj.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack));

                seq.OnComplete(() => {
                    _isTransitioning = false; // 🔓 잠금 해제
                    _currentManualScale = 1.0f;
                    StartBreathing();
                });
            }
            // 2. 축소 복귀 (Stage -> Main)
            else
            {
                // ✅ [여기 채워 넣음]
                Sequence seq = DOTween.Sequence();

                // 구체가 완전히 사라짐 (Zoom In의 역순)
                seq.Append(_sphereObj.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack));

                seq.AppendCallback(() =>
                {
                    // 배경색과 구체색을 초기 테마(StartTheme)로 복구
                    // 배경 <-> 구체 색상 반전 관계 복원
                    _sphereMat.color = _startThemeColor;
                    _mainCamera.backgroundColor = (_startThemeColor == Color.white) ? Color.black : Color.white;

                    // 구체를 화면 가득 채운 크기로 준비 (줄어들면서 등장하기 위해)
                    _sphereObj.localScale = Vector3.one * 50f;
                });

                // 큰 구체가 작아지며 원래 자리로 (1.0f 크기)
                seq.Append(_sphereObj.DOScale(Vector3.one, 0.6f).SetEase(Ease.OutExpo));

                seq.OnComplete(() => {
                    _isTransitioning = false; // 🔓 잠금 해제
                    _currentManualScale = 1.0f;
                    StartBreathing();
                });
            }
        }
        // ❌ 취소 (임계값 못 넘김) -> 띠용 하고 원래대로
        public void ResetScale()
        {
            if (_isTransitioning) return;

            _sphereObj.DOScale(_originalSphereScale, 0.3f).SetEase(Ease.OutBack)
                .OnComplete(() => {
                    _currentManualScale = 1.0f;
                    StartBreathing();
                });
        }
        // MainView 클래스 내부
        public void SetLifeScale(float ratio)
        {
            // 구체 크기 변경 (부드럽게)
            // _sphereObj가 없으면 에러나니 null 체크
            if (_sphereObj != null)
            {
                // 0.3초 동안 부드럽게 크기 변경
                _sphereObj.DOScale(Vector3.one * ratio, 0.3f).SetEase(Ease.OutBack);
            }
        }
    }


}
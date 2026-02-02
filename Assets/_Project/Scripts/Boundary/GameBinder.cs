using System.Collections;
using System.Collections.Generic;
using TouchIT.Control;
using TouchIT.Entity;
using UnityEngine;
using UnityEngine.UI; // [중요] Image 컴포넌트 사용을 위해 추가

namespace TouchIT.Boundary
{
    public class GameBinder : MonoBehaviour, IGameView
    {
        // ==========================================
        // [View References]
        // ==========================================
        [Header("Sub Views")]
        [SerializeField] private LifeRingView _lifeRingView;
        [SerializeField] private SphereView _sphereView;
        [SerializeField] private NoteView _notePrefab;
        [SerializeField] private SphereInteraction _sphereInteraction;
        [Header("Containers")]
        [SerializeField] private Transform _noteContainer;
        [SerializeField] private Transform _particleContainer; // [신규] 파티클 모아두는 곳
        [Header("UI & Visuals")]
        // [신규] 오버워치 스타일 궁 게이지 (Filled 타입의 Image 필요)
        [SerializeField] private Image _comboGaugeImage;
        // [신규] 테마 변경 카운트다운용 라인 렌더러
        [SerializeField] private LineRenderer _timerLineRenderer;

        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem _shockwavePrefab;
        [SerializeField] private ParticleSystem _groggyBubblePrefab;
        [SerializeField] private SpriteRenderer _hitZoneVisual;
        [SerializeField] private ParticleSystem _sparkPrefab; // [신규] 타격 스파크 (번개/불꽃)
        [SerializeField] private ParticleSystem _holdLoopEffectPrefab; // [신규] 루프형 스파크 (Inspector 할당)

        [Header("Pooling")]
        [SerializeField] private int _initialPoolSize = 30;

        // ==========================================
        // [Internal State]
        // ==========================================
        private List<INoteView> _activeNotes = new List<INoteView>();
        private Queue<NoteView> _notePool = new Queue<NoteView>();
        // [최적화] 간단한 파티클 풀
        private Queue<ParticleSystem> _sparkPool = new Queue<ParticleSystem>();
        public float RingRadius => _lifeRingView != null ? _lifeRingView.Radius : 3.0f;
        // [신규] 줌 효과 변수
        private float _defaultCamSize;
        private Camera _mainCam;
        private Coroutine _zoomCoroutine;
        private ParticleSystem _activeHoldEffect; // 인스턴스 저장용
        public void Initialize()
        {
            if (_lifeRingView) _lifeRingView.Initialize();
            if (_sphereView) _sphereView.Initialize();
            if (_sphereInteraction) _sphereInteraction.Initialize();

            // UI 초기화
            if (_comboGaugeImage) _comboGaugeImage.fillAmount = 0f;
            // [Fix] 핑크색 링 해결: 머터리얼이 없으면 강제로 기본 스프라이트 머터리얼 할당
            if (_timerLineRenderer)
            {
                if (_timerLineRenderer.sharedMaterial == null)
                    _timerLineRenderer.material = new Material(Shader.Find("Sprites/Default"));

                _timerLineRenderer.positionCount = 0;
                _timerLineRenderer.enabled = false;
            }
            // 파티클 미리 생성 (10개 정도)
            for (int i = 0; i < 10; i++)
            {
                var p = Instantiate(_sparkPrefab, _particleContainer);
                p.gameObject.SetActive(false);
                _sparkPool.Enqueue(p);
            }
            // [신규] 홀드 이펙트 미리 생성해두고 꺼두기 (Pooling 개념)
            if (_holdLoopEffectPrefab)
            {
                _activeHoldEffect = Instantiate(_holdLoopEffectPrefab, _particleContainer);

                // 위치는 12시 방향(판정선) 고정
                _activeHoldEffect.transform.localPosition = new Vector3(0, RingRadius, 0);
                _activeHoldEffect.gameObject.SetActive(false);

                // 파티클 설정 강제 (Looping이 켜져 있어야 함)
                var main = _activeHoldEffect.main;
                main.loop = true;
            }
            InitializePool();
        }

        // ==========================================
        // [IGameView Implementation]
        // ==========================================
        public void SpawnNote(NoteData data)
        {
            NoteView note = GetNoteFromPool();
            note.Initialize(data, RingRadius, this);
            _activeNotes.Add(note);
        }
        // [신규] 인터페이스 구현
        public void SetHoldEffect(bool isHolding)
        {
            if (_activeHoldEffect == null) return;

            if (isHolding)
            {
                if (!_activeHoldEffect.gameObject.activeSelf)
                {
                    _activeHoldEffect.gameObject.SetActive(true);
                    _activeHoldEffect.Play();
                }
            }
            else
            {
                if (_activeHoldEffect.gameObject.activeSelf)
                {
                    _activeHoldEffect.Stop();
                    _activeHoldEffect.gameObject.SetActive(false);
                }
            }
        }
        public List<INoteView> GetActiveNotes() => _activeNotes;
        public void ReturnNote(INoteView noteInterface)
        {
            NoteView note = noteInterface as NoteView;
            if (note != null && note.gameObject.activeSelf)
            {
                note.gameObject.SetActive(false);
                if (_activeNotes.Contains(note)) _activeNotes.Remove(note);
                _notePool.Enqueue(note);
            }
        }

        public void SetTheme(NoteColor mode)
        {
            if (_sphereView) _sphereView.SetColor(mode);
            if (_lifeRingView) _lifeRingView.SetColor(mode);

            // 1. 배경색 설정
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                // White 모드면 -> 배경을 밝게 (노트는 검게 나올 예정)
                // Black 모드면 -> 배경을 어둡게 (노트는 희게 나올 예정)
                mainCam.backgroundColor = (mode == NoteColor.White)
                    ? new Color(0.9f, 0.9f, 0.92f) // 밝은 회색 배경
                    : new Color(0.1f, 0.1f, 0.12f); // 어두운 남색 배경
            }

            // 2. 히트존(판정선) 색상 (배경과 반대색)
            if (_hitZoneVisual != null)
            {
                _hitZoneVisual.color = (mode == NoteColor.White)
                    ? Color.black // 밝은 배경엔 검은 판정선
                    : Color.white; // 어두운 배경엔 흰 판정선
            }

            // 3. 타이머 라인 색상
            if (_timerLineRenderer != null)
            {
                Color c = (mode == NoteColor.White) ? Color.black : Color.white; // 반대색
                _timerLineRenderer.startColor = c;
                _timerLineRenderer.endColor = c;
            }
        }

        public void ClearAllNotes(bool success)
        {
            for (int i = _activeNotes.Count - 1; i >= 0; i--)
            {
                var note = _activeNotes[i];
                if (success) PlayHitEffect(note.Position, note.Color);
                ReturnNote(note);
            }
        }

        public void PlayHitEffect(Vector3 position, NoteColor color)
        {// [신규] 줌 아웃 펀치 효과 실행
            if (_zoomCoroutine != null) StopCoroutine(_zoomCoroutine);
            _zoomCoroutine = StartCoroutine(HitZoomEffect());
            // 2. [신규] 스파크 이펙트 (풀링 사용)
            if (_sparkPool.Count > 0)
            {
                var spark = _sparkPool.Dequeue();
                spark.transform.position = Vector3.up * RingRadius; // 항상 12시 방향 고정
                spark.transform.rotation = Quaternion.identity; // 필요시 회전
                spark.gameObject.SetActive(true);

                var main = spark.main;
                // White 테마 -> 검은 스파크, Black 테마 -> 흰 스파크 (또는 Cyan)
                main.startColor = (color == NoteColor.White) ? Color.black : Color.cyan;

                spark.Play();
                StartCoroutine(ReturnSparkToPool(spark));
            }

            // 3. 쇼크웨이브 (기존 유지 - 원하면 얘도 풀링 가능)
            if (_shockwavePrefab)
            {
                var shock = Instantiate(_shockwavePrefab, Vector3.zero, Quaternion.identity, _particleContainer);
                var main = shock.main;
                main.startColor = (color == NoteColor.White) ? Color.white : Color.black;
                // [Fix] 색상 분기 처리 강화
                if (color == NoteColor.White) main.startColor = Color.white;
                else if (color == NoteColor.Black) main.startColor = Color.black;
                else main.startColor = Color.cyan; // Cosmic 등 기타 색상은 Cyan으로 처리 (홀드 노트용)
                // 위치 회전 로직 유지
                if (position != Vector3.zero)
                {
                    float angle = Mathf.Atan2(position.y, position.x) * Mathf.Rad2Deg;
                    shock.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
                }
                Destroy(shock.gameObject, 1.0f);
            }
        }
        private IEnumerator ReturnSparkToPool(ParticleSystem spark)
        {
            yield return new WaitForSeconds(0.5f);
            spark.gameObject.SetActive(false);
            _sparkPool.Enqueue(spark);
        }
        // [신규] 줌 아웃 -> 복귀 (Punch)
        private IEnumerator HitZoomEffect()
        {
            if (_mainCam == null) yield break;

            // 1. 줌 아웃 (사이즈 키움)
            float targetSize = _defaultCamSize + 0.2f; // 0.2만큼 뒤로
            float elapsed = 0f;
            float duration = 0.05f; // 아주 빠르게

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _mainCam.orthographicSize = Mathf.Lerp(_defaultCamSize, targetSize, elapsed / duration);
                yield return null;
            }

            // 2. 복귀 (탄력 있게)
            elapsed = 0f;
            duration = 0.15f; // 돌아올 땐 조금 천천히

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _mainCam.orthographicSize = Mathf.Lerp(targetSize, _defaultCamSize, elapsed / duration);
                yield return null;
            }

            _mainCam.orthographicSize = _defaultCamSize;
        }
        public void ReduceLife()
        {
            if (_lifeRingView) _lifeRingView.ReduceLife();
        }

        // ==========================================
        // [신규 구현] 누락되었던 인터페이스 메서드들
        // ==========================================

        // 1. 그로기 상태 구체 이동
        public void UpdateSpherePosition(Vector3 pos)
        {
            if (_sphereInteraction)
            {
                _sphereInteraction.UpdatePosition(pos);
            }
        }

        // 2. 콤보 게이지 업데이트 (UI Image Fill 사용)
        public void UpdateComboGauge(float fillAmount)
        {
            if (_comboGaugeImage)
            {
                _comboGaugeImage.fillAmount = fillAmount;
            }
        }

        // 3. 타이머 링 연출 (LineRenderer로 원 그리기)
        public void SetVisualTimer(float fillAmount, bool isActive)
        {
            if (_timerLineRenderer == null) return;

            _timerLineRenderer.enabled = isActive;
            if (!isActive) return;

            // 원 그리기 로직 (360도 * fillAmount 만큼만 그림)
            int segments = 60;
            float totalAngle = 360f * fillAmount;

            _timerLineRenderer.positionCount = segments + 1;
            _timerLineRenderer.useWorldSpace = false;
            // 링보다 약간 바깥에 그림
            float radius = RingRadius + 0.2f;

            // 12시(90도)부터 시계방향으로 줄어들게 하기
            // 시작: 90도, 종료: 90 - totalAngle
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float currentAngleDeg = 90f - (totalAngle * t);
                float rad = currentAngleDeg * Mathf.Deg2Rad;

                float x = Mathf.Cos(rad) * radius;
                float y = Mathf.Sin(rad) * radius;

                _timerLineRenderer.SetPosition(i, new Vector3(x, y, 0));
            }
        }

        // 그로기 모드 설정
        public void SetGroggyMode(bool isActive)
        {
            if (_sphereInteraction) _sphereInteraction.SetGroggyVisual(isActive);
        }

        public void TriggerGroggyEffect()
        {
            ClearAllNotes(true);
            // 추가적인 전체 화면 쉐이크나 이펙트가 있다면 여기서 실행
        }

        public void PlayGroggyBubbleEffect(Vector3 centerPos, NoteColor theme)
        {
            if (_groggyBubblePrefab)
            {
                Vector3 spawnPos = centerPos + (Vector3)(Random.insideUnitCircle * 1.5f);
                var bubble = Instantiate(_groggyBubblePrefab, spawnPos, Quaternion.identity);
                var main = bubble.main;
                main.startColor = (theme == NoteColor.White) ? Color.black : Color.white;
                Destroy(bubble.gameObject, 1.0f);
            }
        }

        // ==========================================
        // [Pooling System]
        // ==========================================
        private void InitializePool()
        {
            for (int i = 0; i < _initialPoolSize; i++)
            {
                NoteView note = Instantiate(_notePrefab, _noteContainer);
                note.gameObject.SetActive(false);
                _notePool.Enqueue(note);
            }
        }

        private NoteView GetNoteFromPool()
        {
            if (_notePool.Count > 0)
            {
                NoteView note = _notePool.Dequeue();
                note.gameObject.SetActive(true);
                return note;
            }
            return Instantiate(_notePrefab, _noteContainer);
        }
    }
}
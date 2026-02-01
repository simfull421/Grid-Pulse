using UnityEngine;
using System.Collections.Generic;
using TouchIT.Entity;

namespace TouchIT.Control
{
    public class GameMain : MonoBehaviour
    {
        // =========================================================
        // 1. Dependencies (인터페이스 의존성)
        // =========================================================
        private IGameView _view;
        private IAudioManager _audio;
        private RhythmEngine _engine;

        // [정석 구현] Unity Inspector는 Interface를 직접 할당할 수 없으므로,
        // MonoBehaviour로 받고 내부에서 Interface로 변환하여 사용합니다.
        [Header("Dependencies")]
        [SerializeField] private MonoBehaviour _hitSensorRef;
        private IHitSensor _hitSensor;

        // =========================================================
        // 2. State Variables (상태 변수)
        // =========================================================
        private NoteColor _currentMode = NoteColor.White;

        // Input
        private Vector2 _startPos;
        private bool _isTouching;
        private float _touchTime;

        // Groggy Mode
        private bool _isGroggyMode = false;
        private int _comboCount = 0;
        private int _shakeScore = 0;

        // Phase / Swipe Timing
        private bool _isSwipeTiming = false;
        private float _phaseTimer = 0f;
        private const float PHASE_LENGTH = 15.0f;

        // =========================================================
        // 3. Initialization
        // =========================================================
        public void Initialize(IGameView view, IAudioManager audio, BeatLibrary lib)
        {
            _view = view;
            _audio = audio;

            // 엔진 초기화
            _engine = new RhythmEngine();
            _engine.Initialize(view, lib);

            // 센서 인터페이스 연결 (Safety Check)
            _hitSensor = _hitSensorRef as IHitSensor;
            if (_hitSensor == null)
            {
                Debug.LogError("❌ GameMain: Hit Sensor가 연결되지 않았거나 IHitSensor를 구현하지 않았습니다!");
            }

            // 초기 테마 설정
            _view.SetTheme(_currentMode);
        }

        // =========================================================
        // 4. Game Loop
        // =========================================================
        private void Update()
        {
            if (_view == null || _isGroggyMode) return;

            // --- A. 타이머 관리 ---
            _phaseTimer += Time.deltaTime;

            // 스와이프 경고 타이밍 (15초)
            if (_phaseTimer >= PHASE_LENGTH && !_isSwipeTiming)
            {
                _isSwipeTiming = true;
                Debug.Log("⚠️ SWIPE TIMING! 테마를 변경하세요!");
            }

            // 강제 변경 타이밍 (15초 + 3초 여유)
            if (_isSwipeTiming && _phaseTimer >= PHASE_LENGTH + 3.0f)
            {
                ForceChangeTheme();
            }

            // --- B. 게임 로직 업데이트 ---
            if (!_isGroggyMode)
            {
                _engine.OnUpdate(); // 노트 생성
                UpdateNotes();      // 노트 이동 및 미스 체크
                HandleInput();      // 입력 처리
            }
        }

        // =========================================================
        // 5. Input Handling
        // =========================================================
        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _startPos = Input.mousePosition;
                _touchTime = Time.time;
                _isTouching = true;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (!_isTouching) return;
                _isTouching = false;

                Vector2 delta = (Vector2)Input.mousePosition - _startPos;
                float dragDuration = Time.time - _touchTime;

                // [수정] 판정 완화
                // 거리: 80 -> 50 (조금만 움직여도 인정)
                // 시간: 0.3초 -> 0.5초 (조금 천천히 긋더라도 인정)
                if (delta.magnitude > 50f && dragDuration < 0.5f)
                {
                    if (_isSwipeTiming)
                    {
                        SuccessThemeChange();
                    }
                    // 타이밍 아닐 때 스와이프하면 그냥 무시 (탭 판정 안 가게)
                }
                else
                {
                    CheckHit(); // 탭 (짧고 적게 움직임)
                }
            }
        }

        // =========================================================
        // 6. Theme Logic (Phase)
        // =========================================================
        private void SuccessThemeChange()
        {
            Debug.Log("🎉 스와이프 성공!");
            _view.ClearAllNotes(true); // 성공 이펙트와 함께 삭제
            _audio.PlaySfx("Swipe");
            ChangeThemeLogic();
        }

        private void ForceChangeTheme()
        {
            Debug.Log("💢 시간 초과! 강제 변경");
            _view.ReduceLife();
            _view.ClearAllNotes(false); // 이펙트 없이 삭제
            _audio.PlaySfx("Miss");
            ChangeThemeLogic();
        }

        private void ChangeThemeLogic()
        {
            // Enum 순환 로직
            if (_currentMode == NoteColor.White) _currentMode = NoteColor.Black;
            else if (_currentMode == NoteColor.Black) _currentMode = NoteColor.Cosmic;
            else _currentMode = NoteColor.White;

            _view.SetTheme(_currentMode);

            // [중요] 엔진에게 "이제 이 색깔 노트만 뽑아!"라고 명령
            _engine.SetCurrentPhase(_currentMode);

            _phaseTimer = 0f;
            _isSwipeTiming = false;
        }

        // =========================================================
        // 7. Hit Logic (Core Gameplay)
        // =========================================================
        private void CheckHit()
        {
            if (_hitSensor == null) return;

            // [정석] 구체적인 NoteView가 아니라 인터페이스(INoteView)를 받습니다.
            INoteView target = _hitSensor.GetBestHitNote(_currentMode);

            if (target != null)
            {
                // 판정 성공
                _view.PlayHitSound();
                _view.PlayHitEffect(target.Position, target.Color); // 인터페이스 속성 사용

                // 센서 및 풀링 시스템 처리
                _hitSensor.RemoveNote(target);
                target.ReturnToPool();

                // 콤보 및 그로기 게이지
                _comboCount++;
                if (_comboCount >= 10)
                {
                    _comboCount = 0;
                    ToggleGroggyState(true);
                }
            }
        }

        private void UpdateNotes()
        {
            float dt = Time.deltaTime;
            // 활성 노트 리스트를 가져와서 업데이트
            var activeNotes = new List<INoteView>(_view.GetActiveNotes());

            foreach (var note in activeNotes)
            {
                note.UpdateRotation(dt);

                // 화면 밖으로 나가거나 각도를 너무 지나치면 Miss 처리
                // (12시=90도, 반시계 회전. 45도 미만이면 놓친 것으로 간주)
                if (note.CurrentAngle <= 45f)
                {
                    _view.ReduceLife();
                    _audio.PlaySfx("Miss");
                    note.ReturnToPool();
                }
            }
        }

        // =========================================================
        // 8. Groggy Mode Logic
        // =========================================================
        private void ToggleGroggyState(bool turnOn)
        {
            _isGroggyMode = turnOn;
            _view.SetGroggyMode(turnOn);

            if (turnOn)
            {
                Debug.Log("🔥 그로기 타임 발동!");
                _view.TriggerGroggyEffect();
                _shakeScore = 0;

                // 5초 후 자동 종료
                Invoke(nameof(EndGroggy), 5.0f);
            }
        }

        private void EndGroggy()
        {
            Debug.Log("그로기 종료.");
            ToggleGroggyState(false);
        }

        // SphereInteraction(View)에서 호출하는 함수
        public void AddShakeScore()
        {
            _shakeScore += 100;
        }

        // SphereInteraction에서 호출 (쿨타임 걸러진 소리 재생 요청)
        public void PlayShakeSound()
        {
            _audio.PlaySfx("Hit");
        }
    }
}
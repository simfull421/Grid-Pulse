using UnityEngine;
using System.Collections.Generic;
using TouchIT.Entity;
// using TouchIT.Boundary; // [삭제] Boundary 절대 참조 금지!

namespace TouchIT.Control
{
    public class GameMain : MonoBehaviour
    {
        // 구체적인 클래스 대신 인터페이스 사용
        private IGameView _view;
        private IAudioManager _audio;
        private RhythmEngine _engine;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            _engine = new RhythmEngine();
        }

        // Binder에 의해 호출됨 (의존성 주입)
        public void Initialize(IGameView view, IAudioManager audio)
        {
            _view = view;
            _audio = audio;
            _engine.Initialize(view, audio);
        }

        private void Update()
        {
            if (_view == null) return;

            _engine.OnUpdate();

            float dt = Time.deltaTime;

            // [중요] foreach 대신 for문을 역순으로 돕니다.
            // 도는 도중에 노트를 삭제(반납)해야 하기 때문입니다.
            var activeNotes = _view.GetActiveNotes() as List<INoteView>; // 리스트로 형변환 필요할 수 있음
                                                                         // 혹은 아래처럼 복사본을 만들어서 순회
            var noteList = new List<INoteView>(_view.GetActiveNotes());

            foreach (var note in noteList)
            {
                note.UpdateRotation(dt);

                // [수정] Miss 판정 로직 추가
                // 12시가 90도이고, 시계방향으로 각도가 줄어듭니다 (90 -> 80 -> ... -> 0)
                // 각도가 0도(3시 방향)보다 작아지면 완전히 지나간 것으로 판단
                // [수정] 각도가 90도보다 작아지면 (12시를 지나침) Miss
                // 판정 여유를 둬서 75도(1시 방향 쯤)까지 안 눌렀으면 미스로 처리
                if (note.CurrentAngle <= 75f)
                {
                    Debug.Log("MISS! (Time Over)");
                    _view.ReduceLife(1);
                    _audio.PlaySfx("Miss");
                    _view.ReturnNote(note);
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                CheckHit();
            }
        }

        private void CheckHit()
        {
            INoteView closestNode = null;
            float minDiff = float.MaxValue;
            float targetAngle = 90f;

            // 인터페이스를 통해 순회
            foreach (var note in _view.GetActiveNotes())
            {
                float diff = Mathf.Abs(Mathf.DeltaAngle(note.CurrentAngle, targetAngle));
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closestNode = note;
                }
            }

            if (closestNode != null && minDiff <= 15f)
            {
                Debug.Log($"HIT! (Diff: {minDiff:F2})");
                _view.PlayHitEffect();
                _audio.PlayNoteSound(closestNode.SoundIndex, 1);

                // 인터페이스를 넘겨서 반납 요청
                _view.ReturnNote(closestNode);
            }
            else
            {
                Debug.Log("MISS!");
                _view.ReduceLife(1);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // 12시 방향(90도) 기준으로 ±15도 부채꼴 그리기
            Gizmos.color = new Color(0, 1, 0, 0.3f); // 반투명 초록색
            Vector3 center = Vector3.zero;

            // 90도에서 -15도 뺀 곳(75도)부터 시작해서 30도만큼 그림
            // 유니티 Gizmos는 3D 기준이라 방향 계산이 좀 다를 수 있지만 대략적으로 확인
            // (Z축 회전 게임이므로 XY평면에 그려야 함)

            // 간단하게 선 2개로 표시
            float radius = 4.0f; // 링보다 조금 크게

            // 왼쪽 경계 (105도)
            Vector3 leftDir = new Vector3(Mathf.Cos(105 * Mathf.Deg2Rad), Mathf.Sin(105 * Mathf.Deg2Rad), 0);
            // 오른쪽 경계 (75도)
            Vector3 rightDir = new Vector3(Mathf.Cos(75 * Mathf.Deg2Rad), Mathf.Sin(75 * Mathf.Deg2Rad), 0);

            Gizmos.DrawLine(center, center + leftDir * radius);
            Gizmos.DrawLine(center, center + rightDir * radius);

            // 타겟 라인 (90도 - 12시)
            Gizmos.color = Color.red;
            Gizmos.DrawLine(center, center + Vector3.up * radius);
        }
#endif
    }

}
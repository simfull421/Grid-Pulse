using UnityEngine;

namespace TouchIT.Control
{
    public abstract class GameState
    {
        protected GameController Controller;
        public GameState(GameController controller)
        {
            this.Controller = controller;
        }

        public abstract void Enter();
        public abstract void Update();
        public abstract void Exit(); // [Fix] 필수 구현

        // 입력 이벤트 (가상 메서드로 정의하여 필요한 상태만 오버라이드)
        public virtual void OnTouch(Vector2 pos) { }
        public virtual void OnDrag(Vector2 pos) { } // [Fix] 추가됨
        public virtual void OnSwipe(Vector2 dir) { }
        public virtual void OnTouchUp() { } // 빈 가상 메서드 추가
    }
}
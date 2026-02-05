using TouchIT.Boundary; // INoteView가 있는 곳 (Boundary 어셈블리가 아니라 인터페이스 정의부)
// *중요: INoteView 인터페이스 파일은 Control이나 Core 어셈블리에 있어야 서로 참조 가능합니다.
// 만약 INoteView가 Boundary에 있다면, INoteView.cs 파일을 Control 폴더로 옮기세요.

namespace TouchIT.Control
{
    public interface INoteFactory
    {
        // 팩토리에게 노트를 달라고 요청하는 계약
        INoteView CreateNote();
        void ReturnNote(INoteView note);
    }
}
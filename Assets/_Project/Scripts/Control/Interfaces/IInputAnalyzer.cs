using System;
using UniRx;
using UnityEngine;

namespace TouchIT.Boundary
{
    public interface IInputAnalyzer
    {
        IObservable<Vector2> OnTap { get; }
        IObservable<float> OnPinch { get; } // 핀치 중 (델타값)
        IObservable<Unit> OnPinchEnd { get; } // 핀치 끝 (손 뗌) ⬅️ 추가
        IObservable<Vector2> OnDrag { get; }
    }
}
using System;

namespace GameEntity.Unity.Framework
{
    public interface ITimerSystem
    {
        TimerHandle Delay(float seconds, Action callback, bool unscaled = false);
        TimerHandle Every(float interval, Action<int> callback, int repeatCount = -1, bool unscaled = false);
        bool Cancel(TimerHandle handle);
        void CancelAll();
        bool Pause(TimerHandle handle);
        bool Resume(TimerHandle handle);
    }

}

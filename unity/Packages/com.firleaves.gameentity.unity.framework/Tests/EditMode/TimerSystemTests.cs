using NUnit.Framework;

namespace GameEntity.Unity.Framework.Tests
{
    public sealed class TimerSystemTests
    {
        [Test]
        public void Delay_FiresOnceAfterInterval()
        {
            var timer = new TimerSystemEntity();
            timer.Awake();
            var count = 0;
            timer.Delay(1f, () => count++);

            timer.Update(0.5f);
            Assert.AreEqual(0, count);

            timer.Update(0.5f);
            timer.Update(1f);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Every_FiresRepeatCountAndStops()
        {
            var timer = new TimerSystemEntity();
            timer.Awake();
            var count = 0;
            timer.Every(0.25f, tick => count = tick, repeatCount: 3);

            timer.Update(1f);
            timer.Update(1f);

            Assert.AreEqual(3, count);
        }

        [Test]
        public void Cancel_PreventsCallback()
        {
            var timer = new TimerSystemEntity();
            timer.Awake();
            var count = 0;
            var handle = timer.Delay(0.1f, () => count++);

            Assert.IsTrue(timer.Cancel(handle));
            timer.Update(1f);

            Assert.AreEqual(0, count);
        }

        [Test]
        public void PauseAndResume_ControlTimerProgress()
        {
            var timer = new TimerSystemEntity();
            timer.Awake();
            var count = 0;
            var handle = timer.Delay(1f, () => count++);

            Assert.IsTrue(timer.Pause(handle));
            timer.Update(2f);
            Assert.AreEqual(0, count);

            Assert.IsTrue(timer.Resume(handle));
            timer.Update(1f);

            Assert.AreEqual(1, count);
        }
    }
}

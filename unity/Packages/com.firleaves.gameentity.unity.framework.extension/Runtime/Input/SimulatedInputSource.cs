using UnityEngine;

namespace GameEntity.Unity.Framework.Extension
{
    public sealed class SimulatedInputSource : IFrameworkInputSource
    {
        private readonly float _speed;

        public SimulatedInputSource(float speed = 0.7f)
        {
            _speed = speed;
        }

        public bool TryReadInput(int frame, float time, out FrameworkInputFrame input)
        {
            var move = new Vector2(Mathf.Sin(time * _speed), Mathf.Cos(time * _speed * 0.71f));
            var look = new Vector2(Mathf.Sin(time * 0.23f), Mathf.Cos(time * 0.19f)) * 0.05f;
            input = new FrameworkInputFrame(
                frame,
                time,
                move,
                look,
                ButtonPulse(time, 3.2f),
                ButtonPulse(time, 4.5f),
                ButtonPulse(time, 3.8f),
                ButtonPulse(time, 6.2f),
                new FrameworkInputButton(Mathf.Sin(time * 0.6f) > 0.25f, false, false),
                ButtonPulse(time, 5.5f),
                FrameworkInputSourceKind.Simulated);
            return true;
        }

        private static FrameworkInputButton ButtonPulse(float time, float interval)
        {
            var pressed = Mathf.Repeat(time, interval) < 0.04f;
            return new FrameworkInputButton(pressed, pressed, false);
        }
    }
}

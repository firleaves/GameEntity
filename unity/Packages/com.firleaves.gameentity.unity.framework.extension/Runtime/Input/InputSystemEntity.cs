using System.Collections.Generic;
using GameEntity;

namespace GameEntity.Unity.Framework.Extension
{
    public sealed class InputSystemEntity : Entity, IAwake<IFrameworkInputSource>, IUpdate, IDestroy, IInputSystem
    {
        private readonly List<FrameworkInputFrame> _history = new List<FrameworkInputFrame>(180);
        private IFrameworkInputSource _source;
        private FrameworkInputFrame _latest;
        private int _frame;

        public int CurrentFrame => _frame;
        public FrameworkInputFrame LatestFrame => _latest;
        public FrameworkInputSourceKind SourceKind => _latest.SourceKind;
        public IReadOnlyList<FrameworkInputFrame> History => _history;

        public void Awake(IFrameworkInputSource source)
        {
            _source = source;
            _latest = FrameworkInputFrame.Empty(0, 0f);
        }

        public void Update(float deltaTime)
        {
            _frame++;
            var time = _latest.Time + deltaTime;
            if (_source != null && _source.TryReadInput(_frame, time, out var sourceFrame))
            {
                PushFrame(sourceFrame);
                return;
            }

            PushFrame(FrameworkInputFrame.Empty(_frame, time, SourceKind));
        }

        public void SetSource(IFrameworkInputSource source)
        {
            _source = source;
        }

        public void PushFrame(FrameworkInputFrame frame)
        {
            _latest = frame;
            _history.Add(frame);
            while (_history.Count > 180)
            {
                _history.RemoveAt(0);
            }
        }

        public bool TryConsumeLatest(out FrameworkInputFrame frame)
        {
            frame = _latest;
            return _latest.SourceKind != FrameworkInputSourceKind.None || _latest.HasAnyInput;
        }

        public InputSystemSnapshot GetSnapshot()
        {
            return new InputSystemSnapshot
            {
                CurrentFrame = _frame,
                SourceKind = SourceKind,
                LatestFrame = _latest,
                History = _history.ToArray()
            };
        }

        public void OnDestroy()
        {
            _source = null;
            _history.Clear();
            _latest = default;
            _frame = 0;
        }
    }
}

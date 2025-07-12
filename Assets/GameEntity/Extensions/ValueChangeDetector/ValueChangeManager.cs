using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GE.Extensions
{
    public class ValueChangeManager
    {
        private readonly List<IValueChangeDetector> _observers = new();

        public void Add<T>(Func<T> getter, Action<T> onChanged)
        {
            _observers.Add(new ValueObserver<T>(getter, onChanged));
        }

        public void Add(IValueChangeDetector observer)
        {
            _observers.Add(observer);
        }

        public void Evaluate()
        {
            foreach (var obs in _observers)
                obs.Evaluate();
        }
    }

    public class ValueObserver<T> : IValueChangeDetector
    {
        private readonly Func<T> _getter;
        private readonly Action<T> _onChanged;

        private T _lastValue;
        private bool _hasValue = false;

        public ValueObserver(Func<T> getter, Action<T> onChanged)
        {
            _getter = getter;
            _onChanged = onChanged;
        }

        public void Evaluate()
        {
            var currentValue = _getter();

            if (!_hasValue || !EqualityComparer<T>.Default.Equals(_lastValue, currentValue))
            {
                _lastValue = currentValue;
                _hasValue = true;
                _onChanged(currentValue);
            }
        }
    }


    public class MultiObserver<T1, T2> : IValueChangeDetector
    {
        private readonly Func<T1> _getter1;
        private readonly Func<T2> _getter2;
        private readonly Action<T1, T2> _onChanged;

        private T1 _last1;
        private T2 _last2;
        private bool _hasValue = false;

        public MultiObserver(Func<T1> getter1, Func<T2> getter2, Action<T1, T2> onChanged)
        {
            _getter1 = getter1;
            _getter2 = getter2;
            _onChanged = onChanged;
        }

        public void Evaluate()
        {
            var v1 = _getter1();
            var v2 = _getter2();

            if (!_hasValue || !EqualityComparer<T1>.Default.Equals(_last1, v1) ||
                !EqualityComparer<T2>.Default.Equals(_last2, v2))
            {
                _last1 = v1;
                _last2 = v2;
                _hasValue = true;
                _onChanged(v1, v2);
            }
        }
    }

    public class MultiObserver<T1, T2, T3> : IValueChangeDetector
    {
        private readonly Func<T1> _getter1;
        private readonly Func<T2> _getter2;
        private readonly Func<T3> _getter3;
        private readonly Action<T1, T2, T3> _onChanged;

        private T1 _last1;
        private T2 _last2;
        private T3 _last3;
        private bool _hasValue = false;

        public MultiObserver(Func<T1> getter1, Func<T2> getter2, Func<T3> getter3, Action<T1, T2, T3> onChanged)
        {
            _getter1 = getter1;
            _getter2 = getter2;
            _getter3 = getter3;
            _onChanged = onChanged;
        }

        public void Evaluate()
        {
            var v1 = _getter1();
            var v2 = _getter2();
            var v3 = _getter3();

            if (!_hasValue ||
                !EqualityComparer<T1>.Default.Equals(_last1, v1) ||
                !EqualityComparer<T2>.Default.Equals(_last2, v2) ||
                !EqualityComparer<T3>.Default.Equals(_last3, v3))
            {
                _last1 = v1;
                _last2 = v2;
                _last3 = v3;
                _hasValue = true;
                _onChanged(v1, v2, v3);
            }
        }
    }

    public static class ViewObserverExtensions
    {
        public static void AddMulti<T1, T2>(
            this ValueChangeManager mgr,
            Func<T1> g1, Func<T2> g2,
            Action<T1, T2> onChanged)
        {
            mgr.Add(new MultiObserver<T1, T2>(g1, g2, onChanged));
        }

        public static void AddMulti<T1, T2, T3>(
            this ValueChangeManager mgr,
            Func<T1> g1, Func<T2> g2, Func<T3> g3,
            Action<T1, T2, T3> onChanged)
        {
            mgr.Add(new MultiObserver<T1, T2, T3>(g1, g2, g3, onChanged));
        }

        // 你可以继续添加 T4、T5...
    }

    public class Computed<T>
    {
        private readonly Func<T> _compute;
        private T _lastValue;
        private bool _hasValue = false;

        public Computed(Func<T> compute)
        {
            _compute = compute;
        }

        public bool HasChanged()
        {
            var current = _compute();
            if (!_hasValue || !EqualityComparer<T>.Default.Equals(current, _lastValue))
            {
                _lastValue = current;
                _hasValue = true;
                return true;
            }
            return false;
        }

        public T Value => _compute();

        public static Computed<T> When(Func<T> compute)
        {
            return new Computed<T>(compute);
        }
    }

    public class ComputedObserver<T> : IValueChangeDetector
    {
        private readonly Computed<T> _computed;
        private readonly Action<T> _onChanged;

        public ComputedObserver(Computed<T> computed, Action<T> onChanged)
        {
            _computed = computed;
            _onChanged = onChanged;
        }

        public void Evaluate()
        {
            if (_computed.HasChanged())
            {
                _onChanged(_computed.Value);
            }
        }
    }

    public static class ViewObserverComputedExtensions
    {
        public static void Bind<T>(this ValueChangeManager mgr, Computed<T> computed, Action<T> onChanged)
        {
            mgr.Add(new ComputedObserver<T>(computed, onChanged));
        }
    }
}
namespace GE
{
    public abstract class SingletonEntity<T> : Entity where T : SingletonEntity<T>
    {
        public static T Instance;

        protected override void RegisterSystem()
        {
            base.RegisterSystem();
            Instance = (T)this;
        }

        public override void Dispose()
        {
            base.Dispose();
            Instance = null;
        }
    }
}

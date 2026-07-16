using System.Diagnostics;

namespace GameEntity
{
    public abstract class Scene : Entity, IAwake, IDestroy
    {

        public string Name { get; }


        public Scene(string name)
        {
            this.Name = name;
            this.IsCreated = true;
            this.IsNew = true;

            // Log.Info($"scene create: {this.Name} {this.Id} {this.InstanceId}");
        }

        public virtual void Awake()
        {
            Log.Info($"scene awake: {this.Name} {this.Id} {this.InstanceId}");
        }

        public virtual void OnDestroy()
        {
            Log.Info($"scene destroy: {this.Name} {this.Id} {this.InstanceId}");
        }

        protected override string ViewName
        {
            get
            {
                return $"{this.GetType().Name}";
            }
        }
    }
}

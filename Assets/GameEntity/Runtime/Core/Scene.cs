using System.Diagnostics;

namespace GE
{
    [ChildOf]
    public abstract class Scene: Entity, IScene,IAwake,IDestroy
    {
        
        public string Name { get; }
        
        
        public Scene(string name)
        {
            this.Name = name;
            this.Id = IdGenerator.Instance.GenerateId();
            this.InstanceId = IdGenerator.Instance.GenerateInstanceId();
            this.IsCreated = true;
            this.IsNew = true;
            this.IScene = this;
            this.IsRegister = true;
            
            Log.Info($"scene create: {this.Name} {this.Id} {this.InstanceId}");
        }

        public Scene(long id, long instanceId,  string name)
        {
            this.Id = id;
            this.Name = name;
            this.InstanceId = instanceId;
            this.IsCreated = true;
            this.IsNew = true;
            this.IScene = this;
            this.IsRegister = true;
    
            Log.Info($"scene create: {this.Name} {this.Id} {this.InstanceId}");
        }

        public override void Dispose()
        {
            base.Dispose();
            
            Log.Info($"scene dispose: {this.Name} {this.Id} {this.InstanceId}");
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
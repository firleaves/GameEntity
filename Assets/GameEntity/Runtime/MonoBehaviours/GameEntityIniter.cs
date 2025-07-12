using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GE
{

    /// <summary>
    /// 游戏实体初始化器
    /// 需要等待一帧后，才能去调用world 添加场景 和 添加entity
    /// </summary>
    public class GameEntityIniter : MonoBehaviour
    {
        void Awake()
        {
            World.Instance.Root = this.gameObject;
            World.Instance.AddSingleton<EntitySystem>();
            World.Instance.AddSingleton<ObjectPool>();
            World.Instance.AddSingleton<IdGenerator>();
            World.Instance.AddSingleton<TimeInfo>();
            
            World.Instance.InitializeDependencySystem();
        }
        private void Start()
        {




        }



        void Update()
        {
            EntitySystem.Instance.Update(Time.deltaTime,Time.unscaledDeltaTime);
        }

        void LateUpdate()
        {
            EntitySystem.Instance.LateUpdate();
        }

        void FixedUpdate()
        {
            
        }

        void OnApplicationQuit()
        {
            World.Instance.Dispose();
        }


    }
}
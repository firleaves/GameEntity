namespace GameEntity
{
    /// <summary>
    /// Entity 挂接后同步执行的无参数初始化接口。
    /// </summary>
    public interface IAwake
    {
        void Awake();
    }

    /// <summary>
    /// Entity 挂接后同步执行的单参数初始化接口。
    /// </summary>
    /// <typeparam name="T">创建参数类型。</typeparam>
    public interface IAwake<T>
    {
        void Awake(T p1);
    }

    /// <summary>
    /// Entity 挂接后同步执行的双参数初始化接口。
    /// </summary>
    public interface IAwake<T1, T2>
    {
        void Awake(T1 p1, T2 p2);
    }

    /// <summary>
    /// Entity 挂接后同步执行的三参数初始化接口。
    /// </summary>
    public interface IAwake<T1, T2, T3>
    {
        void Awake(T1 p1, T2 p2, T3 p3);
    }

    /// <summary>
    /// Entity 挂接后同步执行的四参数初始化接口。
    /// </summary>
    public interface IAwake<T1, T2, T3, T4>
    {
        void Awake(T1 p1, T2 p2, T3 p3, T4 p4);
    }

    /// <summary>
    /// Entity 在当前生命期第一次满足更新条件时执行一次的第二阶段初始化接口。
    /// </summary>
    public interface IStart
    {
        void Start();
    }


    /// <summary>
    /// 接收宿主传给 World.FixedUpdate 的统一固定模拟步长。
    /// </summary>
    public interface IFixedUpdate
    {
        void FixedUpdate(float fixedDeltaTime);
    }

    /// <summary>
    /// 接收宿主传给 World.Update 的游戏帧时间，或 IEntityUpdateInterval 累计后的时间。
    /// </summary>
    public interface IUpdate
    {
        void Update(float deltaTime);
    }

    /// <summary>
    /// Entity 当前生命期销毁时执行一次的清理接口。
    /// </summary>
    public interface IDestroy
    {
        void OnDestroy();
    }
}

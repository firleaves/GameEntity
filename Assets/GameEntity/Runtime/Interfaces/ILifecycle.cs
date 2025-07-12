namespace GE
{
    // 初始化接口
    public interface IAwake
    {
        void Awake();
    }

    public interface IAwake<T>
    {
        void Awake(T p1);
    }

    public interface IAwake<T1, T2>
    {
        void Awake(T1 p1, T2 p2);
    }


    public interface IAwake<T1, T2, T3>
    {
        void Awake(T1 p1, T2 p2, T3 p3);
    }


    public interface IAwake<T1,T2,T3,T4>
    {
        void Awake(T1 p1,T2 p2,T3 p3,T4 p4);
    }


    // 更新接口
    public interface IUpdate
    {
        void Update(float time);
    }


    public interface ILateUpdate
    {
        void LateUpdate();
    }

    // 销毁接口
    public interface IDestroy
    {
        void OnDestroy();
    }
}

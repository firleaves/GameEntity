namespace GameEntity.Unity.Framework
{
    internal static class NetworkTransportFactory
    {
        public static INetworkTransport Create(NetworkChannelRuntimeOptions options)
        {
            if (options == null)
            {
                throw new FrameworkException("创建网络传输失败：options 不能为空。");
            }

            switch (options.Transport)
            {
                case NetworkTransportKind.Tcp:
                    return new TcpNetworkTransport();
                case NetworkTransportKind.Mock:
                    return new MockNetworkTransport();
                case NetworkTransportKind.Custom:
                    if (options.CustomTransport == null)
                    {
                        throw new FrameworkException("创建网络传输失败：CustomTransport 不能为空。");
                    }

                    return options.CustomTransport;
                default:
                    throw new FrameworkException($"创建网络传输失败：不支持的传输类型 {options.Transport}。");
            }
        }
    }
}

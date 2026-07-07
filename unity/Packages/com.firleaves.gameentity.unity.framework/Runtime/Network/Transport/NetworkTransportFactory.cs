namespace GameEntity.Unity.Framework
{
    internal static class NetworkTransportFactory
    {
        public static INetworkTransport Create(NetworkChannelRuntimeConfig config)
        {
            if (config == null)
            {
                throw new FrameworkException("创建网络传输失败：config 不能为空。");
            }

            switch (config.Transport)
            {
                case NetworkTransportKind.Tcp:
                    return new TcpNetworkTransport();
                case NetworkTransportKind.Mock:
                    return new MockNetworkTransport();
                case NetworkTransportKind.Custom:
                    if (config.CustomTransport == null)
                    {
                        throw new FrameworkException("创建网络传输失败：CustomTransport 不能为空。");
                    }

                    return config.CustomTransport;
                default:
                    throw new FrameworkException($"创建网络传输失败：不支持的传输类型 {config.Transport}。");
            }
        }
    }
}

using System.Runtime.CompilerServices;

// 允许 PlayMode Tests 访问内部成员
[assembly: InternalsVisibleTo("GameEntity.Runtime.Tests")]

// 如果使用了 Unity Test Framework 的动态代理功能，还需要添加
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] 
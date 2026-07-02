using System.Runtime.CompilerServices;

// 允许测试程序集访问内部成员
[assembly: InternalsVisibleTo("GEGAS.Tests")]
[assembly: InternalsVisibleTo("GameEntity.Tests")]

// 如果使用了 Unity Test Framework 的动态代理功能，还需要添加
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

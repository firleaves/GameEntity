using System;
using System.Linq;
using GameEntity;

namespace GameEntity.CoreTestApp
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                Console.WriteLine("GameEntity CoreTestApp 启动");
                BootstrapCore();
                RunCoreScenario();
                Console.WriteLine("Core 场景验证通过");
                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Core 场景验证失败");
                Console.Error.WriteLine(e);
                return 1;
            }
            finally
            {
                World.Instance.Dispose();
            }
        }

        private static void BootstrapCore()
        {
            World.Instance.Dispose();
            Console.WriteLine("Core World 初始化完成");
        }

        private static void RunCoreScenario()
        {
            var scene = World.Instance.AddScene("CoreAppScene", new CoreAppScene("CoreAppScene"));
            var actor = scene.AddChild<ActorEntity>();
            var abilitySystem = actor.AddComponent<AbilitySystemComponent>();
            var effect = abilitySystem.AddChild<CoreEffect, string>("Burning");

            EntityHandle abilityHandle = abilitySystem.Handle;
            EntityHandle effectHandle = effect.Handle;
            EntityRef<CoreEffect> effectRef = effect;

            Expect(actor.Parent == scene, "actor 应该被 scene 拥有。");
            Expect(actor.Owner == scene, "Owner 应该返回 scene。");
            Expect(abilitySystem.Parent == actor, "abilitySystem 应该作为 actor 的 component。");
            Expect(effect.Parent == abilitySystem, "effect 应该作为 abilitySystem 拥有的图节点实例。");
            Expect(effect.GetSceneRoot() == scene, "effect 应该归属 CoreAppScene。");
            Expect(effect.TryFindOwner<ActorEntity>(out var ownerActor) && ownerActor == actor, "effect 应该能语义化找到 Actor owner。");
            Expect(effect.TryGetComponentInAncestors<AbilitySystemComponent>(out var ownerAbility) && ownerAbility == abilitySystem, "effect 应该能在 ancestor 中找到 AbilitySystemComponent。");

            EntitySnapshot snapshot = World.Instance.CaptureEntitySnapshot();
            Expect(snapshot.Nodes.Count == 4, "初始场景应该包含 scene、actor、abilitySystem、effect 四个 hierarchy 节点。");
            Expect(snapshot.Nodes.Count(node => node.Kind == NodeKind.SceneRoot) == 1, "应该只有一个 SceneRoot 节点。");
            Expect(snapshot.Nodes.Count(node => node.Kind == NodeKind.ChildEntity) == 2, "actor 和 effect 应该是 ChildEntity。");
            Expect(snapshot.Nodes.Count(node => node.Kind == NodeKind.ComponentEntity) == 1, "abilitySystem 应该是 ComponentEntity。");
            Expect(World.Instance.ValidateEntities().IsValid, "初始 hierarchy 结构必须有效。");

            RunFrames(5);
            Expect(actor.UpdateCount == 5, "actor 应该被 World.Tick 驱动。");
            Expect(abilitySystem.UpdateCount == 5, "abilitySystem 应该被 World.Tick 驱动。");
            Expect(effect.UpdateCount == 5, "effect 应该被 World.Tick 驱动。");

            actor.RemoveComponent<AbilitySystemComponent>();
            Expect(!actor.IsDisposed, "移除 component 不应该销毁 actor。");
            Expect(abilitySystem.IsDisposed, "移除 component 应该销毁 abilitySystem。");
            Expect(effect.IsDisposed, "移除 component 应该级联销毁 effect。");
            Expect(!effectRef.IsAlive, "effect 的 EntityRef 应该自然失效。");
            Expect(!World.Instance.TryResolve(effectHandle, out CoreEffect _), "旧 effect handle 应该无法解析。");
            Expect(!World.Instance.TryResolve(abilityHandle, out AbilitySystemComponent _), "旧 abilitySystem handle 应该无法解析。");
            Expect(World.Instance.ValidateEntities().IsValid, "移除 component 后 hierarchy 结构必须有效。");

            var newAbilitySystem = actor.AddComponent<AbilitySystemComponent>();
            Expect(newAbilitySystem.Handle.NodeId == abilityHandle.NodeId, "hierarchy 应该复用释放后的 abilitySystem node slot。");
            Expect(newAbilitySystem.Handle.Generation != abilityHandle.Generation, "复用 node slot 时 Generation 必须递增。");
            Expect(World.Instance.TryResolve(newAbilitySystem.Handle, out AbilitySystemComponent resolvedAbility) && resolvedAbility == newAbilitySystem, "新 abilitySystem handle 应该能解析。");
            Expect(!World.Instance.TryResolve(abilityHandle, out AbilitySystemComponent _), "旧 abilitySystem handle 不应该误解析到新对象。");

            scene.Dispose();
            Expect(actor.IsDisposed, "scene dispose 应该级联销毁 actor。");
            Expect(newAbilitySystem.IsDisposed, "scene dispose 应该级联销毁新 abilitySystem。");
            Expect(World.Instance.CaptureEntitySnapshot().Nodes.Count == 0, "scene dispose 后 hierarchy 节点应清空。");
            Expect(World.Instance.ValidateEntities().IsValid, "scene dispose 后 hierarchy 结构仍应有效。");
        }

        private static void RunFrames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                World.Instance.Tick(0.016f, 0.016f);
            }
        }

        private static void Expect(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }

    internal sealed class CoreAppScene : Scene
    {
        public CoreAppScene(string name) : base(name)
        {
        }
    }

    internal sealed class ActorEntity : Entity, IAwake, IUpdate, IDestroy
    {
        public int AwakeCount { get; private set; }

        public int UpdateCount { get; private set; }

        public int DestroyCount { get; private set; }

        public void Awake()
        {
            AwakeCount++;
        }

        public void Update(float time)
        {
            UpdateCount++;
        }

        public void OnDestroy()
        {
            DestroyCount++;
        }
    }

    internal sealed class AbilitySystemComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public int AwakeCount { get; private set; }

        public int UpdateCount { get; private set; }

        public int DestroyCount { get; private set; }

        public void Awake()
        {
            AwakeCount++;
        }

        public void Update(float time)
        {
            UpdateCount++;
        }

        public void OnDestroy()
        {
            DestroyCount++;
        }
    }

    internal sealed class CoreEffect : Entity, IAwake<string>, IUpdate, IDestroy
    {
        public string EffectName { get; private set; }

        public int UpdateCount { get; private set; }

        public int DestroyCount { get; private set; }

        public void Awake(string effectName)
        {
            EffectName = effectName;
        }

        public void Update(float time)
        {
            UpdateCount++;
        }

        public void OnDestroy()
        {
            DestroyCount++;
        }
    }
}

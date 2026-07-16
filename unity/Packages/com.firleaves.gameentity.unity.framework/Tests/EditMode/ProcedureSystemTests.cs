using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace GameEntity.Unity.Framework.Tests
{
    public sealed class ProcedureSystemTests
    {
        [SetUp]
        public void SetUp()
        {
            FirstProcedure.Reset();
            SecondProcedure.Reset();
            BlockingProcedure.Reset();
        }

        [Test]
        public void ChangeState_CallsEnterAndExit()
        {
            var system = new ProcedureSystemEntity();
            system.Awake();
            system.Register<FirstProcedure>();
            system.Register<SecondProcedure>();

            system.ChangeStateAsync<FirstProcedure>().GetAwaiter().GetResult();
            system.ChangeStateAsync<SecondProcedure>().GetAwaiter().GetResult();

            Assert.AreEqual(nameof(SecondProcedure), system.CurrentStateName);
            Assert.AreEqual(1, FirstProcedure.EnterCount);
            Assert.AreEqual(1, FirstProcedure.ExitCount);
            Assert.AreEqual(1, SecondProcedure.EnterCount);
            Assert.AreEqual(0, SecondProcedure.ExitCount);
        }

        [Test]
        public void Update_CallsCurrentProcedureOnlyWhenNotTransitioning()
        {
            var system = new ProcedureSystemEntity();
            system.Awake();
            system.Register<FirstProcedure>();

            system.ChangeStateAsync<FirstProcedure>().GetAwaiter().GetResult();
            system.Update(0.1f);
            system.Update(0.1f);

            Assert.AreEqual(2, FirstProcedure.UpdateCount);
        }

        [Test]
        public void Stop_CallsExitAndClearsCurrentState()
        {
            var system = new ProcedureSystemEntity();
            system.Awake();
            system.Register<FirstProcedure>();

            system.ChangeStateAsync<FirstProcedure>().GetAwaiter().GetResult();
            system.StopAsync().GetAwaiter().GetResult();

            Assert.IsNull(system.CurrentState);
            Assert.IsNull(system.CurrentStateName);
            Assert.AreEqual(1, FirstProcedure.ExitCount);
        }

        [Test]
        public async UniTask ConcurrentChange_WaitsUntilQueuedTransitionCompletes()
        {
            var system = new ProcedureSystemEntity();
            system.Awake();
            system.Register<BlockingProcedure>();
            system.Register<SecondProcedure>();

            var firstTransition = system.ChangeStateAsync<BlockingProcedure>();
            var secondTransition = system.ChangeStateAsync<SecondProcedure>();

            Assert.AreEqual(UniTaskStatus.Pending, firstTransition.Status);
            Assert.AreEqual(UniTaskStatus.Pending, secondTransition.Status);

            BlockingProcedure.EnterCompletion.TrySetResult();
            await firstTransition;
            await secondTransition;

            Assert.AreEqual(nameof(SecondProcedure), system.CurrentStateName);
            Assert.AreEqual(1, BlockingProcedure.ExitCount);
            Assert.AreEqual(1, SecondProcedure.EnterCount);
        }

        [Test]
        public async UniTask StopDuringTransition_WaitsAndExitsCurrentState()
        {
            var system = new ProcedureSystemEntity();
            system.Awake();
            system.Register<BlockingProcedure>();

            var transition = system.ChangeStateAsync<BlockingProcedure>();
            var stop = system.StopAsync();

            Assert.AreEqual(UniTaskStatus.Pending, stop.Status);
            BlockingProcedure.EnterCompletion.TrySetResult();

            await transition;
            await stop;

            Assert.IsNull(system.CurrentState);
            Assert.IsNull(system.CurrentStateName);
            Assert.AreEqual(1, BlockingProcedure.ExitCount);
        }

        private sealed class FirstProcedure : Procedure
        {
            public static int EnterCount;
            public static int ExitCount;
            public static int UpdateCount;

            public static void Reset()
            {
                EnterCount = 0;
                ExitCount = 0;
                UpdateCount = 0;
            }

            public override UniTask EnterAsync(ProcedureContext context)
            {
                EnterCount++;
                return UniTask.CompletedTask;
            }

            public override UniTask ExitAsync(ProcedureContext context)
            {
                ExitCount++;
                return UniTask.CompletedTask;
            }

            public override void Update(float deltaTime)
            {
                UpdateCount++;
            }
        }

        private sealed class SecondProcedure : Procedure
        {
            public static int EnterCount;
            public static int ExitCount;

            public static void Reset()
            {
                EnterCount = 0;
                ExitCount = 0;
            }

            public override UniTask EnterAsync(ProcedureContext context)
            {
                EnterCount++;
                return UniTask.CompletedTask;
            }

            public override UniTask ExitAsync(ProcedureContext context)
            {
                ExitCount++;
                return UniTask.CompletedTask;
            }
        }

        private sealed class BlockingProcedure : Procedure
        {
            public static UniTaskCompletionSource EnterCompletion;
            public static int ExitCount;

            public static void Reset()
            {
                EnterCompletion = new UniTaskCompletionSource();
                ExitCount = 0;
            }

            public override UniTask EnterAsync(ProcedureContext context)
            {
                return EnterCompletion.Task;
            }

            public override UniTask ExitAsync(ProcedureContext context)
            {
                ExitCount++;
                return UniTask.CompletedTask;
            }
        }
    }
}

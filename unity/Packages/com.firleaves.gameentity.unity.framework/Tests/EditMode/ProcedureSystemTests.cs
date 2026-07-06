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
    }
}

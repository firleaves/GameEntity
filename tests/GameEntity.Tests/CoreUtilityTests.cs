using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace GameEntity.Tests
{
    public sealed class CoreUtilityTests : GameEntityTestBase
    {
        [Fact]
        public void TimeInfo_ShouldConvertTimestampsAndApplyServerOffset()
        {
            var time = new TimeInfo();
            time.Awake();
            time.TimeZone = 8;
            var value = new DateTime(2024, 5, 6, 7, 8, 9, 123, DateTimeKind.Utc);

            long timestamp = time.Transition(value);
            DateTime roundTrip = time.ToDateTime(timestamp);
            long beforeUpdate = time.ClientNow();
            time.ServerMinusClientTime = 60_000;
            time.Update();
            long serverDelta = time.ServerNow() - time.ClientNow();

            Assert.Equal(8, time.TimeZone);
            Assert.Equal(value, roundTrip);
            Assert.InRange(time.ClientFrameTime(), beforeUpdate, time.ClientNow());
            Assert.Equal(time.ClientFrameTime() + 60_000, time.ServerFrameTime());
            Assert.InRange(serverDelta, 59_000, 60_000);

            time.Reset();
            Assert.Equal(0, time.ClientFrameTime());
            Assert.Equal(0, time.ServerFrameTime());
        }

        [Fact]
        public void IdStructs_ShouldRoundTripPackedValues()
        {
            var id = new IdStruct(123456, 321, 654321);
            var decodedId = new IdStruct(id.ToLong());
            var instanceId = new InstanceIdStruct(987654321, 123456789);
            var decodedInstanceId = new InstanceIdStruct(instanceId.ToLong());

            Assert.Equal(id.Process, decodedId.Process);
            Assert.Equal(id.Time, decodedId.Time);
            Assert.Equal(id.Value, decodedId.Value);
            Assert.Contains("process: 321", decodedId.ToString());
            Assert.Contains("time: 123456", decodedId.ToString());
            Assert.Equal(instanceId.Time, decodedInstanceId.Time);
            Assert.Equal(instanceId.Value, decodedInstanceId.Value);
            Assert.Equal("time: 987654321, value: 123456789", decodedInstanceId.ToString());
        }

        [Fact]
        public void IdGenerator_ShouldRejectNullTimeAndWrapEntityCounter()
        {
            Assert.Throws<ArgumentNullException>(() => new IdGenerator(null));

            var time = new TimeInfo();
            time.Awake();
            var generator = new IdGenerator(time);
            generator.Awake();
            typeof(IdGenerator)
                .GetField("_value", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(generator, IdGenerator.Mask20bit - 1);

            var wrapped = new IdStruct(generator.GenerateId());
            long firstInstanceId = generator.GenerateInstanceId();
            long secondInstanceId = generator.GenerateInstanceId();

            Assert.Equal(0u, wrapped.Value);
            Assert.NotEqual(firstInstanceId, secondInstanceId);
        }

        [Fact]
        public void ObjectPool_GenericFetchShouldReuseAndClearObjects()
        {
            var pool = new ObjectPool();
            pool.Awake();

            UtilityPoolObject first = pool.Fetch<UtilityPoolObject>();
            pool.Recycle(first);
            UtilityPoolObject reused = pool.Fetch<UtilityPoolObject>();
            object direct = pool.Fetch(typeof(UtilityPoolObject), isFromPool: false);

            Assert.Same(first, reused);
            Assert.IsType<UtilityPoolObject>(direct);

            pool.Recycle(reused);
            pool.Clear();
            Assert.NotSame(first, pool.Fetch<UtilityPoolObject>());
        }

        [Fact]
        public void TypeHelper_ShouldCacheAndDiscoverExactAttributes()
        {
            UtilityMarkerAttribute first = TypeHelper.GetAttribute<UtilityMarkerAttribute>(typeof(MarkedUtilityType));
            UtilityMarkerAttribute second = TypeHelper.GetAttribute<UtilityMarkerAttribute>(typeof(MarkedUtilityType));

            Assert.NotNull(first);
            Assert.Equal("marked", first.Name);
            Assert.Same(first, second);
            Assert.True(TypeHelper.HasAttribute<UtilityMarkerAttribute>(typeof(MarkedUtilityType)));
            Assert.False(TypeHelper.HasAttribute<UtilityMarkerAttribute>(typeof(UnmarkedUtilityType)));
            Assert.Null(TypeHelper.GetAttribute<UtilityMarkerAttribute>(typeof(UnmarkedUtilityType)));
            Assert.Contains(typeof(MarkedUtilityType), TypeHelper.GetTypesByAttribute<UtilityMarkerAttribute>());
        }

        [Fact]
        public void InspectorIgnoreAttribute_ShouldPreserveOptionalReason()
        {
            var withoutReason = new GameEntityInspectorIgnoreAttribute();
            var withReason = new GameEntityInspectorIgnoreAttribute("runtime-only");
            AttributeUsageAttribute usage = typeof(GameEntityInspectorIgnoreAttribute)
                .GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false)
                .Cast<AttributeUsageAttribute>()
                .Single();

            Assert.Null(withoutReason.Reason);
            Assert.Equal("runtime-only", withReason.Reason);
            Assert.Equal(AttributeTargets.Field | AttributeTargets.Property, usage.ValidOn);
        }

        [Fact]
        public void ConsoleLogger_ShouldHonorLevelsAndOutputChannels()
        {
            TextWriter originalOut = Console.Out;
            TextWriter originalError = Console.Error;
            var standardOutput = new StringWriter();
            var errorOutput = new StringWriter();
            try
            {
                Console.SetOut(standardOutput);
                Console.SetError(errorOutput);
                var logger = new ConsoleLogger();

                logger.Debug("debug-enabled");
                logger.Info("info-enabled");
                logger.Warning("warning-enabled");
                logger.Error("error-enabled");
                logger.Exception(new InvalidOperationException("exception-enabled"));
                logger.SetLogLevel(debug: false, info: false, warning: false, error: false);
                logger.Debug("debug-disabled");
                logger.Info("info-disabled");
                logger.Warning("warning-disabled");
                logger.Error("error-disabled");
                logger.Exception(new InvalidOperationException("exception-disabled"));

                Assert.Contains("[DEBUG] debug-enabled", standardOutput.ToString());
                Assert.Contains("[INFO] info-enabled", standardOutput.ToString());
                Assert.Contains("[WARNING] warning-enabled", standardOutput.ToString());
                Assert.DoesNotContain("disabled", standardOutput.ToString());
                Assert.Contains("[ERROR] error-enabled", errorOutput.ToString());
                Assert.Contains("exception-enabled", errorOutput.ToString());
                Assert.DoesNotContain("disabled", errorOutput.ToString());
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
            }
        }

        [Fact]
        public void LogAndNullLogger_ShouldImplementCompleteLoggerContract()
        {
            ILogger previous = Log.Logger;
            var logger = new RecordingLogger();
            var exception = new InvalidOperationException("recorded");
            try
            {
                Log.Logger = logger;
                Log.SetLogLevel(debug: true, info: false, warning: true, error: false);
                InvokeConditionalLog(nameof(Log.Debug), "debug");
                InvokeConditionalLog(nameof(Log.Info), "info");
                Log.Warning("warning");
                Log.Error("error");
                Log.Exception(exception);

                Assert.Equal((true, false, true, false), logger.Levels);
                Assert.Equal("debug", logger.DebugMessage);
                Assert.Equal("info", logger.InfoMessage);
                Assert.Equal("warning", logger.WarningMessage);
                Assert.Equal("error", logger.ErrorMessage);
                Assert.Same(exception, logger.ExceptionValue);
                Assert.Throws<ArgumentNullException>(() => Log.Logger = null);

                NullLogger.Instance.SetLogLevel(true, true, true, true);
                NullLogger.Instance.Debug("debug");
                NullLogger.Instance.Info("info");
                NullLogger.Instance.Warning("warning");
                NullLogger.Instance.Error("error");
                NullLogger.Instance.Exception(exception);
            }
            finally
            {
                Log.Logger = previous;
            }
        }

        private static void InvokeConditionalLog(string methodName, object value)
        {
            typeof(Log).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new[] { value });
        }
    }

    internal sealed class UtilityPoolObject
    {
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    internal sealed class UtilityMarkerAttribute : Attribute
    {
        public UtilityMarkerAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    [UtilityMarker("marked")]
    internal sealed class MarkedUtilityType
    {
    }

    internal sealed class UnmarkedUtilityType
    {
    }

    internal sealed class RecordingLogger : ILogger
    {
        public (bool Debug, bool Info, bool Warning, bool Error) Levels { get; private set; }

        public object DebugMessage { get; private set; }

        public object InfoMessage { get; private set; }

        public object WarningMessage { get; private set; }

        public object ErrorMessage { get; private set; }

        public Exception ExceptionValue { get; private set; }

        public void SetLogLevel(bool debug, bool info, bool warning, bool error)
        {
            Levels = (debug, info, warning, error);
        }

        public void Debug(object message)
        {
            DebugMessage = message;
        }

        public void Info(object message)
        {
            InfoMessage = message;
        }

        public void Warning(object message)
        {
            WarningMessage = message;
        }

        public void Error(object message)
        {
            ErrorMessage = message;
        }

        public void Exception(Exception exception)
        {
            ExceptionValue = exception;
        }
    }
}

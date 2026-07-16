using Xunit;

namespace GameEntity.Tests
{
    public sealed class EntityCreationApiTests : GameEntityTestBase
    {
        [Fact]
        public void AddChildOverloads_ShouldDispatchZeroThroughFourAwakeArguments()
        {
            TestScene scene = CreateScene("creation-child-overloads");

            CreationAwake0Entity zero = scene.AddChild<CreationAwake0Entity>();
            CreationAwake1Entity one = scene.AddChild<CreationAwake1Entity, string>("one");
            CreationAwake2Entity two = scene.AddChild<CreationAwake2Entity, string, int>("two", 2);
            CreationAwake3Entity three = scene.AddChild<CreationAwake3Entity, string, int, bool>("three", 3, true);
            CreationAwake4Entity four = scene.AddChild<CreationAwake4Entity, string, int, bool, float>("four", 4, false, 4.5f);

            AssertAwakeValues(zero, one, two, three, four);
            Assert.All(new Entity[] { zero, one, two, three, four }, entity => Assert.Same(scene, entity.Owner));
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void AddPooledChildOverloads_ShouldDispatchArgumentsAndReuseEachType()
        {
            TestScene scene = CreateScene("creation-pooled-child-overloads");
            CreationAwake0Entity zero = scene.AddPooledChild<CreationAwake0Entity>();
            CreationAwake1Entity one = scene.AddPooledChild<CreationAwake1Entity, string>("one");
            CreationAwake2Entity two = scene.AddPooledChild<CreationAwake2Entity, string, int>("two", 2);
            CreationAwake3Entity three = scene.AddPooledChild<CreationAwake3Entity, string, int, bool>("three", 3, true);
            CreationAwake4Entity four = scene.AddPooledChild<CreationAwake4Entity, string, int, bool, float>("four", 4, false, 4.5f);

            zero.Destroy();
            one.Destroy();
            two.Destroy();
            three.Destroy();
            four.Destroy();

            Assert.Same(zero, scene.AddPooledChild<CreationAwake0Entity>());
            Assert.Same(one, scene.AddPooledChild<CreationAwake1Entity, string>("one-reused"));
            Assert.Same(two, scene.AddPooledChild<CreationAwake2Entity, string, int>("two-reused", 12));
            Assert.Same(three, scene.AddPooledChild<CreationAwake3Entity, string, int, bool>("three-reused", 13, false));
            Assert.Same(four, scene.AddPooledChild<CreationAwake4Entity, string, int, bool, float>("four-reused", 14, true, 14.5f));

            Assert.Equal(2, zero.AwakeCount);
            Assert.Equal("one-reused", one.P1);
            Assert.Equal(("two-reused", 12), (two.P1, two.P2));
            Assert.Equal(("three-reused", 13, false), (three.P1, three.P2, three.P3));
            Assert.Equal(("four-reused", 14, true, 14.5f), (four.P1, four.P2, four.P3, four.P4));
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void AddChildWithIdOverloads_ShouldKeepExplicitIdsAndDispatchArguments()
        {
            TestScene scene = CreateScene("creation-child-id-overloads");

            CreationAwake0Entity zero = scene.AddChildWithId<CreationAwake0Entity>(101);
            CreationAwake1Entity one = scene.AddChildWithId<CreationAwake1Entity, string>(102, "one");
            CreationAwake2Entity two = scene.AddChildWithId<CreationAwake2Entity, string, int>(103, "two", 2);
            CreationAwake3Entity three = scene.AddChildWithId<CreationAwake3Entity, string, int, bool>(104, "three", 3, true);
            CreationAwake4Entity four = scene.AddChildWithId<CreationAwake4Entity, string, int, bool, float>(105, "four", 4, false, 4.5f);

            Assert.Equal(new long[] { 101, 102, 103, 104, 105 }, new[] { zero.Id, one.Id, two.Id, three.Id, four.Id });
            AssertAwakeValues(zero, one, two, three, four);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void AddComponentOverloads_ShouldDispatchZeroThroughFourAwakeArguments()
        {
            TestScene scene = CreateScene("creation-component-overloads");
            ProbeEntity owner = scene.AddChild<ProbeEntity>();

            CreationAwake0Entity zero = owner.AddComponent<CreationAwake0Entity>();
            CreationAwake1Entity one = owner.AddComponent<CreationAwake1Entity, string>("one");
            CreationAwake2Entity two = owner.AddComponent<CreationAwake2Entity, string, int>("two", 2);
            CreationAwake3Entity three = owner.AddComponent<CreationAwake3Entity, string, int, bool>("three", 3, true);
            CreationAwake4Entity four = owner.AddComponent<CreationAwake4Entity, string, int, bool, float>("four", 4, false, 4.5f);

            AssertAwakeValues(zero, one, two, three, four);
            Assert.All(new Entity[] { zero, one, two, three, four }, entity => Assert.Same(owner, entity.Owner));
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void AddPooledComponentOverloads_ShouldDispatchArgumentsAndReuseEachType()
        {
            TestScene scene = CreateScene("creation-pooled-component-overloads");
            ProbeEntity owner = scene.AddChild<ProbeEntity>();
            CreationAwake0Entity zero = owner.AddPooledComponent<CreationAwake0Entity>();
            CreationAwake1Entity one = owner.AddPooledComponent<CreationAwake1Entity, string>("one");
            CreationAwake2Entity two = owner.AddPooledComponent<CreationAwake2Entity, string, int>("two", 2);
            CreationAwake3Entity three = owner.AddPooledComponent<CreationAwake3Entity, string, int, bool>("three", 3, true);
            CreationAwake4Entity four = owner.AddPooledComponent<CreationAwake4Entity, string, int, bool, float>("four", 4, false, 4.5f);

            zero.Destroy();
            one.Destroy();
            two.Destroy();
            three.Destroy();
            four.Destroy();

            Assert.Same(zero, owner.AddPooledComponent<CreationAwake0Entity>());
            Assert.Same(one, owner.AddPooledComponent<CreationAwake1Entity, string>("one-reused"));
            Assert.Same(two, owner.AddPooledComponent<CreationAwake2Entity, string, int>("two-reused", 12));
            Assert.Same(three, owner.AddPooledComponent<CreationAwake3Entity, string, int, bool>("three-reused", 13, false));
            Assert.Same(four, owner.AddPooledComponent<CreationAwake4Entity, string, int, bool, float>("four-reused", 14, true, 14.5f));

            Assert.Equal(2, zero.AwakeCount);
            Assert.Equal("one-reused", one.P1);
            Assert.Equal(("two-reused", 12), (two.P1, two.P2));
            Assert.Equal(("three-reused", 13, false), (three.P1, three.P2, three.P3));
            Assert.Equal(("four-reused", 14, true, 14.5f), (four.P1, four.P2, four.P3, four.P4));
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void AddComponentWithIdOverloads_ShouldKeepExplicitIdsAndDispatchArguments()
        {
            TestScene scene = CreateScene("creation-component-id-overloads");
            ProbeEntity owner = scene.AddChild<ProbeEntity>();

            CreationAwake0Entity zero = owner.AddComponentWithId<CreationAwake0Entity>(201);
            CreationAwake1Entity one = owner.AddComponentWithId<CreationAwake1Entity, string>(202, "one");
            CreationAwake2Entity two = owner.AddComponentWithId<CreationAwake2Entity, string, int>(203, "two", 2);
            CreationAwake3Entity three = owner.AddComponentWithId<CreationAwake3Entity, string, int, bool>(204, "three", 3, true);
            CreationAwake4Entity four = owner.AddComponentWithId<CreationAwake4Entity, string, int, bool, float>(205, "four", 4, false, 4.5f);

            Assert.Equal(new long[] { 201, 202, 203, 204, 205 }, new[] { zero.Id, one.Id, two.Id, three.Id, four.Id });
            AssertAwakeValues(zero, one, two, three, four);
            Assert.True(World.Instance.ValidateEntities().IsValid);
        }

        [Fact]
        public void QueryFacades_ShouldCoverTypedRuntimeAndParentVariants()
        {
            TestScene scene = CreateScene("creation-query-facades");
            CreationAwake0Entity owner = scene.AddChildWithId<CreationAwake0Entity>(901);
            CreationAwake1Entity component = owner.AddComponent<CreationAwake1Entity, string>("component");
            CreationAwake2Entity child = owner.AddChild<CreationAwake2Entity, string, int>("child", 2);

            Assert.True(scene.TryGetChild<CreationAwake0Entity>(901, out CreationAwake0Entity foundChild));
            Assert.Same(owner, foundChild);
            Assert.True(owner.TryGetComponent(typeof(CreationAwake1Entity), out Entity foundComponent));
            Assert.Same(component, foundComponent);
            Assert.Same(scene, child.FindOwner<TestScene>());
            Assert.Same(component, child.GetComponentInParent<CreationAwake1Entity>());
            Assert.True(child.TryGetComponentInParent(out CreationAwake1Entity parentComponent));
            Assert.Same(component, parentComponent);

            owner.SetTestViewName("custom-owner");
            Assert.Equal("custom-owner", owner.GetViewName());
            Assert.True(owner.IsNewForTest);
        }

        private static void AssertAwakeValues(
            CreationAwake0Entity zero,
            CreationAwake1Entity one,
            CreationAwake2Entity two,
            CreationAwake3Entity three,
            CreationAwake4Entity four)
        {
            Assert.Equal(1, zero.AwakeCount);
            Assert.Equal(("one", 1), (one.P1, one.AwakeCount));
            Assert.Equal(("two", 2, 1), (two.P1, two.P2, two.AwakeCount));
            Assert.Equal(("three", 3, true, 1), (three.P1, three.P2, three.P3, three.AwakeCount));
            Assert.Equal(("four", 4, false, 4.5f, 1), (four.P1, four.P2, four.P3, four.P4, four.AwakeCount));
        }
    }

    public sealed class CreationAwake0Entity : Entity, IAwake
    {
        public int AwakeCount { get; private set; }

        public bool IsNewForTest => IsNew;

        public void Awake()
        {
            AwakeCount++;
        }

        public void SetTestViewName(string value)
        {
            ViewName = value;
        }
    }

    public sealed class CreationAwake1Entity : Entity, IAwake<string>
    {
        public int AwakeCount { get; private set; }

        public string P1 { get; private set; }

        public void Awake(string p1)
        {
            AwakeCount++;
            P1 = p1;
        }
    }

    public sealed class CreationAwake2Entity : Entity, IAwake<string, int>
    {
        public int AwakeCount { get; private set; }

        public string P1 { get; private set; }

        public int P2 { get; private set; }

        public void Awake(string p1, int p2)
        {
            AwakeCount++;
            P1 = p1;
            P2 = p2;
        }
    }

    public sealed class CreationAwake3Entity : Entity, IAwake<string, int, bool>
    {
        public int AwakeCount { get; private set; }

        public string P1 { get; private set; }

        public int P2 { get; private set; }

        public bool P3 { get; private set; }

        public void Awake(string p1, int p2, bool p3)
        {
            AwakeCount++;
            P1 = p1;
            P2 = p2;
            P3 = p3;
        }
    }

    public sealed class CreationAwake4Entity : Entity, IAwake<string, int, bool, float>
    {
        public int AwakeCount { get; private set; }

        public string P1 { get; private set; }

        public int P2 { get; private set; }

        public bool P3 { get; private set; }

        public float P4 { get; private set; }

        public void Awake(string p1, int p2, bool p3, float p4)
        {
            AwakeCount++;
            P1 = p1;
            P2 = p2;
            P3 = p3;
            P4 = p4;
        }
    }
}

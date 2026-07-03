using UnityEngine;

namespace GameEntity.Unity.Demo
{
    /// <summary>
    /// 可运行的 Unity 示例：用 GameEntity core 创建实体树，并让 Unity Hierarchy 显示调试投影。
    /// </summary>
    public sealed class GameEntityDemo : MonoBehaviour
    {
        private const string SceneName = "DemoScene";

        private DemoScene _scene;
        private DemoUnit _player;
        private DemoUnit _companion;
        private DemoUnit _monster;
        private DemoStatsComponent _playerStats;
        private DemoStatsComponent _monsterStats;
        private float _elapsed;
        private bool _monsterReparented;

        private void Start()
        {
            EnsureRunner();
            BuildEntityTree();
        }

        private void Update()
        {
            if (_scene == null || _scene.IsDestroyed)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            if (!_monsterReparented && _elapsed >= 3f)
            {
                _monster.ReparentTo(_player);
                _monsterReparented = true;
            }

            if (_playerStats != null)
            {
                _playerStats.Health = Mathf.Max(0, 100 - Mathf.FloorToInt(_elapsed * 3f));
                _playerStats.Energy = Mathf.PingPong(_elapsed * 18f, 100f);
            }

            if (_monsterStats != null)
            {
                _monsterStats.Health = Mathf.Max(0, 80 - Mathf.FloorToInt(_elapsed * 2f));
            }
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(16, 16, 440, 150), "GameEntity for Unity Demo");
            GUI.Label(new Rect(32, 44, 400, 22), "Play 后查看 Hierarchy：GameEntity Demo Root 下会出现 Entity 树。");
            GUI.Label(new Rect(32, 68, 400, 22), $"Scene: {(_scene == null ? "-" : _scene.Name)}");
            GUI.Label(new Rect(32, 92, 400, 22), $"Player Update: {(_player == null ? 0 : _player.UpdateCount)}");
            GUI.Label(new Rect(32, 116, 400, 22), _monsterReparented
                ? "Monster 已通过 Entity.ReparentTo 挂到 Player 下"
                : "3 秒后 Monster 会通过 Entity.ReparentTo 挂到 Player 下");
        }

        private void EnsureRunner()
        {
            var runner = GetComponent<GameEntityRunner>();
            if (runner == null)
            {
                runner = gameObject.AddComponent<GameEntityRunner>();
            }

            runner.ViewRoot = gameObject;
            runner.AutoCreateViews = true;
            runner.DestroyViewsOnEntityDestroy = true;
            runner.UseUnityLogger = true;
            runner.OwnsWorldLifetime = true;
        }

        private void BuildEntityTree()
        {
            Scene existingScene = World.Instance.GetScene(SceneName);
            if (existingScene != null && !existingScene.IsDestroyed)
            {
                existingScene.Destroy();
            }

            _elapsed = 0f;
            _monsterReparented = false;
            _scene = (DemoScene)World.Instance.AddScene(SceneName, new DemoScene(SceneName));

            _player = _scene.AddChild<DemoUnit, string>("Player");
            _playerStats = _player.AddComponent<DemoStatsComponent, int, float>(100, 100f);
            _player.AddComponent<DemoInventoryComponent, int>(3);

            _companion = _player.AddChild<DemoUnit, string>("Companion");
            _companion.AddComponent<DemoStatsComponent, int, float>(60, 70f);

            _monster = _scene.AddChild<DemoUnit, string>("Monster");
            _monsterStats = _monster.AddComponent<DemoStatsComponent, int, float>(80, 25f);
        }

        private void OnDestroy()
        {
            if (_scene != null && !_scene.IsDestroyed)
            {
                _scene.Destroy();
            }

            _scene = null;
            _player = null;
            _companion = null;
            _monster = null;
            _playerStats = null;
            _monsterStats = null;
        }
    }

    internal sealed class DemoScene : Scene
    {
        public DemoScene(string name) : base(name)
        {
        }
    }

    internal sealed class DemoUnit : Entity, IAwake<string>, IUpdate, IDestroy
    {
        private string _name;

        public int UpdateCount { get; private set; }

        protected override string ViewName => string.IsNullOrEmpty(_name) ? base.ViewName : _name;

        public void Awake(string name)
        {
            _name = name;
        }

        public void Update(float time)
        {
            UpdateCount++;
        }

        public void OnDestroy()
        {
        }
    }

    internal sealed class DemoStatsComponent : Entity, IAwake<int, float>
    {
        public int Health { get; set; }

        public float Energy { get; set; }

        protected override string ViewName => "Stats";

        public void Awake(int health, float energy)
        {
            Health = health;
            Energy = energy;
        }
    }

    internal sealed class DemoInventoryComponent : Entity, IAwake<int>
    {
        public int SlotCount { get; private set; }

        protected override string ViewName => "Inventory";

        public void Awake(int slotCount)
        {
            SlotCount = slotCount;
        }
    }
}

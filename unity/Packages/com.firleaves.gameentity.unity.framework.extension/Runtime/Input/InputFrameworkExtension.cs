using GameEntity.Unity.Framework;
using UnityEngine;

namespace GameEntity.Unity.Framework.Extension
{
    [CreateAssetMenu(menuName = "GameEntity/Framework/Input Extension")]
    public sealed class InputFrameworkExtension : FrameworkExtensionAsset
    {
        protected override void Install(FrameworkExtensionContext context)
        {
            var inputSystem = context.Scene.AddChild<InputSystemEntity, IFrameworkInputSource>(null);
            context.SetService<IInputSystem>(inputSystem);
        }
    }
}

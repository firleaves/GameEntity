using UnityEngine;

namespace GameEntity.Unity.Framework.Extension
{
    [DisallowMultipleComponent]
    public sealed class FrameworkExtensionLegacyInputSource : MonoBehaviour, IFrameworkInputSource
    {
        [SerializeField]
        private float mouseSensitivity = 0.08f;

        public bool TryReadInput(int frame, float time, out FrameworkInputFrame input)
        {
            var move = new Vector2(
                Axis(KeyCode.D, KeyCode.RightArrow, KeyCode.A, KeyCode.LeftArrow),
                Axis(KeyCode.W, KeyCode.UpArrow, KeyCode.S, KeyCode.DownArrow));
            var look = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;

            input = new FrameworkInputFrame(
                frame,
                time,
                move,
                look,
                Read(KeyCode.Space),
                Read(KeyCode.LeftShift),
                ReadMouseOrKey(0, KeyCode.J),
                Read(KeyCode.Tab, KeyCode.L),
                ReadMouseOrKey(1, KeyCode.K),
                Read(KeyCode.E),
                FrameworkInputSourceKind.Live);
            return true;
        }

        private static float Axis(KeyCode positiveA, KeyCode positiveB, KeyCode negativeA, KeyCode negativeB)
        {
            var value = 0f;
            if (Input.GetKey(positiveA) || Input.GetKey(positiveB))
            {
                value += 1f;
            }

            if (Input.GetKey(negativeA) || Input.GetKey(negativeB))
            {
                value -= 1f;
            }

            return value;
        }

        private static FrameworkInputButton Read(KeyCode key)
        {
            return new FrameworkInputButton(Input.GetKey(key), Input.GetKeyDown(key), Input.GetKeyUp(key));
        }

        private static FrameworkInputButton Read(KeyCode primary, KeyCode secondary)
        {
            return new FrameworkInputButton(
                Input.GetKey(primary) || Input.GetKey(secondary),
                Input.GetKeyDown(primary) || Input.GetKeyDown(secondary),
                Input.GetKeyUp(primary) || Input.GetKeyUp(secondary));
        }

        private static FrameworkInputButton ReadMouseOrKey(int mouseButton, KeyCode key)
        {
            return new FrameworkInputButton(
                Input.GetMouseButton(mouseButton) || Input.GetKey(key),
                Input.GetMouseButtonDown(mouseButton) || Input.GetKeyDown(key),
                Input.GetMouseButtonUp(mouseButton) || Input.GetKeyUp(key));
        }
    }
}

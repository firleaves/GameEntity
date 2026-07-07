using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameEntity.Unity.Framework.Extension
{
    [Serializable]
    public readonly struct FrameworkInputFrame
    {
        public readonly int Frame;
        public readonly float Time;
        public readonly Vector2 Move;
        public readonly Vector2 Look;
        public readonly FrameworkInputButton Jump;
        public readonly FrameworkInputButton Dash;
        public readonly FrameworkInputButton Attack;
        public readonly FrameworkInputButton LockOn;
        public readonly FrameworkInputButton Aim;
        public readonly FrameworkInputButton Interact;
        public readonly FrameworkInputSourceKind SourceKind;

        public FrameworkInputFrame(
            int frame,
            float time,
            Vector2 move,
            Vector2 look,
            FrameworkInputButton jump,
            FrameworkInputButton dash,
            FrameworkInputButton attack,
            FrameworkInputButton lockOn,
            FrameworkInputButton aim,
            FrameworkInputButton interact,
            FrameworkInputSourceKind sourceKind)
        {
            Frame = frame;
            Time = time;
            Move = Vector2.ClampMagnitude(move, 1f);
            Look = look;
            Jump = jump;
            Dash = dash;
            Attack = attack;
            LockOn = lockOn;
            Aim = aim;
            Interact = interact;
            SourceKind = sourceKind;
        }

        public bool HasAnyInput =>
            Move.sqrMagnitude > 0.0001f
            || Look.sqrMagnitude > 0.0001f
            || Jump.IsDown
            || Dash.IsDown
            || Attack.IsDown
            || LockOn.IsDown
            || Aim.IsDown
            || Interact.IsDown;

        public static FrameworkInputFrame Empty(int frame, float time, FrameworkInputSourceKind sourceKind = FrameworkInputSourceKind.None)
        {
            return new FrameworkInputFrame(
                frame,
                time,
                Vector2.zero,
                Vector2.zero,
                FrameworkInputButton.Up,
                FrameworkInputButton.Up,
                FrameworkInputButton.Up,
                FrameworkInputButton.Up,
                FrameworkInputButton.Up,
                FrameworkInputButton.Up,
                sourceKind);
        }
    }

}

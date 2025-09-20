using System.Collections.Generic;
using UnityEngine;

namespace Framework.Common.Input
{
    public enum InputAction
    {
        Down,
        Up,
        LongPress,
    }

    public abstract class BaseInputInfo
    {
        public readonly InputAction InputAction;

        protected BaseInputInfo(InputAction inputAction)
        {
            InputAction = inputAction;
        }
    }

    public class KeyInputInfo : BaseInputInfo
    {
        public readonly List<KeyCode> KeyCodes;

        public KeyInputInfo(InputAction inputAction, List<KeyCode> keyCodes) : base(inputAction)
        {
            KeyCodes = keyCodes;
        }
    }

    public class MouseInputInfo : BaseInputInfo
    {
        public readonly int MouseId;

        public MouseInputInfo(InputAction inputAction, int mouseId) : base(inputAction)
        {
            MouseId = mouseId;
        }
    } 
}
using System;
using System.Collections.Generic;
using Character;
using Framework.Common.Audio;
using Framework.Common.Function;
using Framework.Core.Attribute;
using Player;
using Player.StateMachine.Action;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Combo.Graph
{
    [Serializable]
    public class ComboGraphParameters
    {
        public PlayerCharacterObject playerCharacter;
        public UnityAction<ComboConfig, List<ComboTip>> onComboPlay;
    }
}
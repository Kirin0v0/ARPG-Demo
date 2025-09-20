// using System;
// using System.Collections.Generic;
// using Character;
// using Framework.Common.Audio;
// using Framework.Common.Function;
// using Framework.Core.Attribute;
// using Player;
// using Player.StateMachine.Action;
// using Sirenix.OdinInspector;
// using UnityEngine;
// using UnityEngine.Events;
// using UnityEngine.Serialization;
//
// namespace Combo.Tree
// {
//     [Serializable]
//     public class ComboTreeParameters
//     {
//         [FormerlySerializedAs("playerCharacterObject")] public PlayerCharacterObject playerCharacter;
//         public AudioManager audioManager;
//         public Transform audioParent;
//         public Transform effectParent;
//         public Transform colliderDetectionParent;
//         [FormerlySerializedAs("comboTreeExecutor")] public ComboTreeExecutor executor;
//         public UnityEvent<ComboConfig, List<ComboTip>> onComboPlay;
//     }
// }
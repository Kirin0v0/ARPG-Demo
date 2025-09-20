using System.Collections;
using Framework.Common.Debug;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace Character.Ability.Navigation
{
    public abstract class CharacterNavigationAbility : BaseCharacterOptionalAbility
    {
        public abstract bool InNavigation { get; protected set; }
        
        protected bool SynchronizeRotationWhenNavigation; // 在导航中是否同步角色面向
        
        public abstract void Tick(float deltaTime);

        public abstract void LateCheckNavigation(float deltaTime);

        public void OpenSynchronizeRotationWhenNavigation()
        {
            SynchronizeRotationWhenNavigation = true;
        }

        public void CloseSynchronizeRotationWhenNavigation()
        {
            SynchronizeRotationWhenNavigation = false;
        }
    }
}
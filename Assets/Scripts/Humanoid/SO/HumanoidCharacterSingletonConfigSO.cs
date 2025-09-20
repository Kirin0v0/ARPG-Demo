using System.Collections.Generic;
using Animancer.TransitionLibraries;
using Common;
using Framework.Core.Singleton;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Humanoid.SO
{
    [CreateAssetMenu(menuName = "Character/Humanoid/Singleton Config")]
    public class HumanoidCharacterSingletonConfigSO: SingletonSerializedScriptableObject<HumanoidCharacterSingletonConfigSO>
    {
        [Title("动画配置")] [InfoBox("配置不同种族的人形角色不同姿势下的移动动画")]
        public Dictionary<HumanoidCharacterRace, TransitionLibraryAsset> raceTransitionLibraryConfigurations;
        public Dictionary<HumanoidCharacterPose, TransitionLibraryAsset> poseTransitionLibraryConfigurations;
    }
}
using System.Collections.Generic;
using Common;
using Damage;
using Framework.Common.Audio;
using Map;
using Skill;
using Unity.VisualScripting;

namespace Character.BehaviourTree
{
    public struct CharacterBehaviourTreeParameters
    {
        public const string TargetParams = "target";
        public const string GotoParams = "goto";

        // 有状态的共享数据，每帧不会重新创建，而是通过外部传入，用于行为树内部节点共享先前的数据
        // 注意，在每次决策执行完毕后都会清空数据
        public Dictionary<string, object> Shared;

        #region 无状态数据，每帧重新创建

        public CharacterObject Character;
        public DamageManager DamageManager;
        public AlgorithmManager AlgorithmManager;
        public GameManager GameManager;
        public SkillManager SkillManager;
        public MapManager MapManager;

        #endregion
    }
}
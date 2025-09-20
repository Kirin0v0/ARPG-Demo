using System;
using System.Linq;
using Character.Brain;
using Character.Ability;
using Character.Ability.Animation;
using Character.Ability.Appearance;
using Character.Ability.Navigation;
using Character.Collider;
using Character.Data;
using Common;
using Damage.Data;
using NodeCanvas.DialogueTrees;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Skill.Runtime;
using Trade.Config;
using UnityEngine;
using VContainer;

namespace Character
{
    [Serializable]
    public class CharacterPosition
    {
        [SerializeField, ReadOnly] private Transform character;

        [SerializeField] private Transform center;
        public Transform Center => center;

        public Vector3 TransformCenterPoint(Vector3 offset)
        {
            return center.position + character.transform.TransformVector(offset);
        }

        [SerializeField] private Transform top;
        public Transform Top => top;

        public Vector3 TransformTopPoint(Vector3 offset)
        {
            return top.position + character.transform.TransformVector(offset);
        }

        [SerializeField] private Transform bottom;
        public Transform Bottom => bottom;

        public Vector3 TransformBottomPoint(Vector3 offset)
        {
            return bottom.position + character.transform.TransformVector(offset);
        }

        public void Check(Transform self)
        {
            character = self;
            center ??= self;
            top ??= self;
            bottom ??= self;
        }
    }

    [Serializable]
    public class CharacterVisual
    {
        [SerializeField, ReadOnly] private Transform character;

        [SerializeField] [InfoBox("角色模型视觉碰撞器，Layer必须是Character")]
        private CharacterCollider collider;

        public CharacterCollider Collider => collider;

        [SerializeField] private Transform center;
        public Transform Center => center;

        public Vector3 TransformCenterPoint(Vector3 offset)
        {
            return center.position + character.transform.TransformVector(offset);
        }

        [SerializeField] private Transform eye;
        public Transform Eye => eye;

        public Vector3 TransformEyePoint(Vector3 offset)
        {
            return eye.position + character.transform.TransformVector(offset);
        }

        [SerializeField] private Transform top;
        public Transform Top => top;

        public Vector3 TransformTopPoint(Vector3 offset)
        {
            return top.position + character.transform.TransformVector(offset);
        }

        [SerializeField] private Transform bottom;
        public Transform Bottom => bottom;

        public Vector3 TransformBottomPoint(Vector3 offset)
        {
            return bottom.position + character.transform.TransformVector(offset);
        }

        [SerializeField] private Transform left;
        public Transform Left => left;

        public Vector3 TransformLeftPoint(Vector3 offset)
        {
            return left.position + character.transform.TransformVector(offset);
        }

        [SerializeField] private Transform right;
        public Transform Right => right;

        public Vector3 TransformRightPoint(Vector3 offset)
        {
            return right.position + character.transform.TransformVector(offset);
        }

        public void Check(Transform self)
        {
            character = self;
            center ??= self;
            eye ??= self;
            top ??= self;
            bottom ??= self;
            left ??= self;
            right ??= self;
        }
    }

    [RequireComponent(typeof(CharacterController))]
    public class CharacterObject : SerializedMonoBehaviour
    {
        #region 角色基本能力

        public CharacterPropertyAbility PropertyAbility { get; protected set; }
        public CharacterResourceAbility ResourceAbility { get; protected set; }
        public CharacterControlAbility ControlAbility { get; protected set; }
        public CharacterStateAbility StateAbility { get; protected set; }

        #endregion

        [Title("角色位置配置")] [SerializeField] private CharacterPosition position;
        public CharacterPosition Position => position;

        [Title("角色视觉配置")] [SerializeField] private CharacterVisual visual;
        public CharacterVisual Visual => visual;

        [Title("角色碰撞配置")] [InfoBox("负责物理碰撞")] [SerializeField]
        private CharacterController characterController;

        public CharacterController CharacterController => characterController;

        [InfoBox("负责战斗攻击盒")] [SerializeField] private CharacterAttackBoxCollider attackBoxCollider;
        public CharacterCollider AttackBoxCollider => attackBoxCollider;

        [InfoBox("负责战斗受击盒")] [SerializeField] private CharacterHitBoxCollider hitBoxCollider;
        public CharacterHitBoxCollider HitBoxCollider => hitBoxCollider;

        [Title("角色AI组件")] [SerializeField] private CharacterBrain brain;
        public CharacterBrain Brain => brain;

        [Title("角色可选能力")] [SerializeField] private CharacterAnimationAbility animationAbility;
        public CharacterAnimationAbility AnimationAbility => animationAbility;

        [SerializeField] private CharacterAudioAbility audioAbility;
        public CharacterAudioAbility AudioAbility => audioAbility;

        [SerializeField] private CharacterEffectAbility effectAbility;
        public CharacterEffectAbility EffectAbility => effectAbility;

        [SerializeField] private CharacterStateChangeAbility stateChangeAbility;
        public CharacterStateChangeAbility StateChangeAbility => stateChangeAbility;

        [SerializeField] private CharacterAppearanceAbility appearanceAbility;
        public CharacterAppearanceAbility AppearanceAbility => appearanceAbility;

        [SerializeField] private CharacterMovementAbility movementAbility;
        public CharacterMovementAbility MovementAbility => movementAbility;

        [SerializeField] private CharacterGravityAbility gravityAbility;
        public CharacterGravityAbility GravityAbility => gravityAbility;

        [SerializeField] private CharacterNavigationAbility navigationAbility;
        public CharacterNavigationAbility NavigationAbility => navigationAbility;

        [SerializeField] private CharacterSkillAbility skillAbility;
        public CharacterSkillAbility SkillAbility => skillAbility;

        [SerializeField] private CharacterBuffAbility buffAbility;
        public CharacterBuffAbility BuffAbility => buffAbility;

        [SerializeField] private CharacterBattleAbility battleAbility;
        public CharacterBattleAbility BattleAbility => battleAbility;

        [SerializeField] private CharacterAtbRewardAbility atbRewardAbility;
        public CharacterAtbRewardAbility AtbRewardAbility => atbRewardAbility;

        [SerializeField] private CharacterInteractAbility interactAbility;
        public CharacterInteractAbility InteractAbility => interactAbility;

        [SerializeField] private CharacterDialogueAbility dialogueAbility;
        public CharacterDialogueAbility DialogueAbility => dialogueAbility;

        [SerializeField] private CharacterTradeAbility tradeAbility;
        public CharacterTradeAbility TradeAbility => tradeAbility;

        [Title("角色数据"), ShowInInspector, ReadOnly]
        public CharacterParameters Parameters { get; } = new();

        [Inject] protected AlgorithmManager AlgorithmManager;

        protected virtual void Awake()
        {
            PropertyAbility = new CharacterPropertyAbility(AlgorithmManager);
            ResourceAbility = new CharacterResourceAbility();
            ControlAbility = new CharacterControlAbility();
            StateAbility = new CharacterStateAbility();
        }

        /// <summary>
        /// 角色初始化函数，注意这里角色初始化先于角色参数设置，因此能力如果需要参数配合的话请在参数设置后额外执行对应函数
        /// </summary>
        public virtual void Init()
        {
            // 先初始化基本能力
            InitNecessaryAbility();
            // 再初始化可选能力
            InitOptionalAbility();
            // 最后初始化AI大脑
            brain?.Init(this);
        }

        public virtual void RenderUpdate(float deltaTime)
        {
            // 各种能力逻辑帧执行
            audioAbility?.Tick(deltaTime);
            effectAbility?.Tick(deltaTime);
            
            // 保证先检测地面和计算竖直速度,再执行导航
            gravityAbility?.UpdateGravity(deltaTime);
            navigationAbility?.Tick(deltaTime);
            
            // 最后是AI渲染帧执行
            brain?.UpdateRenderThoughts(deltaTime);
        }

        public virtual void LogicUpdate(float deltaTime)
        {
            // 优先执行属性、资源、控制和状态检查
            PropertyAbility.CheckProperty();
            ResourceAbility.CheckResource(deltaTime);
            ControlAbility.CheckControl();
            StateAbility.CheckState(deltaTime);

            // 各种能力逻辑帧执行
            battleAbility?.Tick(deltaTime);
            skillAbility?.Tick(deltaTime);
            buffAbility?.Tick(deltaTime);
            interactAbility?.Tick(deltaTime);
            tradeAbility?.Tick(deltaTime);
            dialogueAbility?.Tick(deltaTime);

            // 最后是AI逻辑帧执行
            brain?.UpdateLogicThoughts(deltaTime);
        }

        public virtual void PostUpdate(float renderDeltaTime, float logicDeltaTime)
        {
            animationAbility?.UpdateLayers();
            gravityAbility?.LateUpdateGravity(renderDeltaTime);
            movementAbility?.Tick(renderDeltaTime);
            navigationAbility?.LateCheckNavigation(renderDeltaTime);
        }

        public virtual void Destroy()
        {
            // 先销毁AI大脑
            brain?.Destroy();
            // 再销毁可选能力
            DestroyOptionalAbility();
            // 最后销毁基本能力
            DestroyNecessaryAbility();
        }

        public void SetCharacterParameters(
            int id,
            string prototype,
            string name,
            Vector3 spawnPoint,
            float spawnAngle,
            CharacterProperty baseProperty,
            float normalDamageMultiplier,
            float defenceDamageMultiplier,
            float brokenDamageMultiplier,
            DamageValueType weakness,
            DamageValueType immunity,
            CharacterSide side,
            CharacterDrop drop,
            object[] appearanceParameters = null,
            DialogueTree dialogueTree = null,
            TradeConfig tradeConfig = null,
            float reduceStunTime = 5f,
            bool destroyAfterDead = true,
            float destroyDelay = 2f,
            string[] tags = null,
            string[] abilitySkills = null
        )
        {
            // 立即设置角色位置和旋转量
            gameObject.transform.position = spawnPoint;
            gameObject.transform.rotation = Quaternion.AngleAxis(spawnAngle, Vector3.up);
            // 设置角色参数
            Parameters.id = id;
            Parameters.prototype = prototype;
            Parameters.name = name;
            Parameters.spawnPoint = spawnPoint;
            Parameters.position = gameObject.transform.position;
            Parameters.rotation = gameObject.transform.rotation;
            Parameters.forwardAngle = spawnAngle;
            Parameters.normalDamageMultiplier = normalDamageMultiplier;
            Parameters.defenceDamageMultiplier = defenceDamageMultiplier;
            Parameters.brokenDamageMultiplier = brokenDamageMultiplier;
            Parameters.weakness = weakness;
            Parameters.immunity = immunity;
            Parameters.damageMultiplier = normalDamageMultiplier;
            Parameters.side = side;
            Parameters.drop = drop;
            Parameters.tags = tags ?? Array.Empty<string>();
            // 设置基本能力参数
            ResourceAbility.SetReduceStunTime(reduceStunTime);
            PropertyAbility.BaseProperty = baseProperty;
            ControlAbility.CheckControl();
            StateAbility.SetDestroyParameters(destroyAfterDead, destroyDelay);
            // 设置外观
            appearanceAbility?.SetAppearance(appearanceParameters);
            // 设置对话树
            dialogueAbility?.SetDialogue(name, dialogueTree);
            // 设置交易数据
            tradeAbility?.Bind(tradeConfig);
            // 添加技能
            abilitySkills?.ForEach(skillId =>
            {
                if (string.IsNullOrEmpty(skillId))
                {
                    return;
                }

                skillAbility?.AddSkill(skillId, SkillGroup.Static);
            });
        }

        public bool HasTag(string tag)
        {
            if (Parameters.tags == null || Parameters.tags.Length <= 0) return false;
            return Parameters.tags.Contains(tag);
        }

        protected virtual void InitNecessaryAbility()
        {
            PropertyAbility.Init(this);
            ResourceAbility.Init(this);
            ControlAbility.Init(this);
            StateAbility.Init(this);
        }
        
        protected virtual void InitOptionalAbility()
        {
            stateChangeAbility?.Init(this);
            animationAbility?.Init(this);
            audioAbility?.Init(this);
            effectAbility?.Init(this);
            appearanceAbility?.Init(this);
            movementAbility?.Init(this);
            gravityAbility?.Init(this);
            navigationAbility?.Init(this);
            skillAbility?.Init(this);
            buffAbility?.Init(this);
            battleAbility?.Init(this);
            atbRewardAbility?.Init(this);
            interactAbility?.Init(this);
            dialogueAbility?.Init(this);
            tradeAbility?.Init(this);
        }
        
        protected virtual void DestroyNecessaryAbility()
        {
            PropertyAbility.Dispose();
            ResourceAbility.Dispose();
            ControlAbility.Dispose();
            StateAbility.Dispose();
        }
        
        protected virtual void DestroyOptionalAbility()
        {
            animationAbility?.Dispose();
            audioAbility?.Dispose();
            effectAbility?.Dispose();
            appearanceAbility?.Dispose();
            movementAbility?.Dispose();
            gravityAbility?.Dispose();
            navigationAbility?.Dispose();
            skillAbility?.Dispose();
            buffAbility?.Dispose();
            battleAbility?.Dispose();
            atbRewardAbility?.Dispose();
            interactAbility?.Dispose();
            dialogueAbility?.Dispose();
            tradeAbility?.Dispose();
            stateChangeAbility?.Dispose();
        }
        
        private void OnValidate()
        {
            position ??= new CharacterPosition();
            position.Check(transform);
            visual ??= new CharacterVisual();
            visual.Check(transform);
            characterController ??= GetComponent<CharacterController>();
        }
    }
}
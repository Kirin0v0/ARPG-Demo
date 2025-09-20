using System;
using Camera.Data;
using Character.Ability;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player
{
    public enum PlayerObstacleActionIdea
    {
        None,
        Vault,
        LowClimb,
        HighClimb,
        Hang,
    }

    [Serializable]
    public class PlayerCharacterParameters
    {
        [Title("玩家机制")] public bool inPerfectEvade = false; // 玩家是否处于完美闪避中，仅用于标识玩家状态，伤害判断不使用该字段计算伤害
        public bool inPerfectDefence = false; // 玩家是否处于完美防御中，仅用于标识玩家状态，伤害判断不使用该字段计算伤害
        public bool inAttack = false; // 玩家是否处于攻击中，仅用于标识玩家状态
        public bool inSkill = false; // 玩家是否处于技能释放中，仅用于标识玩家状态
        public Vector3 lastSafePosition; // 玩家最后的安全位置（可用于复活的位置）
        
        #region 渲染帧更新数据

        [Title("玩家移动输入")] public Vector2 playerInputRawValueInFrame; // 该帧中玩家移动输入的原始向量，二维向量
        public Vector3 playerInputMovementInFrame; // 该帧中玩家移动输入在相机坐标系的向量，三维向量
        public Vector3 playerInputCharacterMovementInFrame; // 该帧中玩家移动输入在角色坐标系的向量，仅用于转换后的八向移动，三维向量

        [Title("玩家按键输入")] public bool isSprintInFrame;
        public bool isJumpOrVaultOrClimbInFrame;
        public bool isEquipOrUnequipInFrame;
        public bool isEvadeInFrame;
        public bool isEnterDefendInFrame;
        public bool isDefendingInFrame;
        public bool isAttackInFrame;
        public bool isHeavyAttackInFrame;
        public bool isInteractInFrame;

        [Title("玩家相机锁定数据")] public CameraLockData cameraLockData;

        [Title("玩家闪避攻击计时")] public float evadeAttackCountdown;

        #endregion

        #region 逻辑帧更新数据

        [Title("玩家空中悬挂")] public bool allowHangInThisAirborneProcess;

        [Title("玩家障碍物检测")] public ObstacleData obstacleData;
        public PlayerObstacleActionIdea obstacleActionIdea;

        #endregion
    }
}
using Character.Ability.Navigation;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace Character.Ability.Navigation
{
    public class CharacterNavigationObstacleAbility : CharacterNavigationAbility
    {
        [Title("导航组件配置")] [SerializeField] [InfoBox("组件参数由代码内部控制，不需要提前配置")]
        private NavMeshObstacle navMeshObstacle;

        public NavMeshObstacle NavMeshObstacle => navMeshObstacle;

        public override bool InNavigation { get; protected set; } = false;

        protected override void OnInit()
        {
            base.OnInit();
            // 设置导航障碍
            navMeshObstacle.enabled = true;
            navMeshObstacle.shape = NavMeshObstacleShape.Capsule;
            navMeshObstacle.center = Owner.CharacterController.center;
            navMeshObstacle.height = Owner.CharacterController.height;
            navMeshObstacle.radius = Owner.CharacterController.radius +
                                     2 * GlobalRuleSingletonConfigSO.Instance.collideDetectionExtraRadius;
            navMeshObstacle.carveOnlyStationary = false;
            navMeshObstacle.carving = true;
        }

        public override void Tick(float deltaTime)
        {
        }

        public override void LateCheckNavigation(float deltaTime)
        {
        }
    }
}
using System;
using System.Collections;
using Character;
using Common;
using Damage;
using Damage.Data;
using Events;
using Features.Game.UI;
using Framework.Common.UI.Toast;
using Player;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Interact
{
    [RequireComponent(typeof(GUIContentShowBehaviour))]
    public class InteractableRestSpotObject : InteractableObject
    {
        [Inject] private GameManager _gameManager;
        [Inject] private PlayerDataManager _playerDataManager;
        [Inject] private DamageManager _damageManager;
        [Inject] private GameUIModel _gameUIModel;

        [Title("运行时数据")] [ShowInInspector] public int Id { get; private set; } = -1;

        private GUIContentShowBehaviour _contentShowBehaviour;

        private void Awake()
        {
            _contentShowBehaviour = GetComponent<GUIContentShowBehaviour>();
            _contentShowBehaviour.AllowGUIShow =
                _gameUIModel.AllowGUIShowing().HasValue() && _gameUIModel.AllowGUIShowing().Value;
        }

        private void Update()
        {
            _contentShowBehaviour.enabled = visible;
        }

        public void Init(int id)
        {
            Id = id;
        }

        public override bool AllowInteract(GameObject target)
        {
            if (!gameObject)
            {
                return false;
            }

            var character = target.GetComponent<CharacterObject>();
            if (character != _gameManager.Player || character.Parameters.dead ||
                character.Parameters.battleState != CharacterBattleState.Idle)
            {
                return false;
            }

            return true;
        }

        public override void Interact(GameObject target)
        {
            var character = target.GetComponent<CharacterObject>();
            // 恢复角色所有资源
            character.ResourceAbility.FillResource(false);
            // 发送地图刷新事件
            GameApplication.Instance.EventCenter.TriggerEvent(GameEvents.RefreshMap);
            // 自动存档
            GameApplication.Instance.ArchiveManager.NotifySave(true);
            Toast.Instance.Show("自动保存成功");
        }

        public override string Tip(GameObject target) => "休息";
    }
}
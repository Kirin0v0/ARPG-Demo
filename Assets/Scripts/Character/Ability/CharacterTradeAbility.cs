using System.Collections.Generic;
using Package.Data;
using Sirenix.OdinInspector;
using Trade.Config;
using Trade.Data;
using Trade.Runtime;
using UnityEngine;

namespace Character.Ability
{
    public abstract class CharacterTradeAbility : BaseCharacterOptionalAbility
    {
        [FoldoutGroup("运行时数据")]
        [ShowInInspector, ReadOnly]
        public TradeInventory Inventory { get; protected set; } = new();

        [FoldoutGroup("运行时数据")]
        [ShowInInspector, ReadOnly]
        public TradeRule Rule { get; protected set; } = new();

        [FoldoutGroup("运行时数据")]
        [ShowInInspector, ReadOnly]
        public int Money { set; get; } = 0;

        public abstract void Bind(TradeConfig tradeConfig);

        public abstract void Tick(float deltaTime);

        public virtual void OnSell(TradeOrder order)
        {
        }

        public virtual void OnPay(TradeOrder order)
        {
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            Inventory = new();
            Rule = new();
            Money = 0;
        }
    }
}
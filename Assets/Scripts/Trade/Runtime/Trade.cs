using System;
using Character;
using Unity.Collections;
using UnityEngine;

namespace Trade.Runtime
{
    [Serializable]
    public class Trade
    {
        [SerializeField, ReadOnly] private string serialNumber; // 流水号
        public string SerialNumber => serialNumber;
        
        [SerializeField, ReadOnly] private CharacterObject a; // 交易角色A
        public CharacterObject A => a;
        
        [SerializeField, ReadOnly] private CharacterObject b; // 交易角色B
        public CharacterObject B => b;
        
        [SerializeField, ReadOnly] private TradeManifest manifest = new(); // 交易清单
        public TradeManifest Manifest => manifest;
        
        private System.Action _finish; // 结束委托

        public Trade(string serialNumber, CharacterObject a, CharacterObject b, System.Action finish)
        {
            this.serialNumber = serialNumber;
            this.a = a;
            this.b = b;
            _finish = finish;
        }

        public bool IsSameTrade(CharacterObject a, CharacterObject b)
        {
            return (this.a == a && this.b == b) || (this.a == b && this.b == a);
        }

        public bool ContainsCharacter(CharacterObject character)
        {
            return a == character || b == character;
        }

        public void SetManifest(TradeManifest manifest)
        {
            this.manifest = manifest;
        }

        public void Finish()
        {
            _finish?.Invoke();
        }
    }
}
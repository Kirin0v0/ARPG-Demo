using System;
using System.Collections.Generic;
using Buff.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Buff.SO
{
    [Serializable]
    public class BitBuffTagRule
    {
        [ReadOnly] public BuffTag tag;
        public BuffTag blockTags;
        public BuffTag removedTags;
    }

    [CreateAssetMenu(menuName = "Buff/Tag Rule/Bit")]
    public class BitBuffTagRuleSO : BaseBuffTagRuleSO
    {
        [LabelText("标签规则"),
         InfoBox("Tag：自身标签类\nBlock Tags：在自身存在时禁止添加以下标签的Buff\nRemoved Tags：在自身添加后移除以下标签的Buff"),
         TableList(IsReadOnly = true),
         SerializeField]
        private List<BitBuffTagRule> rules;

        public override bool AllowNewBuffAddWhenOldBuffExists(BuffTag newBuffTag, BuffTag oldBuffTag)
        {
            // 遍历规则，查找满足旧Buff标签的规则是否禁止新Buff标签添加
            foreach (var rule in rules)
            {
                if ((oldBuffTag & rule.tag) != 0)
                {
                    if ((rule.blockTags & newBuffTag) != 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public override bool IsRemovedOldBuffAfterNewBuffAdded(BuffTag oldBuffTag, BuffTag newBuffTag)
        {
            // 遍历规则，查找满足新Buff标签的规则是否在添加后移除旧Buff标签
            foreach (var rule in rules)
            {
                if ((newBuffTag & rule.tag) != 0)
                {
                    if ((rule.removedTags & oldBuffTag) != 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        [OnInspectorInit]
        private void InitData()
        {
            if (rules.Count != 0)
            {
                return;
            }

            foreach (var buffTagValue in Enum.GetValues(typeof(BuffTag)))
            {
                var buffTag = (BuffTag)buffTagValue;
                if (buffTag == BuffTag.None)
                {
                    continue;
                }

                rules.Add(new BitBuffTagRule
                {
                    tag = buffTag,
                    blockTags = BuffTag.None,
                    removedTags = BuffTag.None,
                });
            }
        }

        [Button(name: "重置规则")]
        private void ResetRules()
        {
            rules.Clear();
            InitData();
        }
    }
}
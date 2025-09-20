using System;
using System.Collections.Generic;
using Buff.Data;
using Framework.Common.Debug;
using Framework.Common.Function;
using Framework.Common.UI.Panel;
using Framework.Common.UI.PopupText;
using Framework.Core.Extension;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;
using Random = UnityEngine.Random;

namespace Common
{
    public enum PopupTextType
    {
        NoTypeDamage,
        PhysicsDamage,
        FireDamage,
        IceDamage,
        WindDamage,
        LightningDamage,
        MpDamage,
        HpHeal,
        MpHeal,
    }
    
    public class PopupTextManager : SerializedMonoBehaviour
    {
        [Title("伤害跳字配置")] [SerializeField] private PopupTextAsset noTypeDamagePopupTextAsset;
        [SerializeField] private PopupTextAsset physicsDamagePopupTextAsset;
        [SerializeField] private PopupTextAsset fireDamagePopupTextAsset;
        [SerializeField] private PopupTextAsset iceDamagePopupTextAsset;
        [SerializeField] private PopupTextAsset windDamagePopupTextAsset;
        [SerializeField] private PopupTextAsset lightningDamagePopupTextAsset;
        [SerializeField] private PopupTextAsset mpDamagePopupTextAsset;
        [SerializeField] private PopupTextAsset hpHealPopupTextAsset;
        [SerializeField] private PopupTextAsset mpHealPopupTextAsset;
        [SerializeField] private PopupTextPositionType damagePositionType;
        [SerializeField] private Vector2 damageRandomPositionRange;

        [Title("Buff跳字字体配置")] [SerializeField] private PopupTextAsset defaultBuffPopupTextAsset;
        [SerializeField] private Dictionary<BuffTag, PopupTextAsset> buffTagPopupTextAssets = new();
        [SerializeField] private PopupTextPositionType buffPositionType;
        [SerializeField] private Vector2 buffRandomPositionRange;

        [Inject] private UGUIPanelManager _panelManager;

        private readonly List<PopupText> _showingPopupTexts = new();
        private ObjectPool<PopupText> _popupTextPool;

        private void Awake()
        {
            _popupTextPool = new ObjectPool<PopupText>(
                createFunction: () =>
                {
                    var popupText = new GameObject("PopupText")
                    {
                        transform = { parent = _panelManager.GetLayer(UGUIPanelLayer.Middle) }
                    }.AddComponent<PopupText>();
                    return popupText;
                },
                destroyFunction: (popupText) =>
                {
                    if (!popupText.IsGameObjectDestroyed())
                    {
                        GameObject.Destroy(popupText.gameObject);
                    }
                },
                defaultCapacity: 20,
                maxSize: 500
            );
        }

        private void OnDestroy()
        {
            _showingPopupTexts.ForEach(popupText => { _popupTextPool.Release(popupText, null); });
            _showingPopupTexts.Clear();
            _popupTextPool.Clear();
        }

        public void ShowDamagePopupText(
            string valueText,
            string extraText,
            PopupTextType type,
            Vector3 hitPosition,
            bool isCritical,
            Vector3 hitDirection
        )
        {
            var popupTextAsset = type switch
            {
                PopupTextType.NoTypeDamage => noTypeDamagePopupTextAsset,
                PopupTextType.PhysicsDamage => physicsDamagePopupTextAsset,
                PopupTextType.FireDamage => fireDamagePopupTextAsset,
                PopupTextType.IceDamage => iceDamagePopupTextAsset,
                PopupTextType.WindDamage => windDamagePopupTextAsset,
                PopupTextType.LightningDamage => lightningDamagePopupTextAsset,
                PopupTextType.MpDamage => mpDamagePopupTextAsset,
                PopupTextType.HpHeal => hpHealPopupTextAsset,
                PopupTextType.MpHeal => mpHealPopupTextAsset,
            };
            var text = (isCritical ? $"{valueText}!!!" : $"{valueText}") + extraText;
            var randomOffset = Vector3.zero;
            switch (damagePositionType)
            {
                case PopupTextPositionType.Screen:
                    randomOffset = Random.insideUnitCircle * damageRandomPositionRange;
                    break;
                case PopupTextPositionType.World:
                    var random = Random.insideUnitCircle;
                    randomOffset = new Vector3(
                        UnityEngine.Camera.main.transform.right.x * random.x * damageRandomPositionRange.x,
                        random.y * damageRandomPositionRange.y,
                        UnityEngine.Camera.main.transform.right.z * random.x * damageRandomPositionRange.x
                    );
                    break;
            }

            var toRight = Vector3.Cross(UnityEngine.Camera.main?.transform.forward ?? Vector3.forward, hitDirection).y > 0;
            _popupTextPool.Get(popupText =>
            {
                _showingPopupTexts.Add(popupText);
                popupText.Show(
                    popupTextAsset,
                    text,
                    damagePositionType,
                    hitPosition,
                    new Vector3(randomOffset.x, randomOffset.y, 0),
                    toRight,
                    () =>
                    {
                        _showingPopupTexts.Remove(popupText);
                        _popupTextPool.Release(popupText, null);
                    });
            });
        }

        public void ShowAddBuffPopupText(
            string buffName,
            BuffTag buffTag,
            Vector3 castPosition,
            Vector3 castDirection
        )
        {
            ShowBuffPopupText($"+【{buffName}】", buffTag, castPosition, castDirection);
        }

        public void ShowRemoveBuffPopupText(
            string buffName,
            BuffTag buffTag,
            Vector3 castPosition,
            Vector3 castDirection
        )
        {
            ShowBuffPopupText($"-【{buffName}】", buffTag, castPosition, castDirection);
        }

        private void ShowBuffPopupText(
            string popupText,
            BuffTag buffTag,
            Vector3 castPosition,
            Vector3 castDirection)
        {
            var popupTextAsset = GetBuffPopupTextAsset(buffTag);
            var text = popupText;
            var randomOffset = Vector3.zero;
            switch (buffPositionType)
            {
                case PopupTextPositionType.Screen:
                    randomOffset = Random.insideUnitCircle * buffRandomPositionRange;
                    break;
                case PopupTextPositionType.World:
                    var random = Random.insideUnitCircle;
                    randomOffset = new Vector3(
                        UnityEngine.Camera.main.transform.right.x * random.x * buffRandomPositionRange.x,
                        random.y * buffRandomPositionRange.y,
                        UnityEngine.Camera.main.transform.right.z * random.x * buffRandomPositionRange.x
                    );
                    break;
            }

            var toRight = Vector3.Cross(UnityEngine.Camera.main?.transform.forward ?? Vector3.forward, castDirection).y > 0;
            _popupTextPool.Get(popupText =>
            {
                _showingPopupTexts.Add(popupText);
                popupText.Show(
                    popupTextAsset,
                    text,
                    buffPositionType,
                    castPosition,
                    new Vector3(randomOffset.x, randomOffset.y, 0),
                    toRight,
                    () =>
                    {
                        _showingPopupTexts.Remove(popupText);
                        _popupTextPool.Release(popupText, null);
                    });
            });
        }

        private PopupTextAsset GetBuffPopupTextAsset(BuffTag buffTag)
        {
            return buffTagPopupTextAssets.GetValueOrDefault(buffTag, defaultBuffPopupTextAsset);
        }
    }
}
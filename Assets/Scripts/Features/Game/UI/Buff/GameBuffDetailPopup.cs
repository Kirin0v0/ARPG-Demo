using System;
using Features.Game.Data;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Game.UI.Character
{
    public class GameBuffDetailPopup :  BasePopup<GameBuffDetailUIData>
    {
        [Title("UI关联")] [SerializeField] private TextMeshProUGUI textDetailName;
        [SerializeField] private Image imgDetailThumbnail;
        [SerializeField] private TextMeshProUGUI textDetailIntroduction;
        [SerializeField] private TextMeshProUGUI textDetailDuration;
        [SerializeField] private TextMeshProUGUI textDetailMaxStack;
        [SerializeField] private TextMeshProUGUI textDetailCaster;
        
        protected override void UpdateContent()
        {
            // 重置所有UI
            textDetailName.gameObject.SetActive(false);
            imgDetailThumbnail.gameObject.SetActive(false);
            textDetailIntroduction.gameObject.SetActive(false);
            textDetailDuration.gameObject.SetActive(false);
            textDetailMaxStack.gameObject.SetActive(false);
            textDetailCaster.gameObject.SetActive(false);

            if (Data == null)
            {
                return;
            }

            textDetailName.gameObject.SetActive(true);
            textDetailName.text = Data.Name + (Data.MaxStack > 1 ? $"({Data.Stack}层)" : "");
            imgDetailThumbnail.gameObject.SetActive(true);
            imgDetailThumbnail.sprite = Data.Icon;
            textDetailIntroduction.gameObject.SetActive(true);
            textDetailIntroduction.text = Data.Description;
            textDetailDuration.gameObject.SetActive(true);
            textDetailDuration.text = !Data.Permanent ? $"剩余时间: {Data.Duration.ToString("F1")}秒" : "永久存在";
            textDetailMaxStack.gameObject.SetActive(Data.MaxStack > 1);
            textDetailMaxStack.text = $"最大层数：{Data.MaxStack}";
            textDetailCaster.gameObject.SetActive(true);
            textDetailCaster.text = $"来源：{Data.CasterName}";
        }
    }
}
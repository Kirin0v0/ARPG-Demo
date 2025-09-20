using Character;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Game.UI.Map.Information
{
    public class GameMapAllyInformation : GameMapInformation
    {
        [SerializeField] private Image imgIcon;
        [SerializeField] private TextMeshProUGUI textName;

        public void SetInformation(CharacterObject ally)
        {
            // 查找全局信息
            var information = GlobalRuleSingletonConfigSO.Instance.allyInformation.Find(x => ally.HasTag(x.tag));
            if (information == null)
            {
                SetIconAndNameColor(GlobalRuleSingletonConfigSO.Instance.allyDefaultColor);
                SetNameText(ally.Parameters.name);
            }
            else
            {
                SetIconAndNameColor(information.color);
                SetNameText(information.prefixTitle + ally.Parameters.name);
            }
        }

        private void SetIconAndNameColor(Color color)
        {
            if (imgIcon)
            {
                imgIcon.color = color;
            }

            if (textName)
            {
                textName.color = color;
            }
        }

        private void SetNameText(string name)
        {
            if (!textName)
            {
                return;
            }

            textName.text = name;
        }
    }
}
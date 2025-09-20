using Character;
using TMPro;
using UnityEngine;

namespace Features.Game.UI.Map.Information
{
    public class GameMapEnemyInformation : GameMapInformation
    {
        [SerializeField] private TextMeshProUGUI textName;

        public void SetInformation(CharacterObject enemy)
        {
            if (textName)
            {
                textName.text = enemy.Parameters.name;
            }
        }
    }
}
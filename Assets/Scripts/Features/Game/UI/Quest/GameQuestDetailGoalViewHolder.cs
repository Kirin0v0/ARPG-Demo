using Features.Game.Data;
using Framework.Common.Debug;
using Framework.Common.UI.RecyclerView;
using Quest.Config.Step;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Game.UI.Quest
{
    public class GameQuestDetailGoalViewHolder : RecyclerViewHolder
    {
        [SerializeField] private GameObject indexLayout;
        [SerializeField] private Image imgIndexCompleted;
        [SerializeField] private TextMeshProUGUI textIndex;

        [SerializeField] private GameObject checkLayout;
        [SerializeField] private Image imgCheckInProgress;
        [SerializeField] private Image imgCheckCompleted;

        [SerializeField] private TextMeshProUGUI textDescription;

        public void BindData(GameQuestDetailGoalUIData data)
        {
            switch (data.Relation)
            {
                case QuestStepGoalRelation.Linear:
                {
                    indexLayout.gameObject.SetActive(true);
                    checkLayout.gameObject.SetActive(false);
                    textIndex.gameObject.SetActive(data.ShowIndex);
                    textIndex.text = $"{data.Index + 1}.";
                    imgIndexCompleted.gameObject.SetActive(data.Completed);
                }
                    break;
                case QuestStepGoalRelation.ParallelAnd:
                case QuestStepGoalRelation.ParallelOr:
                default:
                {
                    indexLayout.gameObject.SetActive(false);
                    checkLayout.gameObject.SetActive(true);
                    imgCheckInProgress.gameObject.SetActive(!data.Completed);
                    imgCheckCompleted.gameObject.SetActive(data.Completed);
                }
                    break;
            }

            textDescription.text = data.Description;
        }

        public void UnbindData()
        {
        }
    }
}
using DG.Tweening;
using Events;
using Framework.Common.UI.Panel;
using Skill;
using Skill.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Game.UI
{
    public class GameSkillCommandPanel : BaseUGUIPanel
    {
        private Image _imgSkillPopup;
        [SerializeField] private RectTransform showPoint;
        [SerializeField] private RectTransform hidePoint;

        private TextMeshProUGUI _textSkillName;
        private Sequence _doTweenSequence;

        protected override void OnInit()
        {
            _imgSkillPopup = GetWidget<Image>("ImgSkillPopup");
            _textSkillName = GetWidget<TextMeshProUGUI>("TextSkillName");
            _imgSkillPopup.transform.position = hidePoint.position;
        }

        protected override void OnShow(object payload)
        {
            // 监听技能释放和结束事件
            GameApplication.Instance.EventCenter.AddEventListener<SkillReleaseInfo>(GameEvents.ReleasePlayerSkill,
                OnReleasePlayerSkill);
            GameApplication.Instance.EventCenter.AddEventListener<SkillReleaseInfo>(
                GameEvents.CompletePlayerSkill, OnCompletePlayerSkill);
        }

        protected override void OnShowingUpdate(bool focus)
        {
        }

        protected override void OnHide()
        {
            // 取消监听技能释放和结束事件
            GameApplication.Instance?.EventCenter.RemoveEventListener<SkillReleaseInfo>(
                GameEvents.ReleasePlayerSkill, OnReleasePlayerSkill);
            GameApplication.Instance?.EventCenter.RemoveEventListener<SkillReleaseInfo>(
                GameEvents.CompletePlayerSkill, OnCompletePlayerSkill);
        }

        private void OnReleasePlayerSkill(SkillReleaseInfo skillReleaseInfo)
        {
            _textSkillName.text = skillReleaseInfo.Name;

            _doTweenSequence?.Kill();
            _doTweenSequence = DOTween.Sequence();
            _doTweenSequence.Append(_imgSkillPopup.transform.DOMove(showPoint.transform.position, 0.5f))
                .AppendCallback(() => { _doTweenSequence = null; });
        }

        private void OnCompletePlayerSkill(SkillReleaseInfo skillReleaseInfo)
        {
            _doTweenSequence?.Kill();
            _doTweenSequence = DOTween.Sequence();
            _doTweenSequence.Append(_imgSkillPopup.transform.DOMove(hidePoint.transform.position, 0.5f))
                .AppendCallback(() => { _doTweenSequence = null; });
        }
    }
}
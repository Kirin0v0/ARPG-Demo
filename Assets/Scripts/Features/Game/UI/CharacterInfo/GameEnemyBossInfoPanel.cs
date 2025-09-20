using System;
using System.Linq;
using Buff.Data;
using Character;
using DG.Tweening;
using Features.Game.Data;
using Framework.Common.Debug;
using Framework.Common.UI.Panel;
using Framework.Common.UI.RecyclerView;
using Framework.Common.UI.RecyclerView.Adapter;
using Framework.Common.UI.RecyclerView.LayoutManager;
using Framework.Common.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Game.UI.CharacterInfo
{
    public class GameEnemyBossInfoPanel : BaseUGUIPanel
    {
        private TextMeshProUGUI _textSkill;
        private TextMeshProUGUI _textBroken;
        private HorizontalLayoutGroup _idleStateBar;
        private TextMeshProUGUI _textIdleTitle;
        private HorizontalLayoutGroup _warningStateBar;
        private TextMeshProUGUI _textWarningTitle;
        private HorizontalLayoutGroup _battleStateBar;
        private TextMeshProUGUI _textBattleTitle;
        private Slider _sliderHpFillArea;
        private Slider _sliderStunFillArea;
        private Slider _sliderBreakFillArea;

        private RecyclerView _rvBuffList;
        [SerializeField] private RecyclerViewGridLayoutManager buffListLayoutManager;
        [SerializeField] private RecyclerViewAdapter buffListAdapter;

        private CharacterObject _boss;

        protected override void OnInit()
        {
            _textSkill = GetWidget<TextMeshProUGUI>("TextSkill");
            _textBroken = GetWidget<TextMeshProUGUI>("TextBroken");
            _idleStateBar = GetWidget<HorizontalLayoutGroup>("IdleStateBar");
            _textIdleTitle = GetWidget<TextMeshProUGUI>("TextIdleTitle");
            _warningStateBar = GetWidget<HorizontalLayoutGroup>("WarningStateBar");
            _textWarningTitle = GetWidget<TextMeshProUGUI>("TextWarningTitle");
            _battleStateBar = GetWidget<HorizontalLayoutGroup>("BattleStateBar");
            _textBattleTitle = GetWidget<TextMeshProUGUI>("TextBattleTitle");
            _sliderHpFillArea = GetWidget<Slider>("SliderHpFillArea");
            _sliderStunFillArea = GetWidget<Slider>("SliderStunFillArea");
            _sliderBreakFillArea = GetWidget<Slider>("SliderBreakFillArea");
            _rvBuffList = GetWidget<RecyclerView>("RvBuffList");
            _rvBuffList.Init();
            _rvBuffList.LayoutManager = buffListLayoutManager;
            _rvBuffList.Adapter = buffListAdapter;
        }

        protected override void OnShow(object payload)
        {
        }

        protected override void OnShowingUpdate(bool focus)
        {
            if (!_boss)
            {
                return;
            }

            UpdateCharacterInfo(true);
        }

        protected override void OnHide()
        {
        }

        public void BindBoss(CharacterObject boss)
        {
            _boss = boss;
            UpdateCharacterInfo(false);
        }

        public void UnbindBoss()
        {
            _boss = null;
        }

        private void UpdateCharacterInfo(bool tickUpdate)
        {
            // 获取最新的正在释放的技能，如果没有就不展示技能文字
            if (_boss.SkillAbility && _boss.SkillAbility.NewestReleasingSkill != null)
            {
                _textSkill.gameObject.SetActive(true);
                _textSkill.text = $"【{_boss.SkillAbility.NewestReleasingSkill.Name}】";
            }
            else
            {
                _textSkill.gameObject.SetActive(false);
                _textSkill.text = "";
            }

            _textBroken.gameObject.SetActive(_boss.Parameters.broken);

            switch (_boss.Parameters.battleState)
            {
                case CharacterBattleState.Idle:
                    _idleStateBar.gameObject.SetActive(true);
                    _warningStateBar.gameObject.SetActive(false);
                    _battleStateBar.gameObject.SetActive(false);
                    break;
                case CharacterBattleState.Warning:
                    _warningStateBar.gameObject.SetActive(true);
                    _idleStateBar.gameObject.SetActive(false);
                    _battleStateBar.gameObject.SetActive(false);
                    break;
                case CharacterBattleState.Battle:
                    _battleStateBar.gameObject.SetActive(true);
                    _idleStateBar.gameObject.SetActive(false);
                    _warningStateBar.gameObject.SetActive(false);
                    break;
            }

            _textIdleTitle.text = _boss.Parameters.name;
            _textWarningTitle.text = _boss.Parameters.name;
            _textBattleTitle.text = _boss.Parameters.name;
            if (tickUpdate)
            {
                DOTween.To(() => _sliderHpFillArea.value, x => _sliderHpFillArea.value = x,
                    1f * _boss.Parameters.resource.hp / _boss.Parameters.property.maxHp, 0.5f);
                DOTween.To(() => _sliderStunFillArea.value, x => _sliderStunFillArea.value = x,
                    1f * _boss.Parameters.resource.stun / _boss.Parameters.property.stunMeter, 0.2f);
                DOTween.To(() => _sliderBreakFillArea.value, x => _sliderBreakFillArea.value = x,
                    1f * _boss.Parameters.resource.@break / _boss.Parameters.property.breakMeter, 0.2f);
            }
            else
            {
                _sliderHpFillArea.value = 1f * _boss.Parameters.resource.hp / _boss.Parameters.property.maxHp;
                _sliderStunFillArea.value = 1f * _boss.Parameters.resource.stun / _boss.Parameters.property.stunMeter;
                _sliderBreakFillArea.value =
                    1f * _boss.Parameters.resource.@break / _boss.Parameters.property.breakMeter;
            }

            // 动态展示Buff栏网格布局高度
            var size = _rvBuffList.GetComponent<RectTransform>().sizeDelta;
            var rowNumber = _rvBuffList.Adapter.GetItemCount() / buffListLayoutManager.GetSpanCount() +
                            (_rvBuffList.Adapter.GetItemCount() % buffListLayoutManager.GetSpanCount() > 0 ? 1 : 0);
            _rvBuffList.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x,
                rowNumber * buffListAdapter.GetViewHolderTemplate(0).GetComponent<RectTransform>().sizeDelta.y +
                Mathf.Max(rowNumber - 1, 0) * buffListLayoutManager.GetSpacing());
            UGUIUtil.RefreshLayoutGroupsImmediateAndRecursive(gameObject);
            buffListAdapter.SetData(_boss.Parameters.buffs
                .Where(buff => buff.info.visibility == BuffVisibility.FullVisible)
                .Select(buff => buff.ToSimpleUIData())
                .ToList()
            );
        }
    }
}
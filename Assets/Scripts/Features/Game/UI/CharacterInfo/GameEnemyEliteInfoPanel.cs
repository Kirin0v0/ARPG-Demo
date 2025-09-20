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
    public class GameEnemyEliteInfoPanel : BaseUGUIPanel
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
        private Slider _sliderBreakFillArea;

        private RecyclerView _rvBuffList;
        [SerializeField] private RecyclerViewGridLayoutManager buffListLayoutManager;
        [SerializeField] private RecyclerViewAdapter buffListAdapter;

        private CharacterObject _enemy;

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
            if (!_enemy)
            {
                return;
            }

            UpdateCharacterInfo(true);
        }

        protected override void OnHide()
        {
        }

        public void BindEnemy(CharacterObject enemy)
        {
            _enemy = enemy;
            UpdateCharacterInfo(false);
        }

        public void UnbindEnemy()
        {
            _enemy = null;
        }

        private void UpdateCharacterInfo(bool tickUpdate)
        {
            // 获取最新的正在释放的技能，如果没有就不展示技能文字
            if (_enemy.SkillAbility && _enemy.SkillAbility.NewestReleasingSkill != null)
            {
                _textSkill.gameObject.SetActive(true);
                _textSkill.text = $"【{_enemy.SkillAbility.NewestReleasingSkill.Name}】";
            }
            else
            {
                _textSkill.gameObject.SetActive(false);
                _textSkill.text = "";
            }

            _textBroken.gameObject.SetActive(_enemy.Parameters.broken);

            switch (_enemy.Parameters.battleState)
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

            _textIdleTitle.text = _enemy.Parameters.name;
            _textWarningTitle.text = _enemy.Parameters.name;
            _textBattleTitle.text = _enemy.Parameters.name;
            if (tickUpdate)
            {
                DOTween.To(() => _sliderHpFillArea.value, x => _sliderHpFillArea.value = x,
                    1f * _enemy.Parameters.resource.hp / _enemy.Parameters.property.maxHp, 0.5f);
                DOTween.To(() => _sliderBreakFillArea.value, x => _sliderBreakFillArea.value = x,
                    1f * _enemy.Parameters.resource.@break / _enemy.Parameters.property.breakMeter, 0.2f);
            }
            else
            {
                _sliderHpFillArea.value = 1f * _enemy.Parameters.resource.hp / _enemy.Parameters.property.maxHp;
                _sliderBreakFillArea.value =
                    1f * _enemy.Parameters.resource.@break / _enemy.Parameters.property.breakMeter;
            }

            // 动态展示Buff栏网格布局高度
            var size = _rvBuffList.GetComponent<RectTransform>().sizeDelta;
            var rowNumber = _rvBuffList.Adapter.GetItemCount() / buffListLayoutManager.GetSpanCount() +
                            (_rvBuffList.Adapter.GetItemCount() % buffListLayoutManager.GetSpanCount() > 0 ? 1 : 0);
            _rvBuffList.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x,
                rowNumber * buffListAdapter.GetViewHolderTemplate(0).GetComponent<RectTransform>().sizeDelta.y +
                Mathf.Max(rowNumber - 1, 0) * buffListLayoutManager.GetSpacing());
            UGUIUtil.RefreshLayoutGroupsImmediateAndRecursive(gameObject);
            buffListAdapter.SetData(_enemy.Parameters.buffs
                .Where(buff => buff.info.visibility == BuffVisibility.FullVisible)
                .Select(buff => buff.ToSimpleUIData())
                .ToList()
            );
        }
    }
}
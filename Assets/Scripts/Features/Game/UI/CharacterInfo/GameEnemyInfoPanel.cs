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
using Skill;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Game.UI.CharacterInfo
{
    public class GameEnemyInfoPanel : BaseUGUIPanel
    {
        private TextMeshProUGUI _textSkill;
        private TextMeshProUGUI _textBroken;
        private TextMeshProUGUI _textState;
        private TextMeshProUGUI _textTitle;
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
            _textState = GetWidget<TextMeshProUGUI>("TextState");
            _textTitle = GetWidget<TextMeshProUGUI>("TextTitle");
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
                    _textState.text = "X";
                    break;
                case CharacterBattleState.Warning:
                    _textState.text = "?";
                    break;
                case CharacterBattleState.Battle:
                    _textState.text = "!";
                    break;
            }

            _textTitle.text = _enemy.Parameters.name;
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
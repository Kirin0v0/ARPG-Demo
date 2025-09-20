using System;
using System.Linq;
using Buff.Data;
using Common;
using DG.Tweening;
using Features.Game.Data;
using Framework.Common.UI.Panel;
using Framework.Common.UI.RecyclerView;
using Framework.Common.UI.RecyclerView.Adapter;
using Framework.Common.UI.RecyclerView.LayoutManager;
using Framework.Common.Util;
using Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Features.Game.UI
{
    public class GamePlayerResourcePanel : BaseUGUIPanel
    {
        [Inject] private GameManager _gameManager;

        private PlayerCharacterObject Player => _gameManager.Player;

        private Slider _sliderHpFillArea;
        private TextMeshProUGUI _textHpValue;
        private Slider _sliderMpFillArea;
        private TextMeshProUGUI _textMpValue;
        private Slider _sliderFirstAtbFillArea;
        private Slider _sliderSecondAtbFillArea;
        private Slider _sliderThirdAtbFillArea;

        private RecyclerView _rvBuffList;
        [SerializeField] private RecyclerViewGridLayoutManager buffListLayoutManager;
        [SerializeField] private RecyclerViewAdapter buffListAdapter;

        protected override void OnInit()
        {
            _sliderHpFillArea = GetWidget<Slider>("SliderHpFillArea");
            _textHpValue = GetWidget<TextMeshProUGUI>("TextHpValue");
            _sliderMpFillArea = GetWidget<Slider>("SliderMpFillArea");
            _textMpValue = GetWidget<TextMeshProUGUI>("TextMpValue");
            _sliderFirstAtbFillArea = GetWidget<Slider>("SliderFirstAtbFillArea");
            _sliderSecondAtbFillArea = GetWidget<Slider>("SliderSecondAtbFillArea");
            _sliderThirdAtbFillArea = GetWidget<Slider>("SliderThirdAtbFillArea");
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
            if (!Player)
            {
                return;
            }

            // 更新Hp/Mp
            _textHpValue.text = $"{Player.Parameters.resource.hp}/{Player.Parameters.property.maxHp}";
            _textMpValue.text = $"{Player.Parameters.resource.mp}/{Player.Parameters.property.maxMp}";
            DOTween.To(() => _sliderHpFillArea.value, x => _sliderHpFillArea.value = x,
                1f * Player.Parameters.resource.hp / Player.Parameters.property.maxHp, 0.5f);
            DOTween.To(() => _sliderMpFillArea.value, x => _sliderMpFillArea.value = x,
                1f * Player.Parameters.resource.mp / Player.Parameters.property.maxMp, 0.5f);

            // 拆分Atb量为三格量表各自的进度
            var atbTuple = CalculateAtb();
            DOTween.To(() => _sliderFirstAtbFillArea.value, x => _sliderFirstAtbFillArea.value = x,
                atbTuple.firstAtb, 0.2f);
            DOTween.To(() => _sliderSecondAtbFillArea.value, x => _sliderSecondAtbFillArea.value = x,
                atbTuple.secondAtb, 0.2f);
            DOTween.To(() => _sliderThirdAtbFillArea.value, x => _sliderThirdAtbFillArea.value = x,
                atbTuple.thirdAtb, 0.2f);

            // 动态展示Buff栏网格布局高度
            var size = _rvBuffList.GetComponent<RectTransform>().sizeDelta;
            var rowNumber = _rvBuffList.Adapter.GetItemCount() / buffListLayoutManager.GetSpanCount() +
                            (_rvBuffList.Adapter.GetItemCount() % buffListLayoutManager.GetSpanCount() > 0 ? 1 : 0);
            _rvBuffList.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x,
                rowNumber * buffListAdapter.GetViewHolderTemplate(0)!.GetComponent<RectTransform>().sizeDelta.y +
                Mathf.Max(rowNumber - 1, 0) * buffListLayoutManager.GetSpacing());
            UGUIUtil.RefreshLayoutGroupsImmediateAndRecursive(gameObject);
            buffListAdapter.SetData(Player.Parameters.buffs
                    .Where(buff => buff.info.visibility == BuffVisibility.FullVisible)
                    .Select(buff => buff.ToSimpleUIData())
                    .ToList()
            );

            return;

            (float firstAtb, float secondAtb, float thirdAtb) CalculateAtb()
            {
                var firstAtb = 0f;
                var secondAtb = 0f;
                var thirdAtb = 0f;
                switch (Player.Parameters.resource.atb)
                {
                    case <= 1f:
                        firstAtb = Player.Parameters.resource.atb;
                        break;
                    case <= 2f:
                        firstAtb = 1f;
                        secondAtb = Player.Parameters.resource.atb - 1f;
                        break;
                    default:
                        firstAtb = 1f;
                        secondAtb = 1f;
                        thirdAtb = Player.Parameters.resource.atb - 2f;
                        break;
                }

                return (firstAtb, secondAtb, thirdAtb);
            }
        }

        protected override void OnHide()
        {
        }
    }
}
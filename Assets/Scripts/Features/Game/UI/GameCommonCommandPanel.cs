using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using Framework.Common.Debug;
using Framework.Common.Function;
using Framework.Common.UI.Panel;
using Framework.Common.Util;
using Inputs;
using Player;
using Player.StateMachine.Base;
using Sirenix.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using LinqExtensions = Sirenix.Utilities.LinqExtensions;

namespace Features.Game.UI
{
    [RequireComponent(typeof(VerticalLayoutGroup))]
    public class GameCommonCommandPanel : BaseUGUIPanel
    {
        [SerializeField] private TextMeshProUGUI commandTextPrefab;
        private Image _imgBackground;

        [Inject] private GameManager _gameManager;
        [Inject] private InputInfoManager _inputInfoManager;

        private PlayerCharacterObject Player => _gameManager.Player;

        private readonly Dictionary<string, TextMeshProUGUI> _showingCommands = new();
        private ObjectPool<TextMeshProUGUI> _commandPool;

        protected override void OnInit()
        {
            _imgBackground = GetWidget<Image>("ImgBackground");
            _imgBackground.gameObject.SetActive(false);
            _commandPool = new ObjectPool<TextMeshProUGUI>(
                createFunction: () => GameObject.Instantiate(commandTextPrefab, transform),
                destroyFunction: text => GameObject.Destroy(text)
            );
        }

        protected override void OnShow(object payload)
        {
        }

        protected override void OnShowingUpdate(bool focus)
        {
            // 如果玩家不存在就清空所有正在展示的通用指令
            if (!Player || Player.StateMachine is not IPlayerState playerStateMachine || Player.Parameters.dead)
            {
                LinqExtensions.ForEach(_showingCommands.Values,
                    text => _commandPool.Release(text, text => text.gameObject.SetActive(false)));
                _showingCommands.Clear();
                _imgBackground.gameObject.SetActive(false);
                return;
            }

            // 获取新的通用指令列表
            var newestCommands = new List<string>();
            playerStateMachine.GetStateTransitions().ForEach(transition =>
            {
                // 仅添加通用指令
                if (!transition.commonTransition)
                {
                    return;
                }

                var textStringBuilder = new StringBuilder();
                for (var i = 0; i < transition.operatorTips.Count; i++)
                {
                    var operatorTip = transition.operatorTips[i];
                    var prefix = "";
                    var suffix = "";
                    var temp = operatorTip.tips.Where(tip =>
                        (tip.deviceType & _inputInfoManager.InputDeviceType) != 0).ToList();
                    if (temp.Count <= 1)
                    {
                        prefix = i == 0 ? "" : "+";
                    }
                    else
                    {
                        prefix = i == 0 ? "(" : "+(";
                        suffix = ")";
                    }

                    // 添加前缀文字
                    textStringBuilder.Append(prefix);

                    // 添加内部过渡提示
                    var tips = operatorTip.tips.Where(tip =>
                        (tip.deviceType & _inputInfoManager.InputDeviceType) != 0).ToList();
                    for (var j = 0; j < tips.Count; j++)
                    {
                        // 按照设计，前缀文字+图标+后缀文字
                        var tip = tips[j];

                        // 添加前缀文字
                        if (j > 0)
                        {
                            textStringBuilder.Append(operatorTip.operatorType switch
                            {
                                PlayerStateTransitionOperatorType.And => "+" + tip.prefixText,
                                PlayerStateTransitionOperatorType.Or => "或" + tip.prefixText,
                                _ => tip.prefixText
                            });
                        }
                        else
                        {
                            textStringBuilder.Append(tip.prefixText);
                        }

                        // 添加图标
                        if (!String.IsNullOrEmpty(tip.ImageName))
                        {
                            textStringBuilder.Append($"<sprite name={tip.ImageName}>");
                        }

                        // 添加后缀文字
                        textStringBuilder.Append(tip.suffixText);
                    }

                    // 添加后缀文字
                    textStringBuilder.Append(suffix);
                }

                textStringBuilder.Append($"  {transition.name}");
                newestCommands.Add(textStringBuilder.ToString());
            });
            
            // 对比正在展示和新的指令，隐藏不再展示的指令，并展示新的指令
            var toHideCommands = _showingCommands.Keys.Except(newestCommands).ToList();
            var toShowCommands = newestCommands.Except(_showingCommands.Keys).ToList();
            while (toHideCommands.Count > 0)
            {
                if (_showingCommands.TryGetValue(toHideCommands[0], out var text))
                {
                    _commandPool.Release(text, text => text.gameObject.SetActive(false));
                    _showingCommands.Remove(toHideCommands[0]);
                }
                toHideCommands.RemoveAt(0);
            }
            while (toShowCommands.Count > 0)
            {
                var commandText = _commandPool.Get(text =>
                {
                    text.gameObject.SetActive(true);
                });
                commandText.text = toShowCommands[0];
                _showingCommands.Add(toShowCommands[0], commandText);
                toShowCommands.RemoveAt(0);
            }
            
            // 根据新指令顺序排序正在展示指令UI
            newestCommands.ForEach(command =>
            {
                if (_showingCommands.TryGetValue(command, out var text))
                {
                    text.transform.SetAsLastSibling();
                }
            });

            _imgBackground.gameObject.SetActive(_showingCommands.Count > 0);
            UGUIUtil.RefreshLayoutGroupsImmediateAndRecursive(gameObject);
        }

        protected override void OnHide()
        {
            LinqExtensions.ForEach(_showingCommands.Values,
                text => _commandPool.Release(text, text => text.gameObject.SetActive(false)));
            _showingCommands.Clear();
            _commandPool.Clear();
        }
    }
}
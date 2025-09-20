using System;
using System.Collections.Generic;
using System.Linq;
using Character;
using Character.Ability;
using Events;
using Features.Game.UI;
using Framework.Common.Debug;
using Framework.Core.Extension;
using Inputs;
using Interact;
using Rendering;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem.XInput;
using UnityEngine.Serialization;
using VContainer;

namespace Player.Ability
{
    [Serializable]
    public class CharacterInteractTip
    {
        public InputDeviceType deviceType;
        public string inputText;
        public Texture inputImage;
    }

    public class PlayerCharacterInteractAbility : CharacterInteractAbility
    {
        private new PlayerCharacterObject Owner => base.Owner as PlayerCharacterObject;

        [Header("交互提示配置")] [SerializeField] private List<CharacterInteractTip> interactTips;

        [Header("交互GUI配置")] [SerializeField] private Vector2 referencedResolution = new Vector2(1920, 1080);
        [SerializeField, Range(0f, 1f)] private float centerY = 0.7f;
        [SerializeField] private int textSize = 50;
        [SerializeField] private float textVerticalOffset = 5f;
        [SerializeField] private float iconSize = 70f;
        [SerializeField] private float spacing = 30f;
        [SerializeField] private float backgroundMarginX = 10f;
        [SerializeField] private float backgroundMarginY = 5f;

        [Inject] private InputInfoManager _inputInfoManager;
        [Inject] private IGameUIModel _gameUIModel;

        private Vector2 ScaleFactor =>
            new(Screen.width / referencedResolution.x, Screen.height / referencedResolution.y);

        private bool _allowGUIShow = false;

        protected override void OnInit()
        {
            base.OnInit();
            _allowGUIShow = _gameUIModel.AllowGUIShowing().HasValue() && _gameUIModel.AllowGUIShowing().Value;
            GameApplication.Instance.EventCenter.AddEventListener(GameEvents.AllowGUIShow, HandleAllowGUIShow);
            GameApplication.Instance.EventCenter.AddEventListener(GameEvents.BanGUIShow, HandleBanGUIShow);
        }

        public override void Tick(float deltaTime)
        {
            // 记录上一次交互物体
            var lastInteractableObject = CurrentInteractableObject;
            // 执行帧函数
            base.Tick(deltaTime);
            // 如果当前交互物体和上一次交互物体不一致，则删除上一次物体的描边并添加当前物体的描边
            if (CurrentInteractableObject != lastInteractableObject)
            {
                if (lastInteractableObject != null)
                {
                    var interactableOwner = GetInteractableOwner(lastInteractableObject);
                    if (interactableOwner)
                    {
                        RenderingUtil.RemoveRenderingLayerMask(
                            interactableOwner,
                            -1,
                            GlobalRuleSingletonConfigSO.Instance.interactableRenderingLayerMask
                        );
                    }
                }

                // 这里还要额外判断是否允许展示描边，如果是则才允许添加描边
                if (!_gameUIModel.AllowOutlineShowing().HasValue() || !_gameUIModel.AllowOutlineShowing().Value)
                {
                    return;
                }

                if (CurrentInteractableObject != null)
                {
                    var interactableOwner = GetInteractableOwner(CurrentInteractableObject);
                    if (interactableOwner)
                    {
                        RenderingUtil.AddRenderingLayerMask(
                            interactableOwner,
                            -1,
                            GlobalRuleSingletonConfigSO.Instance.interactableRenderingLayerMask
                        );
                    }
                }
            }
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            GameApplication.Instance?.EventCenter.RemoveEventListener(GameEvents.AllowGUIShow, HandleAllowGUIShow);
            GameApplication.Instance?.EventCenter.RemoveEventListener(GameEvents.BanGUIShow, HandleBanGUIShow);
        }

        private void OnGUI()
        {
            if (!_allowGUIShow || !Owner)
            {
                return;
            }

            // 使用GUI展示第一个交互物体的提示文字
            var inputTip = interactTips.Find(tip =>
                (tip.deviceType & _inputInfoManager.InputDeviceType) != 0);
            if (CurrentInteractableObject == null) return;

            var maxWidth = 0f;
            var maxHeight = 0f;
            var centerY = Screen.height * this.centerY;
            var scaleFactor = ScaleFactor;
            var interactTipText = CurrentInteractableObject.Tip(Owner.gameObject);
            var textStyle = new GUIStyle
            {
                fontSize = (int)(textSize * Mathf.Min(scaleFactor.x, scaleFactor.y)),
                normal = new GUIStyleState
                {
                    textColor = new Color(1f, 1f, 1f, 1f),
                },
            };

            // 测量输入文字和图片的尺寸
            var inputTipTextRect = Vector2.zero;
            var inputTipIconSize = 0f;
            if (inputTip != null)
            {
                inputTipTextRect = textStyle.CalcSize(new GUIContent(inputTip.inputText));
                inputTipIconSize = inputTip.inputImage ? iconSize * Mathf.Min(scaleFactor.x, scaleFactor.y) : 0f;
                maxWidth += inputTipTextRect.x + inputTipIconSize + spacing * scaleFactor.x;
                maxHeight = Mathf.Max(inputTipTextRect.y, inputTipIconSize);
            }

            // 测量交互文字的尺寸
            var interactTipTextRect = textStyle.CalcSize(new GUIContent(interactTipText));
            maxWidth += interactTipTextRect.x;
            maxHeight = Mathf.Max(interactTipTextRect.y, maxHeight);

            // 优先绘制背景
            var backgroundWidth = maxWidth + 2 * backgroundMarginX * scaleFactor.x;
            var backgroundHeight = maxHeight + 2 * backgroundMarginY * scaleFactor.y;
            GUI.Box(
                new Rect(
                    (Screen.width - backgroundWidth) / 2f,
                    centerY - backgroundHeight / 2f,
                    backgroundWidth,
                    backgroundHeight
                ),
                ""
            );

            // 绘制输入文字和图片
            var startX = (Screen.width - maxWidth) / 2;
            if (inputTip != null)
            {
                if (!String.IsNullOrEmpty(inputTip.inputText))
                {
                    GUI.Label(
                        new Rect(
                            startX,
                            centerY - inputTipTextRect.y / 2f + textVerticalOffset * scaleFactor.y,
                            inputTipTextRect.x,
                            inputTipTextRect.y
                        ),
                        inputTip.inputText,
                        textStyle
                    );
                    startX += inputTipTextRect.x;
                }

                if (inputTip.inputImage)
                {
                    GUI.DrawTexture(
                        new Rect(startX, centerY - inputTipIconSize / 2f, inputTipIconSize, inputTipIconSize),
                        inputTip.inputImage
                    );
                    startX += inputTipIconSize;
                }

                startX += spacing * scaleFactor.x;
            }

            // 绘制交互文字
            GUI.Label(
                new Rect(
                    startX,
                    centerY - interactTipTextRect.y / 2f + textVerticalOffset * scaleFactor.y,
                    interactTipTextRect.x,
                    interactTipTextRect.y
                ),
                interactTipText,
                textStyle
            );
        }

        private GameObject GetInteractableOwner(IInteractable interactable)
        {
            if (interactable is not MonoBehaviour monoBehaviour || monoBehaviour.IsGameObjectDestroyed())
            {
                return null;
            }

            var reference = monoBehaviour.gameObject.GetComponent<CharacterReference>();
            if (reference)
            {
                return reference.Value.gameObject;
            }

            return monoBehaviour.gameObject;
        }

        private void HandleAllowGUIShow()
        {
            _allowGUIShow = true;
        }

        private void HandleBanGUIShow()
        {
            _allowGUIShow = false;
        }
    }
}
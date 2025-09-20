using System;
using Archive.Data;
using Features.Appearance.Data;
using Features.Appearance.Model;
using Character.Data;
using Character.Data.Extension;
using Features.SceneGoto;
using Framework.Common.Debug;
using Framework.Common.UI.Panel;
using Framework.DataStructure;
using HSVPicker;
using Humanoid;
using Humanoid.Data;
using Humanoid.Model;
using Inputs;
using Map.Data;
using Player.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VContainer;

namespace Features.Appearance.UI
{
    public class AppearanceEditPanel : BaseUGUIPanel
    {
        [Inject] private AppearanceController _appearanceController;
        [Inject] private EventSystem _eventSystem;
        [Inject] private IAppearanceModel _appearanceModel;
        [Inject] private PlayerInputManager _playerInputManager;
        [Inject] private UGUIPanelManager _panelManager;

        [SerializeField] private BaseSceneGotoSO backGotoSO;
        [SerializeField] private ArchiveSceneGotoSO afterCreateGotoSO;

        // 主侧边栏
        private Button _btnBack;
        private Toggle _togMale;
        private Toggle _togFemale;
        private Button _btnHead;
        private Button _btnBody;

        #region 头部窗口

        [FormerlySerializedAs("headWindowAnimationController")] [SerializeField]
        private AppearanceWindowController headWindowController;

        // 隐藏按钮
        private Button _btnHeadPartHide;

        // 随机按钮
        private Button _btnRandomHeadPart;

        // 头部类型
        [SerializeField] private RectTransform headElementArea;
        private TextMeshProUGUI _textHeadElementName;
        private Button _btnHeadElementLeftArrow;
        private Button _btnHeadElementRightArrow;

        // 头发类型
        [SerializeField] private RectTransform hairArea;
        private TextMeshProUGUI _textHairName;
        private Button _btnHairLeftArrow;
        private Button _btnHairRightArrow;

        // 眉毛类型
        [SerializeField] private RectTransform eyebrowsArea;
        private TextMeshProUGUI _textEyebrowsName;
        private Button _btnEyebrowsLeftArrow;
        private Button _btnEyebrowsRightArrow;

        // 胡须类型
        [SerializeField] private RectTransform facialHairArea;
        private TextMeshProUGUI _textFacialHairName;
        private Button _btnFacialHairLeftArrow;
        private Button _btnFacialHairRightArrow;

        // 颜色区域
        private Button _btnHairColor;
        private Image _imgHairColor;
        private Button _btnStubbleColor;
        private Image _imgStubbleColor;
        private Button _btnEyesColor;
        private Image _imgEyesColor;
        private Button _btnScarColor;
        private Image _imgScarColor;
        [SerializeField] private ColorPicker headColorPicker;

        #endregion

        // 身体窗口
        [FormerlySerializedAs("bodyWindowAnimationController")] [SerializeField]
        private AppearanceWindowController bodyWindowController;

        // 随机按钮
        private Button _btnBodyPartHide;
        private Button _btnRandomBodyPart;

        private Button _btnSkinColor;
        private Image _imgSkinColor;
        private Button _btnBodyArtColor;
        private Image _imgBodyArtColor;
        private Button _btnPrimaryColor;
        private Image _imgPrimaryColor;
        private Button _btnSecondaryColor;
        private Image _imgSecondaryColor;
        private Button _btnMetalPrimaryColor;
        private Image _imgMetalPrimaryColor;
        private Button _btnMetalSecondaryColor;
        private Image _imgMetalSecondaryColor;
        private Button _btnMetalDarkColor;
        private Image _imgMetalDarkColor;
        private Button _btnLeatherPrimaryColor;
        private Image _imgLeatherPrimaryColor;
        private Button _btnLeatherSecondaryColor;
        private Image _imgLeatherSecondaryColor;
        [SerializeField] private ColorPicker bodyColorPicker;

        // 输入区域
        private TMP_InputField _ifUsername;
        private Button _btnCreate;
        private bool _characterCreated;

        protected override void OnInit()
        {
            _btnBack = GetWidget<Button>("BtnBack");
            _togMale = GetWidget<Toggle>("TogMale");
            _togFemale = GetWidget<Toggle>("TogFemale");
            _btnHead = GetWidget<Button>("BtnHead");
            _btnBody = GetWidget<Button>("BtnBody");

            _btnHeadPartHide = GetWidget<Button>("BtnHeadPartHide");
            _btnRandomHeadPart = GetWidget<Button>("BtnRandomHeadPart");

            _textHeadElementName = GetWidget<TextMeshProUGUI>("TextHeadElementName");
            _btnHeadElementLeftArrow = GetWidget<Button>("BtnHeadElementLeftArrow");
            _btnHeadElementRightArrow = GetWidget<Button>("BtnHeadElementRightArrow");

            _textHairName = GetWidget<TextMeshProUGUI>("TextHairName");
            _btnHairLeftArrow = GetWidget<Button>("BtnHairLeftArrow");
            _btnHairRightArrow = GetWidget<Button>("BtnHairRightArrow");

            _textEyebrowsName = GetWidget<TextMeshProUGUI>("TextEyebrowsName");
            _btnEyebrowsLeftArrow = GetWidget<Button>("BtnEyebrowsLeftArrow");
            _btnEyebrowsRightArrow = GetWidget<Button>("BtnEyebrowsRightArrow");

            _textFacialHairName = GetWidget<TextMeshProUGUI>("TextFacialHairName");
            _btnFacialHairLeftArrow = GetWidget<Button>("BtnFacialHairLeftArrow");
            _btnFacialHairRightArrow = GetWidget<Button>("BtnFacialHairRightArrow");

            _btnHairColor = GetWidget<Button>("BtnHairColor");
            _imgHairColor = GetWidget<Image>("ImgHairColor");
            _btnStubbleColor = GetWidget<Button>("BtnStubbleColor");
            _imgStubbleColor = GetWidget<Image>("ImgStubbleColor");
            _btnEyesColor = GetWidget<Button>("BtnEyesColor");
            _imgEyesColor = GetWidget<Image>("ImgEyesColor");
            _btnScarColor = GetWidget<Button>("BtnScarColor");
            _imgScarColor = GetWidget<Image>("ImgScarColor");

            _btnBodyPartHide = GetWidget<Button>("BtnBodyPartHide");
            _btnRandomBodyPart = GetWidget<Button>("BtnRandomBodyPart");

            _btnSkinColor = GetWidget<Button>("BtnSkinColor");
            _imgSkinColor = GetWidget<Image>("ImgSkinColor");
            _btnBodyArtColor = GetWidget<Button>("BtnBodyArtColor");
            _imgBodyArtColor = GetWidget<Image>("ImgBodyArtColor");
            _btnPrimaryColor = GetWidget<Button>("BtnPrimaryColor");
            _imgPrimaryColor = GetWidget<Image>("ImgPrimaryColor");
            _btnSecondaryColor = GetWidget<Button>("BtnSecondaryColor");
            _imgSecondaryColor = GetWidget<Image>("ImgSecondaryColor");
            _btnMetalPrimaryColor = GetWidget<Button>("BtnMetalPrimaryColor");
            _imgMetalPrimaryColor = GetWidget<Image>("ImgMetalPrimaryColor");
            _btnMetalSecondaryColor = GetWidget<Button>("BtnMetalSecondaryColor");
            _imgMetalSecondaryColor = GetWidget<Image>("ImgMetalSecondaryColor");
            _btnMetalDarkColor = GetWidget<Button>("BtnMetalDarkColor");
            _imgMetalDarkColor = GetWidget<Image>("ImgMetalDarkColor");
            _btnLeatherPrimaryColor = GetWidget<Button>("BtnLeatherPrimaryColor");
            _imgLeatherPrimaryColor = GetWidget<Image>("ImgLeatherPrimaryColor");
            _btnLeatherSecondaryColor = GetWidget<Button>("BtnLeatherSecondaryColor");
            _imgLeatherSecondaryColor = GetWidget<Image>("ImgLeatherSecondaryColor");

            _ifUsername = GetWidget<TMP_InputField>("IfUsername");
            _btnCreate = GetWidget<Button>("BtnCreate");
        }

        protected override void OnShow(object payload)
        {
            // 监听返回按钮
            _btnBack.onClick.AddListener(OnClickBackButton);

            // 监听种族以及性别开关
            _appearanceModel.GetSelectedRace().ObserveForever(OnRaceChanged);
            _togMale.onValueChanged.AddListener(OnMaleToggleValueChanged);
            _togFemale.onValueChanged.AddListener(OnFemaleToggleValueChanged);

            // 监听侧边栏按钮
            _btnHead.onClick.AddListener(OnClickHeadButton);
            _btnBody.onClick.AddListener(OnClickBodyButton);

            // 监听颜色设置
            _appearanceModel.GetConfigurationColor().ObserveForever(OnColorChanged);

            // 设置头部窗口
            SetHeadWindow();

            // 设置身体窗口
            SetBodyWindow();

            // 监听创建按钮
            _characterCreated = false;
            _ifUsername.onSelect.AddListener(OnSelectInputField);
            _btnCreate.onClick.AddListener(OnClickCreateButton);

            return;

            void SetHeadWindow()
            {
                _btnHeadPartHide.onClick.AddListener(OnClickHeadPartHideButton);
                _btnRandomHeadPart.onClick.AddListener(OnClickRandomHeadPartButton);

                _appearanceModel.GetSelectedHead().ObserveForever(OnHeadElementSelected);
                _btnHeadElementLeftArrow.onClick.AddListener(OnClickHeadElementLeftArrow);
                _btnHeadElementRightArrow.onClick.AddListener(OnClickHeadElementRightArrow);

                _appearanceModel.GetSelectedHair().ObserveForever(OnHairSelected);
                _btnHairLeftArrow.onClick.AddListener(OnClickHairLeftArrow);
                _btnHairRightArrow.onClick.AddListener(OnClickHairRightArrow);

                _appearanceModel.GetSelectedEyebrow().ObserveForever(OnEyebrowsSelected);
                _btnEyebrowsLeftArrow.onClick.AddListener(OnClickEyebrowsLeftArrow);
                _btnEyebrowsRightArrow.onClick.AddListener(OnClickEyebrowsRightArrow);

                _appearanceModel.GetSelectedFacialHair().ObserveForever(OnFacialHairSelected);
                _btnFacialHairLeftArrow.onClick.AddListener(OnClickFacialHairLeftArrow);
                _btnFacialHairRightArrow.onClick.AddListener(OnClickFacialHairRightArrow);

                _btnHairColor.onClick.AddListener(OnClickHairColor);
                _btnStubbleColor.onClick.AddListener(OnClickStubbleColor);
                _btnEyesColor.onClick.AddListener(OnClickEyesColor);
                _btnScarColor.onClick.AddListener(OnClickScarColor);
            }

            void SetBodyWindow()
            {
                _btnBodyPartHide.onClick.AddListener(OnClickBodyPartHideButton);
                _btnRandomBodyPart.onClick.AddListener(OnClickRandomBodyPartButton);

                _btnSkinColor.onClick.AddListener(OnClickSkinColor);
                _btnBodyArtColor.onClick.AddListener(OnClickBodyArtColor);
                _btnPrimaryColor.onClick.AddListener(OnClickPrimaryColor);
                _btnSecondaryColor.onClick.AddListener(OnClickSecondaryColor);
                _btnMetalPrimaryColor.onClick.AddListener(OnClickMetalPrimaryColor);
                _btnMetalSecondaryColor.onClick.AddListener(OnClickMetalSecondaryColor);
                _btnMetalDarkColor.onClick.AddListener(OnClickMetalDarkColor);
                _btnLeatherPrimaryColor.onClick.AddListener(OnClickLeatherPrimaryColor);
                _btnLeatherSecondaryColor.onClick.AddListener(OnClickLeatherSecondaryColor);
            }
        }

        protected override void OnShowingUpdate(bool focus)
        {
            if (focus && !_eventSystem.currentSelectedGameObject &&
                _playerInputManager.WasPerformedThisFrame(InputConstants.Navigate))
            {
                _eventSystem.SetSelectedGameObject(_btnBack.gameObject);
            }

            if (focus && _playerInputManager.WasPerformedThisFrame(InputConstants.Cancel))
            {
                _eventSystem.SetSelectedGameObject(_btnBack.gameObject);
            }

            // 切换摄像机类型
            _appearanceController.CameraType = headWindowController.Showing ? CameraType.Head : CameraType.Global;
        }

        protected override void OnHide()
        {
            // 解除监听返回按钮
            _btnBack.onClick.RemoveListener(OnClickBackButton);

            // 解除监听种族以及性别开关
            _appearanceModel.GetSelectedRace().RemoveObserver(OnRaceChanged);
            _togMale.onValueChanged.RemoveListener(OnMaleToggleValueChanged);
            _togFemale.onValueChanged.RemoveListener(OnFemaleToggleValueChanged);

            // 解除监听侧边栏按钮
            _btnHead.onClick.RemoveListener(OnClickHeadButton);
            _btnBody.onClick.RemoveListener(OnClickBodyButton);

            // 解除监听颜色设置
            _appearanceModel.GetConfigurationColor().RemoveObserver(OnColorChanged);

            // 重置头部窗口
            ResetHeadWindow();

            // 重置身体窗口
            ResetBodyWindow();

            // 重置创建按钮
            _characterCreated = false;
            _ifUsername.onSelect.RemoveListener(OnSelectInputField);
            _btnCreate.onClick.RemoveListener(OnClickCreateButton);

            return;

            void ResetHeadWindow()
            {
                _btnHeadPartHide.onClick.RemoveListener(OnClickHeadPartHideButton);
                _btnRandomHeadPart.onClick.RemoveListener(OnClickRandomHeadPartButton);

                _appearanceModel.GetSelectedHead().RemoveObserver(OnHeadElementSelected);
                _btnHeadElementLeftArrow.onClick.RemoveListener(OnClickHeadElementLeftArrow);
                _btnHeadElementRightArrow.onClick.RemoveListener(OnClickHeadElementRightArrow);

                _appearanceModel.GetSelectedHair().RemoveObserver(OnHairSelected);
                _btnHairLeftArrow.onClick.RemoveListener(OnClickHairLeftArrow);
                _btnHairRightArrow.onClick.RemoveListener(OnClickHairRightArrow);

                _appearanceModel.GetSelectedEyebrow().RemoveObserver(OnEyebrowsSelected);
                _btnEyebrowsLeftArrow.onClick.RemoveListener(OnClickEyebrowsLeftArrow);
                _btnEyebrowsRightArrow.onClick.RemoveListener(OnClickEyebrowsRightArrow);

                _appearanceModel.GetSelectedFacialHair().RemoveObserver(OnFacialHairSelected);
                _btnFacialHairLeftArrow.onClick.RemoveListener(OnClickFacialHairLeftArrow);
                _btnFacialHairRightArrow.onClick.RemoveListener(OnClickFacialHairRightArrow);

                _btnHairColor.onClick.RemoveListener(OnClickHairColor);
                _btnStubbleColor.onClick.RemoveListener(OnClickStubbleColor);
                _btnEyesColor.onClick.RemoveListener(OnClickEyesColor);
                _btnScarColor.onClick.RemoveListener(OnClickScarColor);
            }

            void ResetBodyWindow()
            {
                _btnBodyPartHide.onClick.RemoveListener(OnClickBodyPartHideButton);
                _btnRandomBodyPart.onClick.RemoveListener(OnClickRandomBodyPartButton);

                _btnSkinColor.onClick.RemoveListener(OnClickSkinColor);
                _btnBodyArtColor.onClick.RemoveListener(OnClickBodyArtColor);
                _btnPrimaryColor.onClick.RemoveListener(OnClickPrimaryColor);
                _btnSecondaryColor.onClick.RemoveListener(OnClickSecondaryColor);
                _btnMetalPrimaryColor.onClick.RemoveListener(OnClickMetalPrimaryColor);
                _btnMetalSecondaryColor.onClick.RemoveListener(OnClickMetalSecondaryColor);
                _btnMetalDarkColor.onClick.RemoveListener(OnClickMetalDarkColor);
                _btnLeatherPrimaryColor.onClick.RemoveListener(OnClickLeatherPrimaryColor);
                _btnLeatherSecondaryColor.onClick.RemoveListener(OnClickLeatherSecondaryColor);
            }
        }

        private void OnClickBackButton()
        {
            backGotoSO?.Goto(null);
        }

        private void OnRaceChanged(HumanoidCharacterRace race)
        {
            switch (race)
            {
                case HumanoidCharacterRace.HumanMale:
                {
                    _togMale.isOn = true;
                    _togFemale.isOn = false;
                }
                    break;
                case HumanoidCharacterRace.HumanFemale:
                {
                    _togMale.isOn = false;
                    _togFemale.isOn = true;
                }
                    break;
            }
        }

        private void OnMaleToggleValueChanged(bool value)
        {
            if (value)
            {
                _appearanceModel.SelectRace(HumanoidCharacterRace.HumanMale);
            }
        }

        private void OnFemaleToggleValueChanged(bool value)
        {
            if (value)
            {
                _appearanceModel.SelectRace(HumanoidCharacterRace.HumanFemale);
            }
        }

        private void OnClickHeadButton()
        {
            headWindowController.Show();
            bodyWindowController.Hide();
            bodyColorPicker.Close();
            _eventSystem.SetSelectedGameObject(_btnHeadPartHide.gameObject);
        }

        private void OnClickBodyButton()
        {
            headWindowController.Hide();
            bodyWindowController.Show();
            headColorPicker.Close();
            _eventSystem.SetSelectedGameObject(_btnBodyPartHide.gameObject);
        }

        private void OnColorChanged(HumanoidAppearanceColor color)
        {
            _imgHairColor.GetComponent<Image>().color = color.HairColor;
            _imgStubbleColor.GetComponent<Image>().color = color.StubbleColor;
            _imgEyesColor.GetComponent<Image>().color = color.EyesColor;
            _imgScarColor.GetComponent<Image>().color = color.ScarColor;
            _imgSkinColor.GetComponent<Image>().color = color.SkinColor;
            _imgBodyArtColor.GetComponent<Image>().color = color.BodyArtColor;
            _imgPrimaryColor.GetComponent<Image>().color = color.PrimaryColor;
            _imgSecondaryColor.GetComponent<Image>().color = color.SecondaryColor;
            _imgMetalPrimaryColor.GetComponent<Image>().color = color.MetalPrimaryColor;
            _imgMetalSecondaryColor.GetComponent<Image>().color = color.MetalSecondaryColor;
            _imgMetalDarkColor.GetComponent<Image>().color = color.MetalDarkColor;
            _imgLeatherPrimaryColor.GetComponent<Image>().color = color.LeatherPrimaryColor;
            _imgLeatherSecondaryColor.GetComponent<Image>().color = color.LeatherSecondaryColor;
        }

        private void OnClickHeadPartHideButton()
        {
            headWindowController.Hide();
            _eventSystem.SetSelectedGameObject(_btnHead.gameObject);
        }

        private void OnClickRandomHeadPartButton()
        {
            _appearanceModel.RandomHeadPart();
        }

        private void OnClickBodyPartHideButton()
        {
            bodyWindowController.Hide();
            _eventSystem.SetSelectedGameObject(_btnBody.gameObject);
        }

        private void OnClickRandomBodyPartButton()
        {
            _appearanceModel.RandomBodyPart();
        }

        private void OnClickHeadElementLeftArrow()
        {
            _appearanceModel.SelectPreviousHead();
        }

        private void OnClickHeadElementRightArrow()
        {
            _appearanceModel.SelectNextHead();
        }

        private void OnClickHairLeftArrow()
        {
            _appearanceModel.SelectPreviousHair();
        }

        private void OnClickHairRightArrow()
        {
            _appearanceModel.SelectNextHair();
        }

        private void OnClickEyebrowsLeftArrow()
        {
            _appearanceModel.SelectPreviousEyebrow();
        }

        private void OnClickEyebrowsRightArrow()
        {
            _appearanceModel.SelectNextEyebrow();
        }

        private void OnClickFacialHairLeftArrow()
        {
            _appearanceModel.SelectPreviousFacialHair();
        }

        private void OnClickFacialHairRightArrow()
        {
            _appearanceModel.SelectNextFacialHair();
        }

        private void OnClickHairColor()
        {
            headColorPicker.Show(
                _btnHairColor.gameObject,
                _imgHairColor.transform.position,
                (color) =>
                {
                    _imgHairColor.color = color;
                    _appearanceModel.SetConfigurationColor(_appearanceModel.GetConfigurationColor().Value
                        .Clone(hairColor: color));
                }, null
            );
        }

        private void OnClickStubbleColor()
        {
            headColorPicker.Show(
                _btnStubbleColor.gameObject,
                _imgStubbleColor.transform.position,
                (color) =>
                {
                    _imgStubbleColor.color = color;
                    _appearanceModel.SetConfigurationColor(_appearanceModel.GetConfigurationColor().Value
                        .Clone(stubbleColor: color));
                }, null
            );
        }

        private void OnClickEyesColor()
        {
            headColorPicker.Show(
                _btnEyesColor.gameObject,
                _imgEyesColor.transform.position,
                (color) =>
                {
                    _imgEyesColor.color = color;
                    _appearanceModel.SetConfigurationColor(_appearanceModel.GetConfigurationColor().Value
                        .Clone(eyesColor: color));
                }, null
            );
        }

        private void OnClickScarColor()
        {
            headColorPicker.Show(
                _btnScarColor.gameObject,
                _imgScarColor.transform.position,
                (color) =>
                {
                    _imgScarColor.color = color;
                    _appearanceModel.SetConfigurationColor(_appearanceModel.GetConfigurationColor().Value
                        .Clone(scarColor: color));
                }, null
            );
        }

        private void OnClickSkinColor()
        {
            var rectTransform = bodyColorPicker.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            bodyColorPicker.Show(
                _btnSkinColor.gameObject,
                _imgSkinColor.transform.position,
                (color) =>
                {
                    _imgSkinColor.color = color;
                    _appearanceModel.SetConfigurationColor(_appearanceModel.GetConfigurationColor().Value
                        .Clone(skinColor: color));
                }, null
            );
        }

        private void OnClickBodyArtColor()
        {
            var rectTransform = bodyColorPicker.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            bodyColorPicker.Show(
                _btnBodyArtColor.gameObject,
                _imgBodyArtColor.transform.position,
                (color) =>
                {
                    _imgBodyArtColor.color = color;
                    _appearanceModel.SetConfigurationColor(_appearanceModel.GetConfigurationColor().Value
                        .Clone(bodyArtColor: color));
                }, null
            );
        }

        private void OnClickPrimaryColor()
        {
            var rectTransform = bodyColorPicker.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            bodyColorPicker.Show(
                _btnPrimaryColor.gameObject,
                _imgPrimaryColor.transform.position,
                (color) =>
                {
                    _imgPrimaryColor.color = color;
                    _appearanceModel.SetConfigurationColor(_appearanceModel.GetConfigurationColor().Value
                        .Clone(primaryColor: color));
                }, null
            );
        }

        private void OnClickSecondaryColor()
        {
            var rectTransform = bodyColorPicker.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            bodyColorPicker.Show(
                _btnSecondaryColor.gameObject,
                _imgSecondaryColor.transform.position,
                (color) =>
                {
                    _imgSecondaryColor.color = color;
                    _appearanceModel.SetConfigurationColor(_appearanceModel.GetConfigurationColor().Value
                        .Clone(secondaryColor: color));
                }, null
            );
        }

        private void OnClickMetalPrimaryColor()
        {
            var rectTransform = bodyColorPicker.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(1f, 0f);
            bodyColorPicker.Show(
                _btnMetalPrimaryColor.gameObject,
                _imgMetalPrimaryColor.transform.position,
                (color) =>
                {
                    _imgMetalPrimaryColor.color = color;
                    _appearanceModel.SetConfigurationColor(_appearanceModel.GetConfigurationColor().Value
                        .Clone(metalPrimaryColor: color));
                }, null
            );
        }

        private void OnClickMetalSecondaryColor()
        {
            var rectTransform = bodyColorPicker.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(1f, 0f);
            bodyColorPicker.Show(
                _btnMetalSecondaryColor.gameObject,
                _imgMetalSecondaryColor.transform.position,
                (color) =>
                {
                    _imgMetalSecondaryColor.color = color;
                    _appearanceModel.SetConfigurationColor(_appearanceModel.GetConfigurationColor().Value
                        .Clone(metalSecondaryColor: color));
                }, null
            );
        }

        private void OnClickMetalDarkColor()
        {
            var rectTransform = bodyColorPicker.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(1f, 0f);
            bodyColorPicker.Show(
                _btnMetalDarkColor.gameObject,
                _imgMetalDarkColor.transform.position,
                (color) =>
                {
                    _imgMetalDarkColor.color = color;
                    _appearanceModel.SetConfigurationColor(_appearanceModel.GetConfigurationColor().Value
                        .Clone(metalDarkColor: color));
                }, null
            );
        }

        private void OnClickLeatherPrimaryColor()
        {
            var rectTransform = bodyColorPicker.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(1f, 0f);
            bodyColorPicker.Show(
                _btnLeatherPrimaryColor.gameObject,
                _imgLeatherPrimaryColor.transform.position,
                (color) =>
                {
                    _imgLeatherPrimaryColor.color = color;
                    _appearanceModel.SetConfigurationColor(_appearanceModel.GetConfigurationColor().Value
                        .Clone(leatherPrimaryColor: color));
                }, null
            );
        }

        private void OnClickLeatherSecondaryColor()
        {
            var rectTransform = bodyColorPicker.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(1f, 0f);
            bodyColorPicker.Show(
                _btnLeatherSecondaryColor.gameObject,
                _imgLeatherSecondaryColor.transform.position,
                (color) =>
                {
                    _imgLeatherSecondaryColor.color = color;
                    _appearanceModel.SetConfigurationColor(_appearanceModel.GetConfigurationColor().Value
                        .Clone(leatherSecondaryColor: color));
                }, null
            );
        }

        private void OnHeadElementSelected(AppearanceDefaultModelData? modelData)
        {
            if (modelData.HasValue)
            {
                headElementArea.gameObject.SetActive(true);
                _textHeadElementName.text = modelData.Value.Alias;
            }
            else
            {
                headElementArea.gameObject.SetActive(false);
            }
        }

        private void OnHairSelected(AppearanceDefaultModelData? modelData)
        {
            if (modelData.HasValue)
            {
                hairArea.gameObject.SetActive(true);
                _textHairName.text = modelData.Value.Alias;
            }
            else
            {
                hairArea.gameObject.SetActive(false);
            }
        }

        private void OnEyebrowsSelected(AppearanceDefaultModelData? modelData)
        {
            if (modelData.HasValue)
            {
                eyebrowsArea.gameObject.SetActive(true);
                _textEyebrowsName.text = modelData.Value.Alias;
            }
            else
            {
                eyebrowsArea.gameObject.SetActive(false);
            }
        }

        private void OnFacialHairSelected(AppearanceDefaultModelData? modelData)
        {
            if (modelData.HasValue)
            {
                facialHairArea.gameObject.SetActive(true);
                _textFacialHairName.text = modelData.Value.Alias;
            }
            else
            {
                facialHairArea.gameObject.SetActive(false);
            }
        }

        private void OnSelectInputField(string value)
        {
            headWindowController.Hide();
            bodyWindowController.Hide();
        }

        private void OnClickCreateButton()
        {
            if (_characterCreated)
            {
                return;
            }

            _characterCreated = true;
            var newArchiveId = GameApplication.Instance.ArchiveManager.SaveNewArchive(new ArchiveData
            {
                auto = true,
                player = new PlayerArchiveData
                {
                    name = _ifUsername.text,
                    race = _appearanceModel.GetSelectedRace().Value,
                    appearance = _appearanceModel.GetAppearance().Value.ToArchiveData(),
                    level = 1,
                    money = 0,
                    experience = 0,
                    map = GetMapArchive(1),
                    hp = int.MaxValue,
                    mp = int.MaxValue,
                },
            });
            afterCreateGotoSO?.Goto(new ArchiveSceneGotoParameters()
            {
                Id = newArchiveId,
            });
            return;

            CharacterMapArchiveData GetMapArchive(int mapId)
            {
                var mapInfoContainer = GameApplication.Instance.ExcelBinaryManager.GetContainer<MapInfoContainer>();
                return new CharacterMapArchiveData
                {
                    id = mapId,
                    position = new SerializableVector3(mapInfoContainer.Data[mapId].DefaultPositionX,
                        mapInfoContainer.Data[mapId].DefaultPositionY, mapInfoContainer.Data[mapId].DefaultPositionZ),
                    forwardAngle = mapInfoContainer.Data[mapId].DefaultForwardAngle
                };
            }
        }
    }
}
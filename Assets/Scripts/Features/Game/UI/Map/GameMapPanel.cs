using System.Collections.Generic;
using Character;
using Common;
using Features.Game.Data;
using Features.Game.UI.Map.Information;
using Framework.Common.Debug;
using Framework.Common.Function;
using Framework.Common.UI.Panel;
using Inputs;
using Map;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;
using PlayerInputManager = Inputs.PlayerInputManager;

namespace Features.Game.UI.Map
{
    public class GameMapPanel : BaseUGUIPanel
    {
        [Inject] private MapManager _mapManager;
        [Inject] private GameManager _gameManager;
        [Inject] private PlayerInputManager _playerInputManager;
        [Inject] private GameUIModel _gameUIModel;

        [Title("地图信息预设体")] [SerializeField] private GameMapInformation briefLocationInformation;
        [SerializeField] private GameMapInformation briefPinInformation;
        [SerializeField] private GameMapAllyInformation briefAllyInformation;
        [SerializeField] private GameMapInformation briefMonsterInformation;
        [SerializeField] private GameMapInformation briefEliteInformation;
        [SerializeField] private GameMapInformation briefBossInformation;
        [SerializeField] private GameMapInformation briefRestSpotInformation;

        [SerializeField] private GameMapInformation detailLocationInformation;
        [SerializeField] private GameMapInformation detailPinInformation;
        [SerializeField] private GameMapAllyInformation detailAllyInformation;
        [SerializeField] private GameMapEnemyInformation detailMonsterInformation;
        [SerializeField] private GameMapEnemyInformation detailEliteInformation;
        [SerializeField] private GameMapEnemyInformation detailBossInformation;
        [SerializeField] private GameMapInformation detailRestSpotInformation;

        [SerializeField] private GameMapInformation indicatorInformation;

        [Title("地图配置")] [SerializeField, MinValue(1f)]
        private float maxScale = 3f;

        [SerializeField, MinValue(1f)] private float detailScaleThreshold = 2f;
        [SerializeField] private float moveSpeed = 3f;

        private RectTransform _mapRestriction;
        private Image _mapWindow;
        private Image _imgMapMask;
        private Image _imgMap;
        private Rect _displayRestriction;
        private GameMapModel _mapModel;

        private ObjectPool<GameMapInformation> _briefLocationInformationPool;
        private ObjectPool<GameMapInformation> _briefPinInformationPool;
        private ObjectPool<GameMapAllyInformation> _briefAllyInformationPool;
        private ObjectPool<GameMapInformation> _briefMonsterInformationPool;
        private ObjectPool<GameMapInformation> _briefEliteInformationPool;
        private ObjectPool<GameMapInformation> _briefBossInformationPool;
        private ObjectPool<GameMapInformation> _briefRestSpotInformationPool;


        private ObjectPool<GameMapInformation> _detailLocationInformationPool;
        private ObjectPool<GameMapInformation> _detailPinInformationPool;
        private ObjectPool<GameMapAllyInformation> _detailAllyInformationPool;
        private ObjectPool<GameMapEnemyInformation> _detailMonsterInformationPool;
        private ObjectPool<GameMapEnemyInformation> _detailEliteInformationPool;
        private ObjectPool<GameMapEnemyInformation> _detailBossInformationPool;
        private ObjectPool<GameMapInformation> _detailRestSpotInformationPool;

        private ObjectPool<GameMapInformation> _indicatorInformationPool;

        private readonly List<GameMapInformation> _showingBriefLocationInformation = new();
        private readonly List<GameMapInformation> _showingBriefPinInformation = new();
        private readonly List<GameMapAllyInformation> _showingBriefAllyInformation = new();
        private readonly List<GameMapInformation> _showingBriefMonsterInformation = new();
        private readonly List<GameMapInformation> _showingBriefEliteInformation = new();
        private readonly List<GameMapInformation> _showingBriefBossInformation = new();
        private readonly List<GameMapInformation> _showingBriefRestSpotInformation = new();

        private readonly List<GameMapInformation> _showingDetailLocationInformation = new();
        private readonly List<GameMapInformation> _showingDetailPinInformation = new();
        private readonly List<GameMapAllyInformation> _showingDetailAllyInformation = new();
        private readonly List<GameMapEnemyInformation> _showingDetailMonsterInformation = new();
        private readonly List<GameMapEnemyInformation> _showingDetailEliteInformation = new();
        private readonly List<GameMapEnemyInformation> _showingDetailBossInformation = new();
        private readonly List<GameMapInformation> _showingDetailRestSpotInformation = new();

        private readonly List<GameMapInformation> _showingIndicatorInformation = new();

        protected override void OnInit()
        {
            // 初始化控件
            _mapRestriction = GetWidget<RectTransform>("MapRestriction");
            _mapWindow = GetWidget<Image>("MapWindow");
            _imgMapMask = GetWidget<Image>("ImgMapMask");
            _imgMap = GetWidget<Image>("ImgMap");
            _displayRestriction = _mapRestriction.rect;

            // 初始化对象池
            _briefLocationInformationPool = new ObjectPool<GameMapInformation>(
                createFunction: () => GameObject.Instantiate(briefLocationInformation, _imgMapMask.transform),
                destroyFunction: (information) => { GameObject.Destroy(information.gameObject); }
            );
            _briefPinInformationPool = new ObjectPool<GameMapInformation>(
                createFunction: () => GameObject.Instantiate(briefPinInformation, _imgMapMask.transform),
                destroyFunction: (information) => { GameObject.Destroy(information.gameObject); }
            );
            _briefAllyInformationPool = new ObjectPool<GameMapAllyInformation>(
                createFunction: () => GameObject.Instantiate(briefAllyInformation, _imgMapMask.transform),
                destroyFunction: (information) => { GameObject.Destroy(information.gameObject); }
            );
            _briefMonsterInformationPool = new ObjectPool<GameMapInformation>(
                createFunction: () => GameObject.Instantiate(briefMonsterInformation, _imgMapMask.transform),
                destroyFunction: (information) => { GameObject.Destroy(information.gameObject); }
            );
            _briefEliteInformationPool = new ObjectPool<GameMapInformation>(
                createFunction: () => GameObject.Instantiate(briefEliteInformation, _imgMapMask.transform),
                destroyFunction: (information) => { GameObject.Destroy(information.gameObject); }
            );
            _briefBossInformationPool = new ObjectPool<GameMapInformation>(
                createFunction: () => GameObject.Instantiate(briefBossInformation, _imgMapMask.transform),
                destroyFunction: (information) => { GameObject.Destroy(information.gameObject); }
            );
            _briefRestSpotInformationPool = new ObjectPool<GameMapInformation>(
                createFunction: () => GameObject.Instantiate(briefRestSpotInformation, _imgMapMask.transform),
                destroyFunction: (information) => { GameObject.Destroy(information.gameObject); }
            );
            _detailLocationInformationPool = new ObjectPool<GameMapInformation>(
                createFunction: () => GameObject.Instantiate(detailLocationInformation, _imgMapMask.transform),
                destroyFunction: (information) => { GameObject.Destroy(information.gameObject); }
            );
            _detailPinInformationPool = new ObjectPool<GameMapInformation>(
                createFunction: () => GameObject.Instantiate(detailPinInformation, _imgMapMask.transform),
                destroyFunction: (information) => { GameObject.Destroy(information.gameObject); }
            );
            _detailAllyInformationPool = new ObjectPool<GameMapAllyInformation>(
                createFunction: () => GameObject.Instantiate(detailAllyInformation, _imgMapMask.transform),
                destroyFunction: (information) => { GameObject.Destroy(information.gameObject); }
            );
            _detailMonsterInformationPool = new ObjectPool<GameMapEnemyInformation>(
                createFunction: () => GameObject.Instantiate(detailMonsterInformation, _imgMapMask.transform),
                destroyFunction: (information) => { GameObject.Destroy(information.gameObject); }
            );
            _detailEliteInformationPool = new ObjectPool<GameMapEnemyInformation>(
                createFunction: () => GameObject.Instantiate(detailEliteInformation, _imgMapMask.transform),
                destroyFunction: (information) => { GameObject.Destroy(information.gameObject); }
            );
            _detailBossInformationPool = new ObjectPool<GameMapEnemyInformation>(
                createFunction: () => GameObject.Instantiate(detailBossInformation, _imgMapMask.transform),
                destroyFunction: (information) => { GameObject.Destroy(information.gameObject); }
            );
            _detailRestSpotInformationPool = new ObjectPool<GameMapInformation>(
                createFunction: () => GameObject.Instantiate(detailRestSpotInformation, _imgMapMask.transform),
                destroyFunction: (information) => { GameObject.Destroy(information.gameObject); }
            );
            _indicatorInformationPool = new ObjectPool<GameMapInformation>(
                createFunction: () => GameObject.Instantiate(indicatorInformation, _imgMapMask.transform),
                destroyFunction: (information) => { GameObject.Destroy(information.gameObject); }
            );
        }

        protected override void OnShow(object payload)
        {
            _mapModel = new GameMapModel(_displayRestriction, _mapManager, _gameManager, maxScale);

            // 隐藏地图
            _mapWindow.gameObject.SetActive(false);

            // 监听玩家输入
            _playerInputManager.RegisterActionPerformed(InputConstants.Scale, HandleScalePerformed);
            _playerInputManager.RegisterActionPerformed(InputConstants.Submit, HandleSubmitPerformed);
            _playerInputManager.RegisterActionPerformed(InputConstants.Map, HandleMapPerformed);
            if (_imgMapMask.TryGetComponent<DragBehaviour>(out var dragBehaviour))
            {
                dragBehaviour.onDragging.AddListener(HandleDragPerformed);
            }

            // 监听地图数据
            _mapModel.GetCurrentMap().ObserveForever(HandleMapUIDataChanged);
            _mapModel.GetIndicatorPosition().ObserveForever(HandleIndicatorPositionChanged);

            // 初始化地图
            _mapModel.FetchCurrentMap();
        }

        protected override void OnShowingUpdate(bool focus)
        {
            var inputAction = _playerInputManager.GetInputAction(InputConstants.Navigate);
            if (focus && inputAction.IsPressed())
            {
                _mapModel.MoveIndicator(inputAction.ReadValue<Vector2>() * moveSpeed);
            }
        }

        protected override void OnHide()
        {
            // 取消监听玩家输入
            _playerInputManager.UnregisterActionPerformed(InputConstants.Scale, HandleScalePerformed);
            _playerInputManager.UnregisterActionPerformed(InputConstants.Submit, HandleSubmitPerformed);
            _playerInputManager.UnregisterActionPerformed(InputConstants.Map, HandleMapPerformed);
            if (_imgMapMask.TryGetComponent<DragBehaviour>(out var dragBehaviour))
            {
                dragBehaviour.onDragging.RemoveListener(HandleDragPerformed);
            }

            // 取消监听地图数据
            _mapModel.GetCurrentMap().RemoveObserver(HandleMapUIDataChanged);
            _mapModel.GetIndicatorPosition().RemoveObserver(HandleIndicatorPositionChanged);

            // 销毁展示的地图信息
            DestroyShowingCharacterInformation();
            DestroyShowingInteractableInformation();
            DestroyShowingPinInformation();
            DestroyShowingIndicatorInformation();

            // 清空对象池
            _briefLocationInformationPool.Clear();
            _briefPinInformationPool.Clear();
            _briefAllyInformationPool.Clear();
            _briefMonsterInformationPool.Clear();
            _briefEliteInformationPool.Clear();
            _briefBossInformationPool.Clear();
            _briefRestSpotInformationPool.Clear();
            _detailLocationInformationPool.Clear();
            _detailPinInformationPool.Clear();
            _detailAllyInformationPool.Clear();
            _detailMonsterInformationPool.Clear();
            _detailEliteInformationPool.Clear();
            _detailBossInformationPool.Clear();
            _detailRestSpotInformationPool.Clear();
            _indicatorInformationPool.Clear();

            // 释放地图
            _mapWindow.gameObject.SetActive(false);
            _mapModel.ReleaseCurrentMap();
            _mapModel.Destroy();
            _mapModel = null;
        }

        private void HandleScalePerformed(InputAction.CallbackContext obj)
        {
            if (!Focus)
            {
                return;
            }

            _mapModel.ScaleMap(obj.ReadValue<float>());
        }

        private void HandleSubmitPerformed(InputAction.CallbackContext obj)
        {
            if (!Focus || !_mapModel.GetCurrentMap().HasValue() || _mapModel.GetCurrentMap().Value == null)
            {
                return;
            }

            // 将地图指示器的屏幕位置转为游戏实际位置
            var mapData = _mapModel.GetCurrentMap().Value;
            var indicatorPosition = _mapModel.GetIndicatorPosition().HasValue()
                ? _mapModel.GetIndicatorPosition().Value
                : Vector2.zero;
            var pinPosition = mapData.DisplayOffsetToGamePosition(indicatorPosition) + mapData.Map.Snapshot.Center3D;

            // 销毁展示的地图针信息
            DestroyShowingPinInformation();

            // 设置地图针的位置并添加地图信息
            _mapManager.SetPinPosition(pinPosition);
            if (mapData.Scale < detailScaleThreshold)
            {
                _showingBriefPinInformation.Add(_briefPinInformationPool.Get((information) =>
                {
                    information.gameObject.SetActive(true);
                    information.SetPosition(
                        mapData.GamePositionToDisplayPosition(
                            _imgMapMask.transform.position,
                            pinPosition,
                            _imgMapMask.transform.lossyScale
                        )
                    );
                }));
            }
            else
            {
                _showingDetailPinInformation.Add(_detailPinInformationPool.Get((information) =>
                {
                    information.gameObject.SetActive(true);
                    information.SetPosition(
                        mapData.GamePositionToDisplayPosition(
                            _imgMapMask.transform.position,
                            pinPosition,
                            _imgMapMask.transform.lossyScale
                        )
                    );
                }));
            }
        }

        private void HandleMapPerformed(InputAction.CallbackContext obj)
        {
            if (!Focus)
            {
                return;
            }

            _gameUIModel.MapUI.SetValue(_gameUIModel.MapUI.Value.Close());
        }

        private void HandleDragPerformed(Vector2 delta)
        {
            _mapModel.MoveMap(-delta);
        }

        private void HandleMapUIDataChanged(GameMapUIData data)
        {
            if (!data.Map || !data.Snapshot)
            {
                _mapWindow.gameObject.SetActive(false);
                return;
            }

            _mapWindow.gameObject.SetActive(true);

            // 设置地图图片，保持其宽高比
            _imgMap.sprite = data.Snapshot;
            _imgMapMask.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, data.WindowSize.x);
            _imgMapMask.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, data.WindowSize.y);

            // 设置地图缩放和偏差
            _imgMap.rectTransform.anchoredPosition = Vector2.zero;
            _imgMap.rectTransform.localScale = new Vector3(data.Scale, data.Scale, 1);
            _imgMap.rectTransform.anchoredPosition = -data.Offset;

            // 销毁展示中的地图信息
            DestroyShowingCharacterInformation();
            DestroyShowingInteractableInformation();
            DestroyShowingPinInformation();

            // 设置目标定位信息
            if (_mapManager.GetPinPosition(out var pinPosition))
            {
                if (data.Scale < detailScaleThreshold)
                {
                    _showingBriefPinInformation.Add(_briefPinInformationPool.Get((information) =>
                    {
                        information.gameObject.SetActive(true);
                        information.SetPosition(
                            data.GamePositionToDisplayPosition(
                                _imgMapMask.transform.position,
                                pinPosition,
                                _imgMapMask.transform.lossyScale
                            )
                        );
                    }));
                }
                else
                {
                    _showingDetailPinInformation.Add(_detailPinInformationPool.Get((information) =>
                    {
                        information.gameObject.SetActive(true);
                        information.SetPosition(
                            data.GamePositionToDisplayPosition(
                                _imgMapMask.transform.position,
                                pinPosition,
                                _imgMapMask.transform.lossyScale
                            )
                        );
                    }));
                }
            }

            // 设置角色信息
            _gameManager.Characters.ForEach(character =>
            {
                switch (character.Parameters.side)
                {
                    case CharacterSide.Player when character == _gameManager.Player:
                    {
                        // 设置玩家信息
                        if (data.Scale < detailScaleThreshold)
                        {
                            _showingBriefLocationInformation.Add(_briefLocationInformationPool.Get((information) =>
                            {
                                information.gameObject.SetActive(true);
                                information.SetPosition(
                                    data.GamePositionToDisplayPosition(
                                        _imgMapMask.transform.position,
                                        _gameManager.Player.Parameters.position,
                                        _imgMapMask.transform.lossyScale
                                    )
                                );
                            }));
                        }
                        else
                        {
                            _showingDetailLocationInformation.Add(_detailLocationInformationPool.Get((information) =>
                            {
                                information.gameObject.SetActive(true);
                                information.SetPosition(
                                    data.GamePositionToDisplayPosition(
                                        _imgMapMask.transform.position,
                                        _gameManager.Player.Parameters.position,
                                        _imgMapMask.transform.lossyScale
                                    )
                                );
                            }));
                        }
                    }
                        break;
                    case CharacterSide.Player:
                    case CharacterSide.Neutral:
                    {
                        // 设置友方Npc信息
                        if (data.Scale < detailScaleThreshold)
                        {
                            _showingBriefAllyInformation.Add(_briefAllyInformationPool.Get((information) =>
                            {
                                information.gameObject.SetActive(true);
                                information.SetPosition(
                                    data.GamePositionToDisplayPosition(
                                        _imgMapMask.transform.position,
                                        character.Parameters.position,
                                        _imgMapMask.transform.lossyScale
                                    )
                                );
                                information.SetInformation(character);
                            }));
                        }
                        else
                        {
                            _showingDetailAllyInformation.Add(_detailAllyInformationPool.Get((information) =>
                            {
                                information.gameObject.SetActive(true);
                                information.SetPosition(
                                    data.GamePositionToDisplayPosition(
                                        _imgMapMask.transform.position,
                                        character.Parameters.position,
                                        _imgMapMask.transform.lossyScale
                                    )
                                );
                                information.SetInformation(character);
                            }));
                        }
                    }
                        break;
                    case CharacterSide.Enemy when character.HasTag("boss"):
                    {
                        // 设置敌方首领信息
                        if (data.Scale < detailScaleThreshold)
                        {
                            _showingBriefBossInformation.Add(_briefBossInformationPool.Get((information) =>
                            {
                                information.gameObject.SetActive(true);
                                information.SetPosition(
                                    data.GamePositionToDisplayPosition(
                                        _imgMapMask.transform.position,
                                        character.Parameters.position,
                                        _imgMapMask.transform.lossyScale
                                    )
                                );
                            }));
                        }
                        else
                        {
                            _showingDetailBossInformation.Add(_detailBossInformationPool.Get((information) =>
                            {
                                information.gameObject.SetActive(true);
                                information.SetPosition(
                                    data.GamePositionToDisplayPosition(
                                        _imgMapMask.transform.position,
                                        character.Parameters.position,
                                        _imgMapMask.transform.lossyScale
                                    )
                                );
                                information.SetInformation(character);
                            }));
                        }
                    }
                        break;
                    case CharacterSide.Enemy when character.HasTag("elite"):
                    {
                        // 设置敌方精英信息
                        if (data.Scale < detailScaleThreshold)
                        {
                            _showingBriefEliteInformation.Add(_briefEliteInformationPool.Get((information) =>
                            {
                                information.gameObject.SetActive(true);
                                information.SetPosition(
                                    data.GamePositionToDisplayPosition(
                                        _imgMapMask.transform.position,
                                        character.Parameters.position,
                                        _imgMapMask.transform.lossyScale
                                    )
                                );
                            }));
                        }
                        else
                        {
                            _showingDetailEliteInformation.Add(_detailEliteInformationPool.Get((information) =>
                            {
                                information.gameObject.SetActive(true);
                                information.SetPosition(
                                    data.GamePositionToDisplayPosition(
                                        _imgMapMask.transform.position,
                                        character.Parameters.position,
                                        _imgMapMask.transform.lossyScale
                                    )
                                );
                                information.SetInformation(character);
                            }));
                        }
                    }
                        break;
                    case CharacterSide.Enemy:
                    {
                        // 设置敌方敌人信息
                        if (data.Scale < detailScaleThreshold)
                        {
                            _showingBriefMonsterInformation.Add(_briefMonsterInformationPool.Get((information) =>
                            {
                                information.gameObject.SetActive(true);
                                information.SetPosition(
                                    data.GamePositionToDisplayPosition(
                                        _imgMapMask.transform.position,
                                        character.Parameters.position,
                                        _imgMapMask.transform.lossyScale
                                    )
                                );
                            }));
                        }
                        else
                        {
                            _showingDetailMonsterInformation.Add(_detailMonsterInformationPool.Get((information) =>
                            {
                                information.gameObject.SetActive(true);
                                information.SetPosition(
                                    data.GamePositionToDisplayPosition(
                                        _imgMapMask.transform.position,
                                        character.Parameters.position,
                                        _imgMapMask.transform.lossyScale
                                    )
                                );
                                information.SetInformation(character);
                            }));
                        }
                    }
                        break;
                }
            });

            // 设置交互物体信息
            _gameManager.RestSpots.ForEach(restSpot =>
            {
                if (data.Scale < detailScaleThreshold)
                {
                    _showingBriefRestSpotInformation.Add(_briefRestSpotInformationPool.Get((information) =>
                    {
                        information.gameObject.SetActive(true);
                        information.SetPosition(
                            data.GamePositionToDisplayPosition(
                                _imgMapMask.transform.position,
                                restSpot.transform.position,
                                _imgMapMask.transform.lossyScale
                            )
                        );
                    }));
                }
                else
                {
                    _showingDetailRestSpotInformation.Add(_detailRestSpotInformationPool.Get((information) =>
                    {
                        information.gameObject.SetActive(true);
                        information.SetPosition(
                            data.GamePositionToDisplayPosition(
                                _imgMapMask.transform.position,
                                restSpot.transform.position,
                                _imgMapMask.transform.lossyScale
                            )
                        );
                    }));
                }
            });

            // 设置地图指示器位置
            SetIndicatorPosition(
                data.Offset,
                _mapModel.GetIndicatorPosition().HasValue() ? _mapModel.GetIndicatorPosition().Value : Vector2.zero
            );
        }

        private void HandleIndicatorPositionChanged(Vector2 position)
        {
            SetIndicatorPosition(
                _mapModel.GetCurrentMap().HasValue() && _mapModel.GetCurrentMap().Value != null
                    ? _mapModel.GetCurrentMap().Value.Offset
                    : Vector2.zero,
                position
            );
        }

        private void SetIndicatorPosition(Vector2 offset, Vector2 position)
        {
            var relativePosition = position - offset;
            DestroyShowingIndicatorInformation();
            _showingIndicatorInformation.Add(_indicatorInformationPool.Get((information) =>
            {
                information.gameObject.SetActive(true);
                information.transform.SetAsLastSibling();
                information.SetPosition(_imgMapMask.transform.position +
                                        new Vector3(relativePosition.x * _imgMapMask.transform.lossyScale.x,
                                            relativePosition.y * _imgMapMask.transform.lossyScale.y, 0f));
            }));
        }

        private void DestroyShowingCharacterInformation()
        {
            _showingBriefLocationInformation.ForEach(information =>
                _briefLocationInformationPool.Release(
                    information,
                    (releasedInformation) => releasedInformation.gameObject.SetActive(false)
                )
            );
            _showingBriefLocationInformation.Clear();

            _showingBriefAllyInformation.ForEach(information =>
                _briefAllyInformationPool.Release(
                    information,
                    (releasedInformation) => releasedInformation.gameObject.SetActive(false)
                )
            );
            _showingBriefAllyInformation.Clear();

            _showingBriefMonsterInformation.ForEach(information =>
                _briefMonsterInformationPool.Release(
                    information,
                    (releasedInformation) => releasedInformation.gameObject.SetActive(false)
                )
            );
            _showingBriefMonsterInformation.Clear();

            _showingBriefEliteInformation.ForEach(information =>
                _briefEliteInformationPool.Release(
                    information,
                    (releasedInformation) => releasedInformation.gameObject.SetActive(false)
                )
            );
            _showingBriefEliteInformation.Clear();

            _showingBriefBossInformation.ForEach(information =>
                _briefBossInformationPool.Release(
                    information,
                    (releasedInformation) => releasedInformation.gameObject.SetActive(false)
                )
            );
            _showingBriefBossInformation.Clear();

            _showingDetailLocationInformation.ForEach(information =>
                _detailLocationInformationPool.Release(
                    information,
                    (releasedInformation) => releasedInformation.gameObject.SetActive(false)
                )
            );
            _showingDetailLocationInformation.Clear();

            _showingDetailAllyInformation.ForEach(information =>
                _detailAllyInformationPool.Release(
                    information,
                    (releasedInformation) => releasedInformation.gameObject.SetActive(false)
                )
            );
            _showingDetailAllyInformation.Clear();

            _showingDetailMonsterInformation.ForEach(information =>
                _detailMonsterInformationPool.Release(
                    information,
                    (releasedInformation) => releasedInformation.gameObject.SetActive(false)
                )
            );
            _showingDetailMonsterInformation.Clear();

            _showingDetailEliteInformation.ForEach(information =>
                _detailEliteInformationPool.Release(
                    information,
                    (releasedInformation) => releasedInformation.gameObject.SetActive(false)
                )
            );
            _showingDetailEliteInformation.Clear();

            _showingDetailBossInformation.ForEach(information =>
                _detailBossInformationPool.Release(
                    information,
                    (releasedInformation) => releasedInformation.gameObject.SetActive(false)
                )
            );
            _showingDetailBossInformation.Clear();
        }

        private void DestroyShowingInteractableInformation()
        {
            _showingBriefRestSpotInformation.ForEach(information =>
                _briefRestSpotInformationPool.Release(
                    information,
                    (releasedInformation) => releasedInformation.gameObject.SetActive(false)
                )
            );
            _showingBriefRestSpotInformation.Clear();

            _showingDetailRestSpotInformation.ForEach(information =>
                _detailRestSpotInformationPool.Release(
                    information,
                    (releasedInformation) => releasedInformation.gameObject.SetActive(false)
                )
            );
            _showingDetailRestSpotInformation.Clear();
        }

        private void DestroyShowingPinInformation()
        {
            _showingBriefPinInformation.ForEach(information =>
                _briefPinInformationPool.Release(
                    information,
                    (releasedInformation) => releasedInformation.gameObject.SetActive(false)
                )
            );
            _showingBriefPinInformation.Clear();

            _showingDetailPinInformation.ForEach(information =>
                _detailPinInformationPool.Release(
                    information,
                    (releasedInformation) => releasedInformation.gameObject.SetActive(false)
                )
            );
            _showingDetailPinInformation.Clear();
        }

        private void DestroyShowingIndicatorInformation()
        {
            _showingIndicatorInformation.ForEach(information =>
                _indicatorInformationPool.Release(
                    information,
                    (releasedInformation) => releasedInformation.gameObject.SetActive(false)
                )
            );
            _showingIndicatorInformation.Clear();
        }
    }
}
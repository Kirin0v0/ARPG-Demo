using System.Collections.Generic;
using System.Linq;
using Camera;
using Character;
using Common;
using Features.Game.UI.Map.Information;
using Framework.Common.Debug;
using Framework.Common.Function;
using Framework.Common.UI.Panel;
using Framework.Common.Util;
using Interact;
using Map;
using Package.Data;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Features.Game.UI.Map
{
    public class GameMiniMapPanel : BaseUGUIPanel
    {
        [Inject] private MiniMapCameraController _miniMapCameraController;
        [Inject] private GameManager _gameManager;
        [Inject] private MapManager _mapManager;

        [Title("小地图渲染图片")] [SerializeField] private RenderTexture renderTexture;

        [Title("地图信息预设体")] [SerializeField] private GameMapInformation locationInformation;
        [SerializeField] private GameMapInformation pinInformation;
        [SerializeField] private GameMapAllyInformation allyInformation;
        [SerializeField] private GameMapInformation monsterInformation;
        [SerializeField] private GameMapInformation eliteInformation;
        [SerializeField] private GameMapInformation bossInformation;
        [SerializeField] private GameMapInformation restSpotInformation;
        [SerializeField] private GameMapInformation weaponInformation;
        [SerializeField] private GameMapInformation gearInformation;
        [SerializeField] private GameMapInformation itemInformation;
        [SerializeField] private GameMapInformation materialInformation;

        private Image _imgMiniMapMask;
        private RawImage _riMiniMap;

        private ObjectPool<GameMapInformation> _locationInformationPool;
        private ObjectPool<GameMapInformation> _pinInformationPool;
        private ObjectPool<GameMapAllyInformation> _allyInformationPool;
        private ObjectPool<GameMapInformation> _monsterInformationPool;
        private ObjectPool<GameMapInformation> _eliteInformationPool;
        private ObjectPool<GameMapInformation> _bossInformationPool;
        private ObjectPool<GameMapInformation> _restSpotInformationPool;
        private ObjectPool<GameMapInformation> _weaponInformationPool;
        private ObjectPool<GameMapInformation> _gearInformationPool;
        private ObjectPool<GameMapInformation> _itemInformationPool;
        private ObjectPool<GameMapInformation> _materialInformationPool;

        private GameMapInformation _showingLocationInformation;
        private GameMapInformation _showingPinInformation;
        private readonly Dictionary<int, GameMapAllyInformation> _showingAllyInformation = new();
        private readonly Dictionary<int, GameMapInformation> _showingMonsterInformation = new();
        private readonly Dictionary<int, GameMapInformation> _showingEliteInformation = new();
        private readonly Dictionary<int, GameMapInformation> _showingBossInformation = new();
        private readonly Dictionary<int, GameMapInformation> _showingRestSpotInformation = new();
        private readonly Dictionary<int, GameMapInformation> _showingWeaponInformation = new();
        private readonly Dictionary<int, GameMapInformation> _showingGearInformation = new();
        private readonly Dictionary<int, GameMapInformation> _showingItemInformation = new();
        private readonly Dictionary<int, GameMapInformation> _showingMaterialInformation = new();

        private RenderTexture _miniMapTargetTexture;

        protected override void OnInit()
        {
            _imgMiniMapMask = GetWidget<Image>("ImgMiniMapMask");
            _riMiniMap = GetWidget<RawImage>("RiMiniMap");
            _riMiniMap.texture = renderTexture;

            // 初始化对象池
            _locationInformationPool = new ObjectPool<GameMapInformation>(
                createFunction: () => GameObject.Instantiate(locationInformation, _imgMiniMapMask.transform),
                destroyFunction: (information) => { GameObject.Destroy(information.gameObject); }
            );
            _pinInformationPool = new ObjectPool<GameMapInformation>(
                createFunction: () => GameObject.Instantiate(pinInformation, _imgMiniMapMask.transform),
                destroyFunction: (information) => { GameObject.Destroy(information.gameObject); }
            );
            _allyInformationPool = new ObjectPool<GameMapAllyInformation>(
                createFunction: () => GameObject.Instantiate(allyInformation, _imgMiniMapMask.transform),
                destroyFunction: (information) => { GameObject.Destroy(information.gameObject); }
            );
            _monsterInformationPool = new ObjectPool<GameMapInformation>(
                createFunction: () => GameObject.Instantiate(monsterInformation, _imgMiniMapMask.transform),
                destroyFunction: (information) => { GameObject.Destroy(information.gameObject); }
            );
            _eliteInformationPool = new ObjectPool<GameMapInformation>(
                createFunction: () => GameObject.Instantiate(eliteInformation, _imgMiniMapMask.transform),
                destroyFunction: (information) => { GameObject.Destroy(information.gameObject); }
            );
            _bossInformationPool = new ObjectPool<GameMapInformation>(
                createFunction: () => GameObject.Instantiate(bossInformation, _imgMiniMapMask.transform),
                destroyFunction: (information) => { GameObject.Destroy(information.gameObject); }
            );
            _restSpotInformationPool = new ObjectPool<GameMapInformation>(
                createFunction: () => GameObject.Instantiate(restSpotInformation, _imgMiniMapMask.transform),
                destroyFunction: (information) => { GameObject.Destroy(information.gameObject); }
            );
            _weaponInformationPool = new ObjectPool<GameMapInformation>(
                createFunction: () => GameObject.Instantiate(weaponInformation, _imgMiniMapMask.transform),
                destroyFunction: (information) => { GameObject.Destroy(information.gameObject); }
            );
            _gearInformationPool = new ObjectPool<GameMapInformation>(
                createFunction: () => GameObject.Instantiate(gearInformation, _imgMiniMapMask.transform),
                destroyFunction: (information) => { GameObject.Destroy(information.gameObject); }
            );
            _itemInformationPool = new ObjectPool<GameMapInformation>(
                createFunction: () => GameObject.Instantiate(itemInformation, _imgMiniMapMask.transform),
                destroyFunction: (information) => { GameObject.Destroy(information.gameObject); }
            );
            _materialInformationPool = new ObjectPool<GameMapInformation>(
                createFunction: () => GameObject.Instantiate(materialInformation, _imgMiniMapMask.transform),
                destroyFunction: (information) => { GameObject.Destroy(information.gameObject); }
            );
        }

        protected override void OnShow(object payload)
        {
            if (_miniMapCameraController.MiniMapCamera)
            {
                // 将自身RenderTexture设置为小地图相机的输出目标
                _miniMapTargetTexture = _miniMapCameraController.MiniMapCamera.targetTexture;
                _miniMapCameraController.MiniMapCamera.targetTexture = renderTexture;
            }
        }

        protected override void OnShowingUpdate(bool focus)
        {
            if (!_miniMapCameraController.MiniMapCamera)
            {
                return;
            }

            // 获取地图位置和尺寸
            var center = _miniMapCameraController.MiniMapCamera.transform.position;
            var width = _miniMapCameraController.ViewportSize.x;
            var height = _miniMapCameraController.ViewportSize.y;

            #region 更新角色信息

            // 更新最新位置并获取隐藏列表
            var toHideAllyInformation = _showingAllyInformation.Keys.ToHashSet();
            var toHideMonsterInformation = _showingMonsterInformation.Keys.ToHashSet();
            var toHideEliteInformation = _showingEliteInformation.Keys.ToHashSet();
            var toHideBossInformation = _showingBossInformation.Keys.ToHashSet();
            _gameManager.Characters.ForEach(character =>
            {
                // 如果角色是玩家或角色不处于显示范围内，则跳过更新
                if (character == _gameManager.Player || !ContainsDisplayRange(character.Parameters.position))
                {
                    return;
                }

                var displayPosition = GetDisplayPosition(character.Parameters.position);

                switch (character.Parameters.side)
                {
                    case CharacterSide.Player:
                    case CharacterSide.Neutral:
                    {
                        // 移除仍在展示的信息
                        toHideAllyInformation.Remove(character.Parameters.id);
                        // 如果已有信息就更新位置，否则就从对象池中获取并添加到展示列表中
                        if (!_showingAllyInformation.TryGetValue(character.Parameters.id, out var information))
                        {
                            information = _allyInformationPool.Get((information) =>
                            {
                                information.gameObject.SetActive(true);
                            });
                            information.SetInformation(character);
                            _showingAllyInformation.Add(character.Parameters.id, information);
                        }

                        information.SetPosition(displayPosition);
                    }
                        break;
                    case CharacterSide.Enemy when character.HasTag("boss"):
                    {
                        // 移除仍在展示的信息
                        toHideBossInformation.Remove(character.Parameters.id);
                        // 如果已有信息就更新位置，否则就从对象池中获取并添加到展示列表中
                        if (!_showingBossInformation.TryGetValue(character.Parameters.id, out var information))
                        {
                            information = _bossInformationPool.Get((information) =>
                            {
                                information.gameObject.SetActive(true);
                            });
                            _showingBossInformation.Add(character.Parameters.id, information);
                        }

                        information.SetPosition(displayPosition);
                    }
                        break;
                    case CharacterSide.Enemy when character.HasTag("elite"):
                    {
                        // 移除仍在展示的信息
                        toHideEliteInformation.Remove(character.Parameters.id);
                        // 如果已有信息就更新位置，否则就从对象池中获取并添加到展示列表中
                        if (!_showingEliteInformation.TryGetValue(character.Parameters.id, out var information))
                        {
                            information = _eliteInformationPool.Get((information) =>
                            {
                                information.gameObject.SetActive(true);
                            });
                            _showingEliteInformation.Add(character.Parameters.id, information);
                        }

                        information.SetPosition(displayPosition);
                    }
                        break;
                    case CharacterSide.Enemy:
                    {
                        // 移除仍在展示的信息
                        toHideMonsterInformation.Remove(character.Parameters.id);
                        // 如果已有信息就更新位置，否则就从对象池中获取并添加到展示列表中
                        if (!_showingMonsterInformation.TryGetValue(character.Parameters.id, out var information))
                        {
                            information = _monsterInformationPool.Get((information) =>
                            {
                                information.gameObject.SetActive(true);
                            });
                            _showingMonsterInformation.Add(character.Parameters.id, information);
                        }

                        information.SetPosition(displayPosition);
                    }
                        break;
                }
            });

            // 隐藏不再显示的信息
            toHideAllyInformation.ForEach(id =>
            {
                if (_showingAllyInformation.TryGetValue(id, out var information))
                {
                    _allyInformationPool.Release(information,
                        (information) => { information.gameObject.SetActive(false); });
                    _showingAllyInformation.Remove(id);
                }
            });
            toHideMonsterInformation.ForEach(id =>
            {
                if (_showingMonsterInformation.TryGetValue(id, out var information))
                {
                    _monsterInformationPool.Release(information,
                        (information) => { information.gameObject.SetActive(false); });
                    _showingMonsterInformation.Remove(id);
                }
            });
            toHideEliteInformation.ForEach(id =>
            {
                if (_showingEliteInformation.TryGetValue(id, out var information))
                {
                    _eliteInformationPool.Release(information,
                        (information) => { information.gameObject.SetActive(false); });
                    _showingEliteInformation.Remove(id);
                }
            });
            toHideBossInformation.ForEach(id =>
            {
                if (_showingBossInformation.TryGetValue(id, out var information))
                {
                    _bossInformationPool.Release(information,
                        (information) => { information.gameObject.SetActive(false); });
                    _showingBossInformation.Remove(id);
                }
            });

            #endregion

            #region 更新可交互对象信息

            // 更新休息点最新位置并获取隐藏列表
            var toHideRestSpotInformation = _showingRestSpotInformation.Keys.ToHashSet();
            _gameManager.RestSpots.ForEach(restSpotObject =>
            {
                // 如果对象不处于显示范围内，则跳过
                if (!ContainsDisplayRange(restSpotObject.transform.position))
                {
                    return;
                }

                var displayPosition = GetDisplayPosition(restSpotObject.transform.position);
                // 移除仍在展示的信息
                toHideRestSpotInformation.Remove(restSpotObject.Id);
                // 如果已有信息就更新位置，否则就从对象池中获取并添加到展示列表中
                if (!_showingRestSpotInformation.TryGetValue(restSpotObject.Id, out var information))
                {
                    information = _restSpotInformationPool.Get((information) =>
                    {
                        information.gameObject.SetActive(true);
                    });
                    _showingRestSpotInformation.Add(restSpotObject.Id, information);
                }

                information.SetPosition(displayPosition);
            });

            // 更新物品最新位置并获取隐藏列表
            var toHideWeaponInformation = _showingWeaponInformation.Keys.ToHashSet();
            var toHideGearInformation = _showingGearInformation.Keys.ToHashSet();
            var toHideItemInformation = _showingItemInformation.Keys.ToHashSet();
            var toHideMaterialInformation = _showingMaterialInformation.Keys.ToHashSet();
            _gameManager.Packages.ForEach(packageObject =>
            {
                // 如果对象不处于显示范围内，则跳过
                if (!ContainsDisplayRange(packageObject.transform.position))
                {
                    return;
                }

                var displayPosition = GetDisplayPosition(packageObject.transform.position);
                switch (packageObject.PackageType)
                {
                    case PackageType.Weapon:
                    {
                        // 移除仍在展示的信息
                        toHideWeaponInformation.Remove(packageObject.Id);
                        // 如果已有信息就更新位置，否则就从对象池中获取并添加到展示列表中
                        if (!_showingWeaponInformation.TryGetValue(packageObject.Id, out var information))
                        {
                            information = _weaponInformationPool.Get((information) =>
                            {
                                information.gameObject.SetActive(true);
                            });
                            _showingWeaponInformation.Add(packageObject.Id, information);
                        }

                        information.SetPosition(displayPosition);
                    }
                        break;
                    case PackageType.Gear:
                    {
                        // 移除仍在展示的信息
                        toHideGearInformation.Remove(packageObject.Id);
                        // 如果已有信息就更新位置，否则就从对象池中获取并添加到展示列表中
                        if (!_showingGearInformation.TryGetValue(packageObject.Id, out var information))
                        {
                            information = _gearInformationPool.Get((information) =>
                            {
                                information.gameObject.SetActive(true);
                            });
                            _showingGearInformation.Add(packageObject.Id, information);
                        }

                        information.SetPosition(displayPosition);
                    }
                        break;
                    case PackageType.Item:
                    {
                        // 移除仍在展示的信息
                        toHideItemInformation.Remove(packageObject.Id);
                        // 如果已有信息就更新位置，否则就从对象池中获取并添加到展示列表中
                        if (!_showingItemInformation.TryGetValue(packageObject.Id, out var information))
                        {
                            information = _itemInformationPool.Get((information) =>
                            {
                                information.gameObject.SetActive(true);
                            });
                            _showingItemInformation.Add(packageObject.Id, information);
                        }

                        information.SetPosition(displayPosition);
                    }
                        break;
                    case PackageType.Material:
                    {
                        // 移除仍在展示的信息
                        toHideMaterialInformation.Remove(packageObject.Id);
                        // 如果已有信息就更新位置，否则就从对象池中获取并添加到展示列表中
                        if (!_showingMaterialInformation.TryGetValue(packageObject.Id, out var information))
                        {
                            information = _materialInformationPool.Get((information) =>
                            {
                                information.gameObject.SetActive(true);
                            });
                            _showingMaterialInformation.Add(packageObject.Id, information);
                        }

                        information.SetPosition(displayPosition);
                    }
                        break;
                }
            });

            // 隐藏不再显示的信息
            toHideRestSpotInformation.ForEach(id =>
            {
                if (_showingRestSpotInformation.TryGetValue(id, out var information))
                {
                    _restSpotInformationPool.Release(information,
                        (information) => { information.gameObject.SetActive(false); });
                    _showingRestSpotInformation.Remove(id);
                }
            });
            toHideWeaponInformation.ForEach(id =>
            {
                if (_showingWeaponInformation.TryGetValue(id, out var information))
                {
                    _weaponInformationPool.Release(information,
                        (information) => { information.gameObject.SetActive(false); });
                    _showingWeaponInformation.Remove(id);
                }
            });
            toHideGearInformation.ForEach(id =>
            {
                if (_showingGearInformation.TryGetValue(id, out var information))
                {
                    _gearInformationPool.Release(information,
                        (information) => { information.gameObject.SetActive(false); });
                    _showingGearInformation.Remove(id);
                }
            });
            toHideItemInformation.ForEach(id =>
            {
                if (_showingItemInformation.TryGetValue(id, out var information))
                {
                    _itemInformationPool.Release(information,
                        (information) => { information.gameObject.SetActive(false); });
                    _showingItemInformation.Remove(id);
                }
            });
            toHideMaterialInformation.ForEach(id =>
            {
                if (_showingMaterialInformation.TryGetValue(id, out var information))
                {
                    _materialInformationPool.Release(information,
                        (information) => { information.gameObject.SetActive(false); });
                    _showingMaterialInformation.Remove(id);
                }
            });

            #endregion

            // 更新玩家定位信息
            if (_gameManager.Player)
            {
                // 如果没有玩家定位信息，就从对象池中获取一个
                if (!_showingLocationInformation)
                {
                    _showingLocationInformation = _locationInformationPool.Get((information) =>
                    {
                        information.gameObject.SetActive(true);
                    });
                }

                // 同步玩家位置
                _showingLocationInformation.SetPosition(GetDisplayPosition(_gameManager.Player.Parameters.position));
            }
            else
            {
                // 如果已有信息就释放
                if (_showingLocationInformation)
                {
                    _locationInformationPool.Release(_showingLocationInformation,
                        (information) => { information.gameObject.SetActive(false); });
                }

                _showingLocationInformation = null;
            }

            // 更新地图针信息
            if (_mapManager.GetPinPosition(out var pinPosition))
            {
                // 如果没有地图针信息，就从对象池中获取一个
                if (!_showingPinInformation)
                {
                    _showingPinInformation = _pinInformationPool.Get((information) =>
                    {
                        information.gameObject.SetActive(true);
                    });
                }

                // 同步地图针位置
                _showingPinInformation.SetPosition(GetDisplayPosition(pinPosition));
            }
            else
            {
                // 如果已有信息就释放
                if (_showingPinInformation)
                {
                    _pinInformationPool.Release(_showingPinInformation,
                        (information) => { information.gameObject.SetActive(false); });
                }

                _showingPinInformation = null;
            }

            return;

            bool ContainsDisplayRange(Vector3 worldPosition)
            {
                var radius = Mathf.Min(width, height) / 2f;
                var offset = new Vector2(worldPosition.x - center.x, worldPosition.z - center.z);
                return offset.sqrMagnitude <= radius * radius;
            }

            Vector3 GetDisplayPosition(Vector3 worldPosition)
            {
                // 获取相机视口位置（x、y处于0~1之间）
                var viewportPosition =
                    _miniMapCameraController.MiniMapCamera.WorldToViewportPoint(worldPosition);
                viewportPosition = new Vector3(Mathf.Clamp01(viewportPosition.x), Mathf.Clamp01(viewportPosition.y),
                    viewportPosition.z);

                // 将坐标转换为以(0.5,0.5)为中心的坐标系
                var centeredPosition = new Vector2(viewportPosition.x - 0.5f, viewportPosition.y - 0.5f);
                if (centeredPosition.magnitude > 0.5f)
                {
                    centeredPosition = centeredPosition.normalized * 0.5f;
                }

                // 转换为屏幕坐标系绝对位置
                var mapDisplayPosition =
                    new Vector3(centeredPosition.x + 0.5f, centeredPosition.y + 0.5f, 0);
                var displayOffset = _riMiniMap.rectTransform.rect.min * _riMiniMap.rectTransform.lossyScale +
                                    new Vector2(
                                        mapDisplayPosition.x * _riMiniMap.rectTransform.rect.width *
                                        _riMiniMap.rectTransform.lossyScale.x,
                                        mapDisplayPosition.y * _riMiniMap.rectTransform.rect.height *
                                        _riMiniMap.rectTransform.lossyScale.y);
                var displayPosition = _riMiniMap.transform.position +
                                      new Vector3(displayOffset.x, displayOffset.y, 0f);
                return displayPosition;
            }
        }

        protected override void OnHide()
        {
            if (_miniMapCameraController.MiniMapCamera)
            {
                // 恢复小地图相机的输出目标
                _miniMapCameraController.MiniMapCamera.targetTexture = _miniMapTargetTexture;
                _miniMapTargetTexture = null;
            }

            #region 销毁展示中的地图信息并清空对象池

            if (_showingLocationInformation)
            {
                _locationInformationPool.Release(
                    _showingLocationInformation,
                    (releasedInformation) => releasedInformation.gameObject.SetActive(false)
                );
                _showingLocationInformation = null;
            }

            _locationInformationPool.Clear();

            if (_showingPinInformation)
            {
                _pinInformationPool.Release(
                    _showingPinInformation,
                    (releasedInformation) => releasedInformation.gameObject.SetActive(false)
                );
                _showingPinInformation = null;
            }

            _pinInformationPool.Clear();

            _showingAllyInformation.ForEach(pair =>
                _allyInformationPool.Release(
                    pair.Value,
                    (releasedInformation) => releasedInformation.gameObject.SetActive(false)
                )
            );
            _showingAllyInformation.Clear();
            _allyInformationPool.Clear();

            _showingMonsterInformation.ForEach(pair =>
                _monsterInformationPool.Release(
                    pair.Value,
                    (releasedInformation) => releasedInformation.gameObject.SetActive(false)
                )
            );
            _showingMonsterInformation.Clear();
            _monsterInformationPool.Clear();

            _showingEliteInformation.ForEach(pair =>
                _eliteInformationPool.Release(
                    pair.Value,
                    (releasedInformation) => releasedInformation.gameObject.SetActive(false)
                )
            );
            _showingEliteInformation.Clear();
            _eliteInformationPool.Clear();

            _showingBossInformation.ForEach(pair =>
                _bossInformationPool.Release(
                    pair.Value,
                    (releasedInformation) => releasedInformation.gameObject.SetActive(false)
                )
            );
            _showingBossInformation.Clear();
            _bossInformationPool.Clear();

            _showingWeaponInformation.ForEach(pair =>
                _weaponInformationPool.Release(
                    pair.Value,
                    (releasedInformation) => releasedInformation.gameObject.SetActive(false)
                )
            );
            _showingWeaponInformation.Clear();
            _weaponInformationPool.Clear();

            _showingGearInformation.ForEach(pair =>
                _gearInformationPool.Release(
                    pair.Value,
                    (releasedInformation) => releasedInformation.gameObject.SetActive(false)
                )
            );
            _showingGearInformation.Clear();
            _gearInformationPool.Clear();

            _showingItemInformation.ForEach(pair =>
                _itemInformationPool.Release(
                    pair.Value,
                    (releasedInformation) => releasedInformation.gameObject.SetActive(false)
                )
            );
            _showingItemInformation.Clear();
            _itemInformationPool.Clear();

            _showingMaterialInformation.ForEach(pair =>
                _materialInformationPool.Release(
                    pair.Value,
                    (releasedInformation) => releasedInformation.gameObject.SetActive(false)
                )
            );
            _showingMaterialInformation.Clear();
            _materialInformationPool.Clear();

            #endregion
        }
    }
}
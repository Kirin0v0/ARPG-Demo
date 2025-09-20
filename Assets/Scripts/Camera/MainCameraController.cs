using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Camera.Data;
using Character;
using Character.Collider;
using Cinemachine;
using Common;
using Events;
using Features.Game.UI;
using Framework.Common.Debug;
using Framework.Common.UI.Toast;
using Framework.Common.Util;
using Framework.Core.Extension;
using Framework.Core.Lifecycle;
using Humanoid;
using Inputs;
using Map;
using Player;
using Rendering;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using PlayerInputManager = Inputs.PlayerInputManager;
using Random = UnityEngine.Random;

namespace Camera
{
    [RequireComponent(typeof(UnityEngine.Camera), typeof(AudioListener))]
    public class MainCameraController : MonoBehaviour
    {
        [Inject] private PlayerInputManager _playerInputManager;
        [Inject] private CameraModel _cameraModel;
        [Inject] private GameManager _gameManager;
        [Inject] private BattleManager _battleManager;
        [Inject] private MapManager _mapManager;
        [Inject] private IGameUIModel _gameUIModel;

        [Title("可见度检测配置")] [SerializeField, MinValue(0f)]
        private float visibilityCheckInterval = 0.1f;

        [SerializeField, MinValue(0f)] private float visibilityCheckDistance = 30f;

        [Title("固定相机配置")] [SerializeField] private CinemachineVirtualCameraBase unlockCamera;
        [SerializeField] private CinemachineVirtualCameraBase lockCamera;
        [SerializeField] private CinemachineVirtualCameraBase selectionCamera;

        [Title("调试")] [SerializeField] private bool debug;

        #region 相机场景

        private CameraScene? _currentCameraScene = null;

        private CameraScene? CurrentCameraScene
        {
            get => _currentCameraScene;
            set
            {
                if (_currentCameraScene == value || value == null)
                {
                    return;
                }

                SetCameraSceneInternal(value.Value);
            }
        }

        #endregion

        #region 目标选择功能

        private bool _inTargetSelections;
        private float _inTargetSelectionsTime = 0f;
        private readonly List<CharacterObject> _selectionTargets = new();
        private CharacterObject _selectedTarget;
        private int _selectedIndex = -1;

        private int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex == value)
                {
                    return;
                }

                _selectedIndex = value;

                // 删除选择相机目标的选中描边
                if (_selectedTarget)
                {
                    RenderingUtil.RemoveRenderingLayerMask(
                        _selectedTarget.gameObject,
                        GlobalRuleSingletonConfigSO.Instance.characterModelLayer,
                        GlobalRuleSingletonConfigSO.Instance.targetRenderingLayerMask
                    );
                }

                // 设置选择相机目标
                if (_inTargetSelections)
                {
                    _selectedTarget = _selectionTargets[_selectedIndex];
                    selectionCamera.LookAt = _selectedTarget.Visual.Eye.transform;
                    GameApplication.Instance.EventCenter.TriggerEvent(GameEvents.SelectTarget, _selectedTarget);
                    // 在允许时才会添加描边
                    if (_gameUIModel.AllowOutlineShowing().HasValue() && _gameUIModel.AllowOutlineShowing().Value)
                    {
                        // 添加选择相机目标的选中描边
                        RenderingUtil.AddRenderingLayerMask(
                            _selectedTarget.gameObject,
                            GlobalRuleSingletonConfigSO.Instance.characterModelLayer,
                            GlobalRuleSingletonConfigSO.Instance.targetRenderingLayerMask
                        );
                    }
                }
                else
                {
                    _selectedTarget = null;
                    selectionCamera.LookAt = null;
                }
            }
        }

        private bool AllowReactSelectionInput => _inTargetSelectionsTime >= 0.5f;

        #endregion

        #region 敌人锁定功能

        private readonly List<(CharacterObject character, int totalScore)> _visibleEnemyRank = new();
        private readonly Queue<CharacterObject> _targetEnemyRecordQueue = new();
        public List<CharacterObject> PlayerVisibleEnemies => _visibleEnemyRank.Select(x => x.character).ToList();
        private CharacterObject _lockTarget;

        #endregion

        #region 显示描边功能

        private readonly HashSet<GameObject> _visibleOutlineGameObjects = new();

        #endregion

        private VectorToDirectionInterceptor _vectorToDirectionInterceptor;

        private VectorToDirectionInterceptor VectorToDirectionInterceptor
        {
            get
            {
                if (!_vectorToDirectionInterceptor)
                {
                    _vectorToDirectionInterceptor = gameObject.GetComponent<VectorToDirectionInterceptor>();
                    if (!_vectorToDirectionInterceptor)
                    {
                        _vectorToDirectionInterceptor = gameObject.AddComponent<VectorToDirectionInterceptor>();
                        _vectorToDirectionInterceptor.cooldownDuration = 0.2f;
                    }
                }

                return _vectorToDirectionInterceptor;
            }
        }

        private UnityEngine.Camera _mainCamera;
        private AudioListener _audioListener;

        private readonly Dictionary<string, List<(Vector3 start, Vector3 end)>> _visibleRaycasts = new();

        private void Awake()
        {
            _mainCamera = GetComponent<UnityEngine.Camera>();
            _audioListener = GetComponent<AudioListener>();
            _audioListener.enabled = false;

            _cameraModel.GetScene().Observe(gameObject.GetMonoLifecycle(), HandleCameraSceneChanged);
            _cameraModel.GetLock().Observe(gameObject.GetMonoLifecycle(), HandleCameraLockOrUnlock);

            GameApplication.Instance.EventCenter.AddEventListener<CharacterObject[]>(
                GameEvents.StartTargetSelection, OnStartTargetSelection);
            GameApplication.Instance.EventCenter.AddEventListener(GameEvents.CancelTargetSelection,
                OnCancelTargetSelection);
            GameApplication.Instance.EventCenter.AddEventListener<CharacterObject>(
                GameEvents.FinishTargetSelection, OnFinishTargetSelection);

            _playerInputManager.RegisterActionPerformed(InputConstants.Lock, HandleLockInput);
            _playerInputManager.RegisterActionPerformed(InputConstants.Navigate, HandleSelectionNavigateInput);
            _playerInputManager.RegisterActionPerformed(InputConstants.Submit, HandleSelectionConfirmInput);
            _playerInputManager.RegisterActionPerformed(InputConstants.Cancel, HandleSelectionCancelInput);

            _gameManager.OnPlayerCreated += OnPlayerCreated;
            _gameManager.OnPlayerDestroyed += OnPlayerDestroyed;

            _mapManager.BeforeMapLoad += ResetCamera;
            _mapManager.AfterMapLoad += OnMapChanged;

            // 协程固定时间间隔检测场景物体的可见度
            StartCoroutine(CheckSceneVisibility());
            // 协程更新可见的敌人排序
            StartCoroutine(UpdateEnemyInCameraViewRank());
            // 协程更新可见的角色和可交互物品描边
            StartCoroutine(UpdateVisibleObjectsOutline());
        }

        private void FixedUpdate()
        {
            // 如果当前玩家不处于战斗状态，则切换至自由模式
            if (!_gameManager.Player || _gameManager.Player.Parameters.battleState != CharacterBattleState.Battle)
            {
                _cameraModel.SetLockData(new CameraLockData
                {
                    @lock = false,
                    lockTarget = null,
                });
            }

            // 如果当前锁定对象死亡，则切换至自由模式并删除已死亡的角色
            if (_cameraModel.GetLock().Value.lockTarget?.Parameters.dead == true)
            {
                _cameraModel.SetLockData(new CameraLockData
                {
                    @lock = false,
                    lockTarget = null,
                });

                _visibleEnemyRank.RemoveAll(x => x.character.Parameters.dead);
            }

            // 每帧查询可见敌人是否死亡，死亡则删除可见敌人
            var toRemoveEnemyIndex = 0;
            while (toRemoveEnemyIndex < _visibleEnemyRank.Count)
            {
                if (_visibleEnemyRank[toRemoveEnemyIndex].character.Parameters.dead)
                {
                    _visibleEnemyRank.RemoveAt(toRemoveEnemyIndex);
                }
                else
                {
                    toRemoveEnemyIndex++;
                }
            }
        }

        private void Update()
        {
            if (_inTargetSelections)
            {
                _inTargetSelectionsTime += Time.unscaledDeltaTime;
            }
            else
            {
                _inTargetSelectionsTime = 0f;
            }
        }

        private void OnGUI()
        {
            if (!debug)
            {
                return;
            }

            // 绘制锁定优先级文字
            for (var i = 0; i < _visibleEnemyRank.Count; i++)
            {
                var enemyRank = _visibleEnemyRank[i];
                if (enemyRank.character.Parameters.dead)
                {
                    continue;
                }

                var screenPosition =
                    MathUtil.GetGUIScreenPosition(GetComponent<UnityEngine.Camera>(),
                        enemyRank.character.transform.position);
                var contentWidth = 200f;
                var contentHeight = 100f;
                Rect rect = new Rect(screenPosition.x - contentWidth / 2, screenPosition.y - contentHeight,
                    contentWidth, contentHeight);
                GUI.Label(
                    rect,
                    $"优先级: {i}\n得分: {enemyRank.totalScore}",
                    new GUIStyle
                    {
                        fontSize = 30,
                    }
                );
            }
        }

        private void OnDrawGizmos()
        {
            if (!debug)
            {
                return;
            }

            // 绘制可见度射线
            foreach (var visibleRaycast in _visibleRaycasts)
            {
                Gizmos.color = Random.ColorHSV();
                foreach (var raycast in visibleRaycast.Value)
                {
                    Gizmos.DrawLine(raycast.start, raycast.end);
                }
            }
        }

        private void OnDestroy()
        {
            GameApplication.Instance?.EventCenter.RemoveEventListener<CharacterObject[]>(
                GameEvents.StartTargetSelection, OnStartTargetSelection);
            GameApplication.Instance?.EventCenter.RemoveEventListener(GameEvents.CancelTargetSelection,
                OnCancelTargetSelection);
            GameApplication.Instance?.EventCenter.RemoveEventListener<CharacterObject>(
                GameEvents.FinishTargetSelection, OnFinishTargetSelection);

            _playerInputManager.UnregisterActionPerformed(InputConstants.Lock, HandleLockInput);
            _playerInputManager.UnregisterActionPerformed(InputConstants.Navigate, HandleSelectionNavigateInput);
            _playerInputManager.UnregisterActionPerformed(InputConstants.Submit, HandleSelectionConfirmInput);
            _playerInputManager.UnregisterActionPerformed(InputConstants.Cancel, HandleSelectionCancelInput);

            _gameManager.OnPlayerCreated -= OnPlayerCreated;
            _gameManager.OnPlayerDestroyed -= OnPlayerDestroyed;

            _mapManager.BeforeMapLoad -= ResetCamera;
            _mapManager.AfterMapLoad -= OnMapChanged;

            _mainCamera = null;
        }

        private void HandleLockInput(InputAction.CallbackContext callbackContext)
        {
            if (_cameraModel.GetScene().Value.Scene != CameraScene.Normal || !_gameManager.Player ||
                _gameManager.Player.Parameters.battleState != CharacterBattleState.Battle)
            {
                return;
            }

            if (_cameraModel.GetLock().Value.@lock)
            {
                _cameraModel.SetLockData(new CameraLockData
                {
                    @lock = false,
                    lockTarget = null,
                });
            }
            else
            {
                // 如果当前不存在可见敌人，则不切换摄像机
                if (_visibleEnemyRank.Count == 0)
                {
                    return;
                }

                // 再次检查敌人是否死亡，死亡敌人不可锁定
                var lockEnemyIndex = _visibleEnemyRank.FindIndex(enemy => !enemy.character.Parameters.dead);
                if (lockEnemyIndex == -1)
                {
                    return;
                }

                var lockEnemy = _visibleEnemyRank[lockEnemyIndex];

                // 记录当前已被锁定过的敌人，并保持仅为5个
                _targetEnemyRecordQueue.Enqueue(lockEnemy.character);
                if (_targetEnemyRecordQueue.Count > 5)
                {
                    _targetEnemyRecordQueue.Dequeue();
                }

                _cameraModel.SetLockData(new CameraLockData
                {
                    @lock = true,
                    lockTarget = lockEnemy.character,
                });
            }
        }

        private void HandleSelectionNavigateInput(InputAction.CallbackContext callbackContext)
        {
            if (!AllowReactSelectionInput || !_inTargetSelections)
            {
                return;
            }

            var navigation = callbackContext.ReadValue<Vector2>();
            VectorToDirectionInterceptor.Intercept(navigation, direction =>
            {
                switch (direction)
                {
                    case VectorToDirectionInterceptor.Direction.Left:
                    {
                        SelectedIndex = SelectedIndex > 0 ? SelectedIndex - 1 : _selectionTargets.Count - 1;
                    }
                        break;
                    case VectorToDirectionInterceptor.Direction.Right:
                    {
                        SelectedIndex = SelectedIndex < _selectionTargets.Count - 1 ? SelectedIndex + 1 : 0;
                    }
                        break;
                }
            });
        }

        private void HandleSelectionConfirmInput(InputAction.CallbackContext callbackContext)
        {
            if (!AllowReactSelectionInput || !_inTargetSelections)
            {
                return;
            }

            GameApplication.Instance.EventCenter.TriggerEvent(GameEvents.FinishTargetSelection,
                _selectionTargets[SelectedIndex]);
        }

        private void HandleSelectionCancelInput(InputAction.CallbackContext callbackContext)
        {
            if (!AllowReactSelectionInput || !_inTargetSelections)
            {
                return;
            }

            GameApplication.Instance.EventCenter.TriggerEvent(GameEvents.CancelTargetSelection);
        }

        private void HandleCameraSceneChanged(CameraSceneData cameraSceneData)
        {
            CurrentCameraScene = cameraSceneData.Scene;
        }

        private void HandleCameraLockOrUnlock(CameraLockData cameraLockData)
        {
            // 删除锁定描边
            if (_lockTarget)
            {
                RenderingUtil.RemoveRenderingLayerMask(
                    _lockTarget.gameObject,
                    GlobalRuleSingletonConfigSO.Instance.characterModelLayer,
                    GlobalRuleSingletonConfigSO.Instance.lockRenderingLayerMask
                );
            }

            _lockTarget = cameraLockData.lockTarget;
            if (cameraLockData.@lock)
            {
                // 设置锁定时的相机参数
                lockCamera.Follow = _gameManager.Player?.transform;
                lockCamera.LookAt = _lockTarget?.Visual.Eye.transform ?? _lockTarget?.transform;
                lockCamera.transform.position = transform.position;
                lockCamera.transform.rotation = transform.rotation;
                lockCamera.gameObject.SetActive(true);
                unlockCamera.gameObject.SetActive(false);
                // 在允许时才会添加描边
                if (_lockTarget && _gameUIModel.AllowOutlineShowing().HasValue() &&
                    _gameUIModel.AllowOutlineShowing().Value)
                {
                    // 添加锁定描边
                    RenderingUtil.AddRenderingLayerMask(
                        _lockTarget.gameObject,
                        GlobalRuleSingletonConfigSO.Instance.characterModelLayer,
                        GlobalRuleSingletonConfigSO.Instance.lockRenderingLayerMask
                    );
                }
            }
            else
            {
                // 设置未锁定时的相机参数
                unlockCamera.Follow = _gameManager.Player?.transform;
                unlockCamera.LookAt = _gameManager.Player?.transform;
                unlockCamera.transform.position = transform.position;
                unlockCamera.transform.rotation = transform.rotation;
                unlockCamera.gameObject.SetActive(true);
                lockCamera.gameObject.SetActive(false);
            }
        }

        private void SetCameraSceneInternal(CameraScene value)
        {
            DebugUtil.LogOrange($"相机场景切换：{_currentCameraScene}-->{value}");

            BeforeCameraSceneChanged();
            _currentCameraScene = value;
            AfterCameraSceneChanged();

            return;

            void BeforeCameraSceneChanged()
            {
                switch (_currentCameraScene)
                {
                    case CameraScene.Custom:
                    {
                    }
                        break;
                    case CameraScene.Selection:
                    {
                        selectionCamera.gameObject.SetActive(false);
                    }
                        break;
                    case CameraScene.Normal:
                    {
                        lockCamera.gameObject.SetActive(false);
                        unlockCamera.gameObject.SetActive(false);
                    }
                        break;
                }
            }

            void AfterCameraSceneChanged()
            {
                switch (_currentCameraScene)
                {
                    case CameraScene.Custom:
                    {
                    }
                        break;
                    case CameraScene.Selection:
                    {
                        selectionCamera.gameObject.SetActive(true);
                        selectionCamera.Follow = _gameManager.Player?.transform;
                        selectionCamera.transform.position = transform.position;
                        selectionCamera.transform.rotation = transform.rotation;
                    }
                        break;
                    case CameraScene.Normal:
                    {
                        HandleCameraLockOrUnlock(_cameraModel.GetLock().Value);
                    }
                        break;
                }
            }
        }

        private void OnStartTargetSelection(CharacterObject[] targets)
        {
            if (targets.Length == 0)
            {
                Toast.Instance.Show("没有可选对象", 2f);
                StartCoroutine(DelayCancelTargetSelection());
                return;
            }
            
            _inTargetSelections = true;
            // 对可选目标进行排序
            SortSelectableTargets();
            // 默认成0开始
            SelectedIndex = 0;

            return;

            void SortSelectableTargets()
            {
                // 计算各目标分数
                var priorityScores = CalculateTargetPriorityScores();
                var visibilityScores = CalculateTargetVisibilityScores();
                var angleScores = CalculateTargetAngleScores();
                var distanceScores = CalculateTargetDistanceScores();
                // 根据得分调整优先级排序
                var totalScore = targets.Select((t, i) => (target: t,
                    score: priorityScores[i] + visibilityScores[i] + angleScores[i] + distanceScores[i])).ToList();
                totalScore.Sort((a, b) => a.score >= b.score ? -1 : 1);
                // 以起点为基础，将玩家与第一优先级目标的引导线为12点方向，对剩余目标进行顺时针排序
                var firstTarget = totalScore[0].target;
                var startPoint = _gameManager.Player.transform.position;
                var guideLine = firstTarget == _gameManager.Player
                    ? _gameManager.Player.transform.forward
                    : (
                        firstTarget.Parameters.position - _gameManager.Player.Parameters.position).normalized;
                var remainingTargets = totalScore.Skip(1).Select(x => x.target).Select(target =>
                {
                    // 计算目标相对于起点的方向向量
                    var direction = (target.Parameters.position - startPoint).normalized;
                    // 计算与引导线的顺时针夹角（以Y轴为旋转轴）
                    var angle = Vector3.SignedAngle(guideLine, direction, Vector3.up);
                    // 转换为0-360度表示（负值转正值）
                    var clockwiseAngle = angle < 0 ? 360 + angle : angle;
                    return new
                    {
                        target = target,
                        direction = direction,
                        angle = clockwiseAngle
                    };
                }).ToList();
                // 重新构建选择可选目标列表
                _selectionTargets.Clear();
                _selectionTargets.Add(firstTarget);
                _selectionTargets.AddRange(remainingTargets.OrderBy(x => x.angle).Select(x => x.target).ToList());
            }

            int[] CalculateTargetPriorityScores()
            {
                var scores = new int[targets.Length];
                for (var i = 0; i < targets.Length; i++)
                {
                    var target = targets[i];
                    if (target == _gameManager.Player)
                    {
                        scores[i] = 10;
                    }
                    else if (target == _lockTarget)
                    {
                        scores[i] = 3;
                    }
                    else
                    {
                        scores[i] = 0;
                    }
                }

                return scores;
            }

            int[] CalculateTargetVisibilityScores()
            {
                var scores = new int[targets.Length];
                for (var i = 0; i < targets.Length; i++)
                {
                    var target = targets[i];
                    scores[i] = target.Parameters.visible ? 3 : 0;
                }

                return scores;
            }

            int[] CalculateTargetAngleScores()
            {
                var angles = new List<(int index, float angle)>();
                for (var i = 0; i < targets.Length; i++)
                {
                    var target = targets[i];
                    angles.Add((
                        i,
                        Vector3.Angle(
                            transform.forward,
                            new Vector3(target.transform.position.x - transform.position.x, 0,
                                target.transform.position.z - transform.position.z)
                        )));
                }

                angles.Sort((a, b) => a.angle.CompareTo(b.angle));
                var scores = new int[targets.Length];
                for (var i = 0; i < angles.Count; i++)
                {
                    var visibleEnemyAngle = angles[i];
                    switch (i)
                    {
                        case 0:
                        {
                            scores[visibleEnemyAngle.index] = 3;
                        }
                            break;
                        case 1:
                        {
                            scores[visibleEnemyAngle.index] = 2;
                        }
                            break;
                        case 2:
                        {
                            scores[visibleEnemyAngle.index] = 1;
                        }
                            break;
                        default:
                        {
                            scores[visibleEnemyAngle.index] = 0;
                        }
                            break;
                    }
                }

                return scores;
            }

            int[] CalculateTargetDistanceScores()
            {
                var distances = new List<(int index, float distance)>();
                for (var i = 0; i < targets.Length; i++)
                {
                    var target = targets[i];
                    distances.Add((
                        i,
                        Vector3.Distance(_gameManager.Player.Visual.Center.transform.position,
                            target.Visual.Center.transform.position)
                    ));
                }

                distances.Sort((a, b) => a.distance.CompareTo(b.distance));
                var scores = new int[targets.Length];
                for (var i = 0; i < distances.Count; i++)
                {
                    var visibleEnemyAngle = distances[i];
                    switch (i)
                    {
                        case 0:
                        {
                            scores[visibleEnemyAngle.index] = 3;
                        }
                            break;
                        case 1:
                        {
                            scores[visibleEnemyAngle.index] = 2;
                        }
                            break;
                        case 2:
                        {
                            scores[visibleEnemyAngle.index] = 1;
                        }
                            break;
                        default:
                        {
                            scores[visibleEnemyAngle.index] = 0;
                        }
                            break;
                    }
                }

                return scores;
            }
        }

        private IEnumerator DelayCancelTargetSelection()
        {
            yield return new WaitForSecondsRealtime(0.1f);
            GameApplication.Instance.EventCenter.TriggerEvent(GameEvents.CancelTargetSelection);
        }
        
        private void OnCancelTargetSelection()
        {
            _inTargetSelections = false;
            _selectionTargets.Clear();
            SelectedIndex = -1;
        }

        private void OnFinishTargetSelection(CharacterObject target)
        {
            _inTargetSelections = false;
            _selectionTargets.Clear();
            SelectedIndex = -1;
        }

        private void OnPlayerCreated(PlayerCharacterObject player)
        {
            transform.position = player.transform.position;
            HandleCameraLockOrUnlock(_cameraModel.GetLock().Value);
            _audioListener.enabled = true;
        }

        private void OnPlayerDestroyed(PlayerCharacterObject player)
        {
            _audioListener.enabled = false;
        }

        private void ResetCamera()
        {
            // 重置主相机数据
            _inTargetSelections = false;
            _selectionTargets.Clear();
            _selectedIndex = -1;
            _visibleEnemyRank.Clear();
            _targetEnemyRecordQueue.Clear();
            _visibleOutlineGameObjects.Clear();
        }

        private void OnMapChanged()
        {
            if (!_mapManager.Map)
            {
                return;
            }

            if (!gameObject.TryGetComponent<Skybox>(out var skybox))
            {
                skybox = gameObject.AddComponent<Skybox>();
            }

            skybox.material = _mapManager.Map.Skybox;
        }

        private IEnumerator CheckSceneVisibility()
        {
            while (true)
            {
                _visibleRaycasts.Clear();

                // 检测角色可见度
                _gameManager.Characters.ForEach(character =>
                {
                    // 玩家一定可见
                    if (character == _gameManager.Player)
                    {
                        character.Parameters.visible = true;
                        return;
                    }
                    
                    // 角色正在玩家活跃的战斗则认为可见
                    if (character.Parameters.battleState == CharacterBattleState.Battle &&
                        _battleManager.IsPlayerActiveBattle(character.Parameters.battleId))
                    {
                        character.Parameters.visible = true;
                        return;
                    }

                    // 判断角色是否处于相机视口内，不处于即不可见
                    if (!MathUtil.IsWorldBoxInScreen(_mainCamera, character.Visual.Center.position,
                            new Vector3(character.CharacterController.radius,
                                character.CharacterController.height,
                                character.CharacterController.radius)))
                    {
                        character.Parameters.visible = false;
                        return;
                    }

                    // 判断相机与角色距离，超出检测距离认为太远不可见
                    if (Vector3.Distance(transform.position, character.Visual.Center.position) >
                        visibilityCheckDistance)
                    {
                        character.Parameters.visible = false;
                        return;
                    }

                    // 判断角色中心或边缘是否不被遮挡，不被遮挡认为可见
                    var center = character.Visual.Center.transform.position;
                    var top = character.Visual.Top.transform.position;
                    var bottom = character.Visual.Bottom.transform.position;
                    var left = character.Visual.Left.transform.position;
                    var right = character.Visual.Right.transform.position;
                    if (IsNotBlockInCamera(character.gameObject, center,
                            GlobalRuleSingletonConfigSO.Instance.characterModelLayer,
                            GlobalRuleSingletonConfigSO.Instance.maskLayer) ||
                        IsNotBlockInCamera(character.gameObject, top,
                            GlobalRuleSingletonConfigSO.Instance.characterModelLayer,
                            GlobalRuleSingletonConfigSO.Instance.maskLayer) ||
                        IsNotBlockInCamera(character.gameObject, bottom,
                            GlobalRuleSingletonConfigSO.Instance.characterModelLayer,
                            GlobalRuleSingletonConfigSO.Instance.maskLayer) ||
                        IsNotBlockInCamera(character.gameObject, left,
                            GlobalRuleSingletonConfigSO.Instance.characterModelLayer,
                            GlobalRuleSingletonConfigSO.Instance.maskLayer) ||
                        IsNotBlockInCamera(character.gameObject, right,
                            GlobalRuleSingletonConfigSO.Instance.characterModelLayer,
                            GlobalRuleSingletonConfigSO.Instance.maskLayer))
                    {
                        character.Parameters.visible = true;
                        return;
                    }

                    // 默认认为不可见
                    character.Parameters.visible = false;
                });

                // 检测交互物体可见度
                _gameManager.InteractableObjects.ForEach(obj =>
                {
                    // 判断物体是否处于相机视口内，不处于即不可见
                    if (!MathUtil.IsWorldPositionInScreen(_mainCamera, obj.transform.position))
                    {
                        obj.visible = false;
                        return;
                    }

                    // 判断相机与物体距离，超出检测距离认为太远不可见
                    if (Vector3.Distance(transform.position, obj.transform.position) >
                        visibilityCheckDistance)
                    {
                        obj.visible = false;
                        return;
                    }

                    // 判断物体中心是否不被遮挡，不被遮挡认为可见
                    var center = obj.transform.position;
                    if (IsNotBlockInCamera(obj.gameObject, center,
                            GlobalRuleSingletonConfigSO.Instance.interactLayer,
                            GlobalRuleSingletonConfigSO.Instance.maskLayer))
                    {
                        obj.visible = true;
                        return;
                    }

                    // 默认认为不可见
                    obj.visible = false;
                });

                yield return new WaitForSeconds(visibilityCheckInterval);
            }

            bool IsNotBlockInCamera(GameObject target, Vector3 position, LayerMask targetLayer, LayerMask obstacleLayer)
            {
                if (_visibleRaycasts.TryGetValue(target.GetInstanceID().ToString(), out var raycasts))
                {
                    raycasts.Add((start: transform.position, end: position));
                }
                else
                {
                    _visibleRaycasts.Add(target.GetInstanceID().ToString(), new List<(Vector3 start, Vector3 end)>
                    {
                        (start: transform.position, end: position),
                    });
                }

                // 获取相机与物体之间的射线检测碰撞列表
                var raycastHits = Physics.RaycastAll(
                    transform.position,
                    position - transform.position,
                    visibilityCheckDistance,
                    targetLayer | obstacleLayer
                );
                if (raycastHits.Length == 0)
                {
                    return false;
                }

                Array.Sort(raycastHits, (a, b) => a.distance.CompareTo(b.distance));
                // 这里规定如果在碰撞到目标物体前碰撞到障碍物就认为目标物体不可见
                foreach (var raycastHit in raycastHits)
                {
                    // 成功碰撞到目标物体，认为物体可见
                    if (raycastHit.transform.gameObject == target.gameObject ||
                        (raycastHit.transform.TryGetComponent<CharacterCollider>(out var characterCollider) &&
                         characterCollider.Owner.gameObject == target.gameObject))
                    {
                        return true;
                    }

                    // 碰撞到障碍物，认为物体不可见
                    if (((1 << raycastHit.transform.gameObject.layer) & obstacleLayer) != 0)
                    {
                        return false;
                    }
                }

                // 默认不可见
                return false;
            }
        }

        private IEnumerator UpdateEnemyInCameraViewRank()
        {
            while (true)
            {
                _visibleEnemyRank.Clear();
                if (_gameManager.Player && _gameManager.Player.BattleAbility)
                {
                    // 获取可见非死亡状态下的战斗敌人
                    var visibleEnemies = _gameManager.Player.BattleAbility.BattleEnemies.Where(character =>
                    {
                        // 过滤非敌人角色
                        if (character.Parameters.side != CharacterSide.Enemy)
                        {
                            return false;
                        }

                        // 过滤死亡角色
                        if (character.Parameters.dead)
                        {
                            return false;
                        }

                        // 过滤不可见敌人
                        if (!character.Parameters.visible)
                        {
                            return false;
                        }

                        // 最终返回可见
                        return true;
                    }).Select(x => x.GetComponent<CharacterObject>()).ToArray();

                    // 计算得分
                    var enemyAngleScore = CalculateEnemyAngleScores(visibleEnemies);
                    var enemyDistanceScore = CalculateEnemyDistanceScores(_gameManager.Player, visibleEnemies);
                    var enemyLockScore = CalculateEnemyLockScores(visibleEnemies);
                    // 根据得分调整排序
                    var enemyTotalScore = visibleEnemies.Select((t, i) =>
                        (enemy: t, score: enemyAngleScore[i] + enemyDistanceScore[i] + enemyLockScore[i])).ToList();
                    enemyTotalScore.Sort((a, b) => a.score >= b.score ? -1 : 1);
                    _visibleEnemyRank.Clear();
                    _visibleEnemyRank.AddRange(enemyTotalScore);
                }

                yield return 0;
            }

            int[] CalculateEnemyAngleScores(CharacterObject[] visibleEnemies)
            {
                var visibleEnemyAngles = new List<(int index, float angle)>();
                for (var i = 0; i < visibleEnemies.Length; i++)
                {
                    var enemy = visibleEnemies[i];
                    visibleEnemyAngles.Add((
                        i,
                        Vector3.Angle(
                            transform.forward,
                            new Vector3(enemy.transform.position.x - transform.position.x, 0,
                                enemy.transform.position.z - transform.position.z)
                        )));
                }

                visibleEnemyAngles.Sort((a, b) => a.angle.CompareTo(b.angle));
                var visibleEnemyAngleScore = new int[visibleEnemies.Length];
                for (var i = 0; i < visibleEnemyAngles.Count; i++)
                {
                    var visibleEnemyAngle = visibleEnemyAngles[i];
                    switch (i)
                    {
                        case 0:
                            visibleEnemyAngleScore[visibleEnemyAngle.index] = 3;
                            break;
                        case 1:
                            visibleEnemyAngleScore[visibleEnemyAngle.index] = 2;
                            break;
                        default:
                            visibleEnemyAngleScore[visibleEnemyAngle.index] = 1;
                            break;
                    }
                }

                return visibleEnemyAngleScore;
            }

            int[] CalculateEnemyDistanceScores(PlayerCharacterObject player, CharacterObject[] visibleEnemies)
            {
                var visibleEnemyDistances = new List<(int index, float distance)>();
                for (var i = 0; i < visibleEnemies.Length; i++)
                {
                    var enemy = visibleEnemies[i];
                    visibleEnemyDistances.Add((
                        i,
                        Vector3.Distance(player.Visual.Center.transform.position,
                            enemy.Visual.Center.transform.position)
                    ));
                }

                visibleEnemyDistances.Sort((a, b) => a.distance.CompareTo(b.distance));
                var visibleEnemyDistanceScore = new int[visibleEnemies.Length];
                for (var i = 0; i < visibleEnemyDistances.Count; i++)
                {
                    var visibleEnemyAngle = visibleEnemyDistances[i];
                    switch (i)
                    {
                        case 0:
                            visibleEnemyDistanceScore[visibleEnemyAngle.index] = 3;
                            break;
                        case 1:
                            visibleEnemyDistanceScore[visibleEnemyAngle.index] = 2;
                            break;
                        default:
                            visibleEnemyDistanceScore[visibleEnemyAngle.index] = 1;
                            break;
                    }
                }

                return visibleEnemyDistanceScore;
            }

            int[] CalculateEnemyLockScores(CharacterObject[] visibleEnemies)
            {
                var visibleEnemyLockScore = new int[visibleEnemies.Length];
                var targetEnemyRecords = _targetEnemyRecordQueue.ToArray();
                for (var i = 0; i < visibleEnemies.Length; i++)
                {
                    var visibleEnemy = visibleEnemies[i];
                    var isLockedPrevious = false;
                    for (var j = targetEnemyRecords.Length - 1; j >= 0; j--)
                    {
                        if (visibleEnemy == targetEnemyRecords[j])
                        {
                            isLockedPrevious = true;
                            if (j == targetEnemyRecords.Length - 1)
                            {
                                visibleEnemyLockScore[i] = 0;
                            }
                            else if (j == targetEnemyRecords.Length - 2)
                            {
                                visibleEnemyLockScore[i] = 1;
                            }
                            else
                            {
                                visibleEnemyLockScore[i] = 2;
                            }

                            break;
                        }
                    }

                    if (!isLockedPrevious)
                    {
                        visibleEnemyLockScore[i] = 3;
                    }
                }

                return visibleEnemyLockScore;
            }
        }

        private IEnumerator UpdateVisibleObjectsOutline()
        {
            while (true)
            {
                // 清空旧的显示描边列表并剔除其渲染层级
                _visibleOutlineGameObjects.ForEach(obj =>
                {
                    // 由于物体可能被销毁，所以这里需要提前判断
                    if (obj.IsGameObjectDestroyed())
                    {
                        return;
                    }

                    // 角色武器也要剔除渲染层级
                    if (obj.TryGetComponent<CharacterObject>(out var character))
                    {
                        RenderingUtil.RemoveRenderingLayerMask(
                            obj,
                            GlobalRuleSingletonConfigSO.Instance.characterModelLayer,
                            GlobalRuleSingletonConfigSO.Instance.outlineRenderingLayerMask);
                        if (character is HumanoidCharacterObject humanoidCharacter && humanoidCharacter.WeaponAbility)
                        {
                            if (humanoidCharacter.WeaponAbility.LeftHandWeaponSlot != null &&
                                humanoidCharacter.WeaponAbility.LeftHandWeaponSlot.Object)
                            {
                                RenderingUtil.RemoveRenderingLayerMask(
                                    humanoidCharacter.WeaponAbility.LeftHandWeaponSlot.Object.gameObject,
                                    -1,
                                    GlobalRuleSingletonConfigSO.Instance.outlineRenderingLayerMask);
                            }

                            if (humanoidCharacter.WeaponAbility.RightHandWeaponSlot != null &&
                                humanoidCharacter.WeaponAbility.RightHandWeaponSlot.Object)
                            {
                                RenderingUtil.RemoveRenderingLayerMask(
                                    humanoidCharacter.WeaponAbility.RightHandWeaponSlot.Object.gameObject,
                                    -1,
                                    GlobalRuleSingletonConfigSO.Instance.outlineRenderingLayerMask);
                            }
                        }
                    }
                    else
                    {
                        RenderingUtil.RemoveRenderingLayerMask(
                            obj,
                            -1,
                            GlobalRuleSingletonConfigSO.Instance.outlineRenderingLayerMask
                        );
                    }
                });
                _visibleOutlineGameObjects.Clear();

                // 这里直接判断是否允许添加描边，不允许就跳过后续
                if (!_gameUIModel.AllowOutlineShowing().HasValue() || !_gameUIModel.AllowOutlineShowing().Value)
                {
                    yield return 0;
                    continue;
                }

                // 获取需要显示描边的角色
                var visibleCharacters = _gameManager.Characters.Where(character =>
                {
                    // 玩家一定要显示描边
                    if (character == _gameManager.Player)
                    {
                        return true;
                    }

                    // 死亡角色不需要显示描边
                    if (character.Parameters.dead)
                    {
                        return false;
                    }

                    // 不可见角色不需要显示描边
                    if (!character.Parameters.visible)
                    {
                        return false;
                    }

                    // 最终返回需要显示描边
                    return true;
                }).ToList();
                _visibleOutlineGameObjects.AddRange(visibleCharacters.Select(character => character.gameObject));
                // 获取需要显示描边的可交互物体
                var visibleInteractableObjects = _gameManager.InteractableObjects.Where(obj => obj.visible).ToList();
                _visibleOutlineGameObjects.AddRange(visibleInteractableObjects.Select(obj => obj.gameObject));
                // 对需要显示描边的物体添加渲染层级
                _visibleOutlineGameObjects.ForEach(obj =>
                {
                    // 角色仅渲染角色模型层和武器对象，其他物体则渲染全部层
                    if (obj.TryGetComponent<CharacterObject>(out var character))
                    {
                        RenderingUtil.AddRenderingLayerMask(
                            obj,
                            GlobalRuleSingletonConfigSO.Instance.characterModelLayer,
                            GlobalRuleSingletonConfigSO.Instance.outlineRenderingLayerMask
                        );
                        if (character is HumanoidCharacterObject humanoidCharacter && humanoidCharacter.WeaponAbility)
                        {
                            if (humanoidCharacter.WeaponAbility.LeftHandWeaponSlot != null &&
                                humanoidCharacter.WeaponAbility.LeftHandWeaponSlot.Object)
                            {
                                RenderingUtil.AddRenderingLayerMask(
                                    humanoidCharacter.WeaponAbility.LeftHandWeaponSlot.Object.gameObject,
                                    -1,
                                    GlobalRuleSingletonConfigSO.Instance.outlineRenderingLayerMask);
                            }

                            if (humanoidCharacter.WeaponAbility.RightHandWeaponSlot != null &&
                                humanoidCharacter.WeaponAbility.RightHandWeaponSlot.Object)
                            {
                                RenderingUtil.AddRenderingLayerMask(
                                    humanoidCharacter.WeaponAbility.RightHandWeaponSlot.Object.gameObject,
                                    -1,
                                    GlobalRuleSingletonConfigSO.Instance.outlineRenderingLayerMask);
                            }
                        }
                    }
                    else
                    {
                        RenderingUtil.AddRenderingLayerMask(
                            obj,
                            -1,
                            GlobalRuleSingletonConfigSO.Instance.outlineRenderingLayerMask
                        );
                    }
                });
                yield return 0;
            }
        }
    }
}
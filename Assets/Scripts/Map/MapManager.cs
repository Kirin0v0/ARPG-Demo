using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Archive;
using Archive.Data;
using Framework.Common.Debug;
using Framework.DataStructure;
using Map.Data;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Map
{
    /// <summary>
    /// 地图管理器，管理地图的加载以及地图相关数据的持久化
    /// </summary>
    public class MapManager : MonoBehaviour, IArchivable
    {
        // 地图缓存数据
        private class MapCacheData
        {
            public int MapId;
            public string MapPath;
            public string MapSnapshotPath;
            public GameObject MapPrefab;
            public GameObject MapInstance;
            public Sprite MapSnapshot;
        }

        private const int EmptyMapId = -1;

        [Title("父对象配置")] [SerializeField] private Transform mapParent;

        [Title("缓存配置")] [SerializeField] private int maxCacheSize = 3;

        [Inject] private IObjectResolver _objectResolver;

        public event System.Action BeforeMapLoad;
        public event System.Action AfterMapLoad;

        private readonly LinkedList<MapCacheData> _mapCaches = new(); // 地图缓存链表，采用最近加载排序，将最远加载放在前面，最近加载放在后面

        public int MapId => _mapCaches.Count == 0 ? EmptyMapId : _mapCaches.Last.Value.MapId;

        public MapObject Map =>
            _mapCaches.Count == 0 ? null : _mapCaches.Last.Value.MapInstance?.GetComponent<MapObject>();

        private readonly Dictionary<string, HashSet<int>> _mapInteractedPackages = new(); // 全部地图上的已交互物品记录

        // 地图固定针位置
        private string _pinMapId;
        private Vector3 _pinPosition;

        private void Awake()
        {
            GameApplication.Instance.ArchiveManager.Register(this);
        }

        private void OnDestroy()
        {
            DestroyAllCaches();
            GameApplication.Instance?.ArchiveManager.Unregister(this);
        }

        public Task<GameObject> SwitchMap(int mapId, string path)
        {
            DebugUtil.LogYellow($"切换地图Id：{mapId}");
            var source = new TaskCompletionSource<GameObject>();
            BeforeMapLoad?.Invoke();

            // 销毁当前地图
            DestroyCurrentMap();

            // 遍历地图缓存，如果存在对应缓存则直接复用
            if (TryGetCache(mapId, true, out var mapCache))
            {
                mapCache.MapInstance = _objectResolver.Instantiate(mapCache.MapPrefab, mapParent);
                DebugUtil.LogYellow($"复用地图对象：{mapCache.MapInstance}");
                source.SetResult(mapCache.MapInstance);
                AfterMapLoad?.Invoke();
                return source.Task;
            }

            // 缓存新地图数据
            var newMapCache = new MapCacheData
            {
                MapId = mapId,
                MapPath = path,
                MapPrefab = null,
                MapSnapshot = null
            };
            AddNewestCache(newMapCache);

            // 加载地图资源
            GameApplication.Instance.AddressablesManager.LoadAssetAsync<GameObject>(path,
                handle =>
                {
                    // 加载地图对象后再次判断该地图数据是否处于缓存，不处于就直接返回，不做任何处理
                    if (!TryGetCache(mapId, false, out var mapCache))
                    {
                        source.SetResult(null);
                        return;
                    }

                    mapCache.MapPrefab = handle;
                    mapCache.MapInstance = _objectResolver.Instantiate(handle, mapParent);
                    // 如果地图对象有地图快照，则加载地图快照，直到加载结束才算是完成加载
                    if (mapCache.MapInstance.TryGetComponent<MapObject>(out var mapObject))
                    {
                        mapCache.MapSnapshotPath = mapObject.Snapshot.Path;
                        if (!string.IsNullOrEmpty(mapCache.MapSnapshotPath))
                        {
                            GameApplication.Instance.AddressablesManager.LoadAssetAsync<Sprite>(
                                mapCache.MapSnapshotPath, sprite =>
                                {
                                    // 加载地图快照后再次判断该地图数据是否处于缓存，不处于就直接返回，不做任何处理
                                    if (!TryGetCache(mapId, false, out var mapCache))
                                    {
                                        source.SetResult(null);
                                        return;
                                    }

                                    mapCache.MapSnapshot = sprite;
                                    DebugUtil.LogYellow($"创建地图对象：{mapCache.MapInstance}");
                                    source.SetResult(mapCache.MapInstance);
                                    AfterMapLoad?.Invoke();
                                });
                        }
                    }
                    else
                    {
                        DebugUtil.LogYellow($"创建地图对象：{mapCache.MapInstance}");
                        source.SetResult(mapCache.MapInstance);
                        AfterMapLoad?.Invoke();
                    }
                });

            return source.Task;
        }

        public void DestroyCurrentMap()
        {
            if (_mapCaches.Count == 0)
            {
                return;
            }

            // 销毁当前地图实例
            var mapCacheData = _mapCaches.Last.Value;
            if (mapCacheData.MapInstance != null)
            {
                DebugUtil.LogYellow($"销毁地图对象：{mapCacheData.MapInstance}");
                GameObject.Destroy(mapCacheData.MapInstance);
                mapCacheData.MapInstance = null;
            }
        }

        /// <summary>
        /// 记录地图交互物品已交互
        /// </summary>
        /// <param name="id"></param>
        public void RecordPackageInteracted(int id)
        {
            if (_mapInteractedPackages.TryGetValue(MapId.ToString(), out var interactedPackages))
            {
                interactedPackages.Add(id);
            }
            else
            {
                interactedPackages = new HashSet<int> { id };
                _mapInteractedPackages.Add(MapId.ToString(), interactedPackages);
            }
        }

        public bool IsPackageInteracted(int mapId, int mapPackageId)
        {
            return _mapInteractedPackages.TryGetValue(mapId.ToString(), out var interactedPackages) &&
                   interactedPackages.Contains(mapPackageId);
        }

        public bool GetPinPosition(out Vector3 position)
        {
            position = Vector3.zero;
            if (!string.Equals(MapId.ToString(), _pinMapId))
            {
                return false;
            }

            position = _pinPosition;
            return true;
        }

        public void SetPinPosition(Vector3 position)
        {
            _pinMapId = MapId.ToString();
            _pinPosition = position;
        }

        public void Save(ArchiveData archiveData)
        {
            // 保存地图记录
            archiveData.map.maps = new();
            _mapInteractedPackages.ForEach(pair =>
            {
                if (archiveData.map.maps.TryGetValue(pair.Key, out var mapItemArchiveData))
                {
                    mapItemArchiveData.interactedPackages = pair.Value.ToList();
                }
                else
                {
                    archiveData.map.maps.Add(pair.Key, new MapItemArchiveData
                    {
                        id = int.Parse(pair.Key),
                        interactedPackages = pair.Value.ToList()
                    });
                }
            });
            archiveData.map.pin = new MapPinArchiveData
            {
                mapId = _pinMapId,
                position = new SerializableVector3(_pinPosition)
            };
        }

        public void Load(ArchiveData archiveData)
        {
            // 这里仅加载地图记录，至于地图则交由场景加载，方便控制加载对象的流程
            _mapInteractedPackages.Clear();
            archiveData.map.maps.ForEach(pair =>
            {
                _mapInteractedPackages.Add(pair.Key, pair.Value.interactedPackages.ToHashSet());
            });
            _pinMapId = archiveData.map.pin.mapId;
            _pinPosition = archiveData.map.pin.position.ToVector3();
        }

        private bool TryGetCache(int mapId, bool reuse, out MapCacheData data)
        {
            data = null;
            // 遍历链表查找匹配的缓存
            var current = _mapCaches.First;
            while (current != null)
            {
                if (current.Value.MapId == mapId)
                {
                    data = current.Value;
                    // 如果复用，则将缓存移动到链表末尾
                    if (reuse)
                    {
                        _mapCaches.Remove(current);
                        _mapCaches.AddLast(data);
                    }

                    return true;
                }

                current = current.Next;
            }

            return false;
        }

        private void AddNewestCache(MapCacheData data)
        {
            _mapCaches.AddLast(data);
            // 如果缓存数量超过最大缓存数，则卸载最老的缓存
            if (_mapCaches.Count > maxCacheSize)
            {
                var oldestCache = _mapCaches.First.Value;
                DestroyCache(oldestCache);
                _mapCaches.RemoveFirst();
            }
        }

        private void DestroyAllCaches()
        {
            foreach (var cache in _mapCaches)
            {
                DestroyCache(cache);
            }

            _mapCaches.Clear();
        }

        private void DestroyCache(MapCacheData data)
        {
            DebugUtil.LogYellow($"卸载地图缓存：{data.MapId}");
            
            if (data.MapInstance)
            {
                GameObject.Destroy(data.MapInstance);
                data.MapInstance = null;
            }

            if (!string.IsNullOrEmpty(data.MapPath))
            {
                GameApplication.Instance?.AddressablesManager.ReleaseAsset<GameObject>(data.MapPath);
                data.MapPath = null;
            }

            if (!string.IsNullOrEmpty(data.MapSnapshotPath))
            {
                GameApplication.Instance?.AddressablesManager.ReleaseAsset<Sprite>(data.MapSnapshotPath);
                data.MapSnapshotPath = null;
            }
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using System.Threading.Tasks;
using JetBrains.Annotations;

public enum BTileAttribute
{
    None = 0,
    StartPoint,
    EndPoint,

    BTileAttribute_End
}

public enum BTileType : int
{
    Empty = 0,
    Block,
    Attack,
    Guard,
    Shield,

    BTileType_End
}

public enum BBoardDrawState
{
    None = 0,
    Drawing,
    Finished,

    BBoardDrawstate_End
}

public enum UnitActionType
{
    None = -1,
    Idle = 0,
    Move,
    Attack,
    Guard,
    Dodge,
    KnockBack,

    Death,

    UnitActionType_End
}

public interface IDataLoad
{
    public void LoadData();
    public void Release();
}


[System.Serializable]
public struct LevelBaseData
{
    public uint Idx;
    public uint Level;
    public uint NeedExp;
    public float MaxHp;
    public float Atk;
    public float Def;
    public float Dodge;
    public float Spd;
}

[System.Serializable]
public struct UnitData
{
    public uint Idx;
    public uint NameIdx;
    public uint AnimGroupIdx;
    public float Hp;
    public float MaxHp;
    public float Atk;
    public float Def;
    public float Dodge; //0~1000
    public float Spd;
    public Sprite PortraitSpr;
}

public struct ApplyStatusData
{
    public float MaxHp;
    public float Atk;
    public float Def;
    public float Dodge; //0~1000
    public float Spd;
    public float ShieldCrushTime;

    public override string ToString()
    {
        return $"(maxHP:{MaxHp}, atk:{Atk}, def:{Def}, dodge:{Dodge}, spd:{Spd}, sct:{ShieldCrushTime})";
    }

    public void Clear()
    {
        MaxHp = 0.0f;
        Atk = 0.0f;
        Def = 0.0f;
        Dodge = 0.0f; //0~1000
        Spd = 0.0f;
        ShieldCrushTime = 0.0f;
    }
}

public struct UsageUnitData
{
    public ReactiveProperty<float> Hp;
    public ReactiveProperty<float> MaxHp;
    public float Atk;
    public float Def;
    public float Dodge; //0~1000
    public float Spd;
    public float ShieldCrushTime;
    public ReactiveProperty<float> ShieldCrushedTime;

    public override string ToString()
    {
        return $"(hp:{Hp.Value}, maxHP:{MaxHp.Value}, atk:{Atk}, def:{Def}, dodge:{Dodge}, spd:{Spd}, sct:{ShieldCrushTime}, crushedTime:{ShieldCrushedTime.Value})";
    }
}

[System.Serializable]
public struct TextData
{
    public uint Idx;
    public string Text;
}

[System.Serializable]
public struct MonsterData
{
    public uint Idx;
    public uint UnitIdx;
    public uint PatternIdx;
    public float SizeScale;
    public uint BoardDefaultWidth;
    public uint BoardDefaultHeight;
}

[System.Serializable]
public struct StageData
{
    public uint Idx;
    public uint Stage;
    public uint SubStage;
    public uint[] MonsterIdx;
    public uint[] MonsterCount;
    public uint[] RewardIdx; //monsteridx.len
}

[System.Serializable]
public struct AnimationData
{
    public uint Idx;
    public string ControllerKey;
}

[System.Serializable]
public struct RewardData
{
    public uint Idx;
    public uint Exp;
}

[System.Serializable]
public struct MapData
{
    public uint Idx;
    public uint[] StageIdx;
}

public struct UnitActionData
{
    public static UnitActionData DefaultAction_None = new UnitActionData(UnitActionType.None);
    public static UnitActionData DefaultAction_Idle = new UnitActionData(UnitActionType.Idle);
    public static UnitActionData DefaultAction_Move = new UnitActionData(UnitActionType.Move);

    public UnitActionType Type;
    public System.Action BeforeAction;
    public System.Action AfterAction;

    public UnitActionData(UnitActionType type, System.Action beforeAction = null, System.Action afterAction = null)
    {
        this.Type = type;
        this.BeforeAction = beforeAction;
        this.AfterAction = afterAction;
    }
}

[System.Serializable]
public class UserData
{
    public int Level;
    public int Exp;
    public int MapIdx;
    public int StageIdx; //현재 진행
    public int SavedMapIdx;
    public int SavedStageIdx; //가장 마지막으로 저장된 위치

    public SavedUserData Convert()
    {
        return new SavedUserData()
        {
            Level = Level, 
            Exp = Exp, 
            MapIdx = MapIdx, 
            StageIdx = StageIdx, 
            SavedMapIdx = SavedMapIdx,
        };
    }
}

[System.Serializable]
public class SavedUserData 
{
    public int Level;
    public int Exp;
    public int MapIdx;
    public int StageIdx; //현재 진행
    public int SavedMapIdx;
    public int SavedStageIdx; //가장 마지막으로 저장된 위치

    public UserData Convert()
    {
        return new UserData()
        {
            Level = Level, 
            Exp = Exp,
            MapIdx = MapIdx, 
            StageIdx = StageIdx, 
            SavedMapIdx= SavedMapIdx,
            SavedStageIdx = SavedStageIdx,
        };
    }
}

[System.Serializable]
public class ClearData
{
    public int StageIdx;
    public int ClearState; //0none, 1clear, 2rewarded
}

[System.Serializable]
public class StageClearData
{
    public Dictionary<int, ClearData> ClearDatas;

    public SavedStageClearData Convert()
    {
        SavedStageClearData sscd = new SavedStageClearData() { ClearDatas = new List<ClearData>() };

        sscd.ClearDatas = ClearDatas.Values.ToList();

        return sscd;
    }

    public void SetState(int stageIdx, int state)
    {
        if(!ClearDatas.ContainsKey(stageIdx))
        {
            ClearDatas.Add(stageIdx, new ClearData() { StageIdx = stageIdx, ClearState = 0 });
        }

        ClearDatas[stageIdx].ClearState = state;
    }

    public int GetState(int stageIdx)
    {
        if(ClearDatas.ContainsKey(stageIdx))
        {
            return ClearDatas[stageIdx].ClearState;
        }

        return -1;
    }
}

[System.Serializable]
public class SavedStageClearData
{
    public List<ClearData> ClearDatas;

    public StageClearData Convert()
    {
        StageClearData scd = new StageClearData() { ClearDatas = new Dictionary<int, ClearData>() };

        foreach(var d in ClearDatas)
        {
            if(!scd.ClearDatas.ContainsKey(d.StageIdx))
            {
                scd.ClearDatas.Add(d.StageIdx, d);
            }
        }

        return scd;
    }
}


public static class Commons
{
    // Resource keys (addressables) - centralize keys to avoid magic strings
    public readonly static string ResKey_PlayerUnit = "PlayerUnit";
    public readonly static string ResKey_MonsterUnit = "MonsterUnit";
    public readonly static string ResKey_Tile = "Tile";
    public readonly static string ResKey_Board = "Board";
    public readonly static string ResKey_HPUI = "HPUI";
    public readonly static string ResKey_PortraitUI = "PortraitUI";

    public readonly static string ResKey_BaseTile = "BaseTile";
    public readonly static string ResKey_BlockTile = "BlockTile";
    public readonly static string ResKey_AttackTile = "AttackTile"; 
    public readonly static string ResKey_GuardTile = "GuardTile";
    public readonly static string ResKey_ShieldTile = "ShieldTile";

    public readonly static string SceneName_Init = "InitScene";
    public readonly static string SceneName_Loading = "LoadingScene";
    public readonly static string SceneName_Main = "MainScene";
    public readonly static string SceneName_StageSelect = "StageSelectScene";

    public readonly static int BOARD_WIDTH_MIN = 4;
    public readonly static int BOARD_WIDTH_MAX = 128;
    public readonly static int BOARD_HEIGHT_MIN = 4;
    public readonly static int BOARD_HEIGHT_MAX = 128;
    public readonly static int DEFAULT_TILE_WIDTH = 64;
    public readonly static int DEFAULT_TILE_HEIGHT = 64;
    public readonly static int DEFAULT_BOARD_WIDTH = 256;
    public readonly static int DEFAULT_BOARD_HEIGHT = 256;

    public class Util
    {
        public static int GetRandom(int min, int max, int exclusive = int.MaxValue)
        {
            int r = Random.Range(min, max);
            bool isValid = min <= exclusive && exclusive <= max;

            while (isValid && r == exclusive)
            {
                r = Random.Range(min, max);
            }

            return r;
        }

        public static int GetRandom(int min, int max, int[] exclusives = null)
        {
            int r = Random.Range(min, max);
            bool isValid = exclusives != null && exclusives.Length > 0;
            bool isFull = exclusives != null && max - min <= exclusives.Count(x => min <= x && x <= max);

            while (isValid && !isFull && exclusives.Contains(r))
            {
                r = Random.Range(min, max);
                isFull = max - min <= exclusives.Count(x => min <= x && x <= max);
            }

            if (isFull)
            {
                r = -1;
            }

            return r;
        }

        // Convert a world position to a Canvas local position (anchoredPosition) suitable for UI placement.
        // canvas: target Canvas
        // cam: camera used for WorldToScreenPoint (use main camera). For ScreenSpace-Overlay canvas, cam may be null.
        public static Vector2 WorldToCanvasPosition(Canvas canvas, Camera cam, Vector3 worldPos)
        {
            if (canvas == null)
                return Vector2.zero;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();

            // For ScreenSpace-Overlay, RectTransformUtility.WorldToScreenPoint ignores camera, but call with cam (can be null)
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPos);

            Camera forUiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : (canvas.worldCamera ?? cam);

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, forUiCamera, out localPoint);

            return localPoint;
        }

    }

    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance == null)
            {
                Instance = this as T;
                
                DontDestroyOnLoad(gameObject);
                OnSingletonAwake();
            }
        }

        protected virtual void OnDestroy()
        {
            OnSingletonDestroyed();

            if (Instance == this)
                Instance = null;
        }

        // 파생 클래스는 이 훅들만 오버라이드하면 됨
        protected virtual void OnSingletonAwake() { }
        protected virtual void OnSingletonDestroyed() { }
    }

    public class SimplePool<T> where T : MonoBehaviour
    {
        // Optional helpers
        public int Available => pool.Count;
        public int TotalOwned => ownedInstanceIds.Count;
        public int TotalCreated => totalCount;
        public int Capacity => capacity;

        private T prefab = null;
        private Transform parent = null;

        // Pooled object with reference to the prefab it was created from
        private struct PooledObject
        {
            public T Instance;
            public T Prefab;

            public PooledObject(T instance, T prefab)
            {
                Instance = instance;
                Prefab = prefab;
            }
        }

        private bool isAddressable = false;
        private string addressableKey = string.Empty;

        private Queue<PooledObject> pool = null;
        private System.Action<T> onGet = null;
        private System.Action<T> onRelease = null;

        // Track ownership and currently pooled instances to prevent double-release or cross-pool release
        private HashSet<EntityId> ownedInstanceIds = new HashSet<EntityId>();
        private HashSet<EntityId> pooledInstanceIds = new HashSet<EntityId>();

        // capacity: maximum total instances that may be created by this pool
        private int capacity = 0;
        private int totalCount = 0; // number of instances created by this pool

        public SimplePool(int capacity, T prefab, Transform parent = null, System.Action<T> onGet = null, System.Action<T> onRelease = null)
        {
            this.capacity = capacity;
            this.prefab = prefab;
            this.parent = parent;
            this.pool = new Queue<PooledObject>();
            this.onGet = onGet;
            this.onRelease = onRelease;
            this.isAddressable = false;
            this.addressableKey = string.Empty;
        }

        /// <summary>
        /// Create a pool that uses Addressables via ResourceManager to instantiate items.
        /// Note: this constructor does not synchronously create instances. Call PrewarmAsync to populate the pool.
        /// </summary>
        public SimplePool(int capacity, string addressableKey, Transform parent = null, System.Action<T> onGet = null, System.Action<T> onRelease = null)
        {
            this.capacity = capacity;
            this.prefab = null;
            this.parent = parent;
            this.pool = new Queue<PooledObject>();
            this.onGet = onGet;
            this.onRelease = onRelease;
            this.isAddressable = true;
            this.addressableKey = addressableKey;
        }

        public T Get()
        {
            if (pool.Count > 0)
            {
                PooledObject p = pool.Dequeue();
                T obj = p.Instance;

                // remove from pooled ids
                EntityId id = obj.GetEntityId();
                pooledInstanceIds.Remove(id);

                obj.gameObject.SetActive(true);
                onGet?.Invoke(obj);

                return obj;
            }

            // create new instance and record ownership if capacity allows
            if (totalCount < capacity)
            {
                if (!isAddressable && prefab != null)
                {
                    T newObj = Object.Instantiate(prefab, parent);
                    EntityId nid = newObj.GetEntityId();
                    ownedInstanceIds.Add(nid);
                    totalCount++;

                    onGet?.Invoke(newObj);

                    return newObj;
                }

                Debug.LogWarning($"SimplePool.Get: addressable pool empty and synchronous creation is not available for {typeof(T).Name}. Consider calling PrewarmAsync before Get.");
                return null;
            }

            Debug.LogWarning($"SimplePool.Get: reached capacity ({capacity}) for {typeof(T).Name}, cannot create new instance.");
            return null;
        }

        public void Release(T obj)
        {
            if (obj == null)
                return;

            EntityId id = obj.GetEntityId();

            // If this instance was not created by this pool, destroy it to avoid cross-pool reuse
            if (!ownedInstanceIds.Contains(id))
            {
                Debug.LogWarning($"SimplePool.Release: object (id:{id}) was not created by this pool. Destroying it to avoid cross-pool issues.");
                Object.Destroy(obj.gameObject);
                return;
            }

            // Prevent double-release
            if (pooledInstanceIds.Contains(id))
            {
                Debug.LogWarning($"SimplePool.Release: object (id:{id}) is already pooled. Ignoring release.");
                return;
            }

            onRelease?.Invoke(obj);
            obj.gameObject.transform.SetParent(parent);
            obj.gameObject.SetActive(false);

            pool.Enqueue(new PooledObject(obj, prefab));
            pooledInstanceIds.Add(id);
        }

        public void ReleaseAll()
        {
            foreach(PooledObject pooled in pool)
            { 
                Release(pooled.Instance);
            }
        }

        public void Clear()
        {
            while (pool.Count > 0)
            {
                PooledObject pooled = pool.Dequeue();

                if (isAddressable)
                {
                    ResourceManager.Instance.ReleaseInstance(pooled.Instance as GameObject);
                }
                else
                {
                    GameObject.Destroy(pooled.Instance);
                }
            }
        }

        // Create inactive instances up to requested count (bounded by capacity)
        public void Prewarm(int count)
        {
            int canCreate = Mathf.Max(0, capacity - totalCount);
            int toCreate = Mathf.Min(count, canCreate);

            for (int i = 0; i < toCreate; i++)
            {
                T newObj = Object.Instantiate(prefab, parent);
                newObj.gameObject.SetActive(false);
                EntityId nid = newObj.GetEntityId();
                ownedInstanceIds.Add(nid);
                pooledInstanceIds.Add(nid);
                pool.Enqueue(new PooledObject(newObj, prefab));
                totalCount++;
            }
        }

        /// <summary>
        /// Async prewarm for addressable-backed pool. Instantiates up to count items using ResourceManager.InstantiateAsyncTask.
        /// </summary>
        public async Task PrewarmAsync(int count)
        {
            if (!isAddressable || string.IsNullOrEmpty(addressableKey))
            {
                Prewarm(count);
                return;
            }

            int canCreate = Mathf.Max(0, capacity - totalCount);
            int toCreate = Mathf.Min(count, canCreate);

            for (int i = 0; i < toCreate; i++)
            {
                GameObject go = await ResourceManager.Instance.InstantiateAsyncTask(addressableKey, parent);
                if (go == null)
                {
                    Debug.LogError($"PrewarmAsync: failed to instantiate addressable '{addressableKey}'");
                    break;
                }

                T newObj = go.GetComponent<T>();
                if (newObj == null)
                {
                    Debug.LogError($"PrewarmAsync: instantiated object does not contain component {typeof(T).Name}");
                    GameObject.Destroy(go);
                    break;
                }

                newObj.gameObject.SetActive(false);
                EntityId nid = newObj.GetEntityId();
                ownedInstanceIds.Add(nid);
                pooledInstanceIds.Add(nid);
                pool.Enqueue(new PooledObject(newObj, prefab));
                totalCount++;
            }
        }
    }
}

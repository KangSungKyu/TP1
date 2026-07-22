using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;


public enum ResponseType : uint
{
    Success = 0,

    Failed_DB_Error = 1,

    Failed_NotFound_Data = 2,
    Failed_NotFound_Session = 3,
    Failed_NotFound_DB = 4,
    
    ResponseType_End
}

public enum DataTableType : uint //1~999
{
    None = 0,
    TextData,
    UnitData,
    LevelBaseData,
    MonsterData,
    AnimationData,
    MapData,
    StageData,
    RewardData,
    SkillData,
    MonsterPatternData,

    DataTableType_End
}

public enum BBoardType
{
    None = 0,
    Offensive,
    Defensive,

    BBoardType_End
}

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
    Skill,
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

public enum PuzzleResultType
{
    NotYet = 0,
    Completed,
    Expired,

    PuzzleResultType_End
}

public enum UnitActionType
{
    None = -1,
    Idle = 0,
    Move,
    Attack,
    Guard,
    Dodge,
    Death,

    UnitActionType_End
}

public enum DamageResultType
{
    Damaged = 0,
    Dodge,

    DamageResultType_End
}


public enum StageType : int
{
    Battle = 0,
    Rest,

    StageType_End
}

public enum SkillType
{
    None = 0,
    Damaged,
    Buffed,

    SkillType_End
}

public enum SkillTargetType
{
    Single = 0,
    Multiple,

    SkillTargetType_End
}

public enum MonsterPatternType
{
    Cycle = 0,
    Random,

    MonsterPatternType_End
}

public interface IDataLoad
{
    public int GetDataCount();
    public void LoadData(string csvText);
    public void Release();
}


[System.Serializable]
public class LevelBaseData
{
    [Name("idx")]
    public uint Idx { get; set; }
    [Name("level")]
    public uint Level { get; set; }
    [Name("needexp")]
    public uint NeedExp { get; set; }
    [Name("maxhp")]
    public float MaxHp { get; set; }
    [Name("atk")]
    public float Atk { get; set; }
    [Name("def")]
    public float Def { get; set; }
    [Name("dodge")]
    public float Dodge { get; set; }
    [Name("spd")]
    public float Spd { get; set; }
}

[System.Serializable]
public class UnitData
{
    [Name("idx")]
    public uint Idx { get; set; }
    [Name("nameidx")]
    public uint NameIdx { get; set; }
    [Name("animgroupidx")]
    public uint AnimGroupIdx { get; set; }
    [Name("hp")]
    public float Hp { get; set; }
    [Name("maxhp")]
    public float MaxHp { get; set; }
    [Name("atk")]
    public float Atk { get; set; }
    [Name("def")]
    public float Def { get; set; }
    [Name("dodge")]
    public float Dodge { get; set; } //0~1000
    [Name("spd")]
    public float Spd { get; set; }
    [Name("portraitidx")]
    public int PortraitIdx { get; set; }
    [Name("attackidx")]
    public int AttackIdx { get; set; }
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

public class PuzzleResult
{
    public PuzzleResultType ResultType;
    public BBoard CurrentBoard;
    public UnitBase Attacker;
    public List<UnitBase> TargetList;
    public List<SkillData> SkillList;
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
public class TextData
{
    [Name("idx")]
    public uint Idx { get; set; }
    [Name("text")]
    public string Text { get; set; }
}

[System.Serializable]
public class MonsterData
{
    [Name("idx")]
    public uint Idx { get; set; }
    [Name("unitidx")]
    public uint UnitIdx { get; set; }
    [Name("patternidx")]
    public uint PatternIdx { get; set; }
    [Name("sizescale")]
    public float SizeScale { get; set; }
    [Name("boarddefaultwidth")]
    public uint BoardDefaultWidth { get; set; }
    [Name("boarddefaultheight")]
    public uint BoardDefaultHeight { get; set; }
}

[System.Serializable]
public class StageData
{
    [Name("idx")]
    public uint Idx { get; set; }
    [Name("type")]
    public StageType Type { get; set; } //0 battle, 1 point
    [Name("stage")]
    public uint Stage { get; set; }
    [Name("substage")]
    public uint SubStage { get; set; }
    [Name("monsteridx"), TypeConverter(typeof(UIntArrayConverter))]
    public uint[] MonsterIdx { get; set; }
    [Name("monstercount"), TypeConverter(typeof(UIntArrayConverter))]
    public uint[] MonsterCount { get; set; }
    [Name("rewardidx"), TypeConverter(typeof(UIntArrayConverter))]
    public uint[] RewardIdx { get; set; } //monsteridx.len
    [Name("bgsprite")]
    public string BgSprite { get; set; }
}

[System.Serializable]
public class AnimationData
{
    [Name("idx")]
    public uint Idx { get; set; }
    [Name("controllerkey")]
    public string ControllerKey { get; set; }
}

[System.Serializable]
public class RewardData
{
    [Name("idx")]
    public uint Idx { get; set; }
    [Name("exp")]
    public uint Exp { get; set; }
}

[System.Serializable]
public class MapData
{
    [Name("idx")]
    public uint Idx { get; set; }
    [Name("stageidx"), TypeConverter(typeof(UIntArrayConverter))]
    public uint[] StageIdx { get; set; }
    [Name("bgsprite")]
    public string BgSprite { get; set; }
}


[System.Serializable]
public class SkillData
{
    [Name("idx")]
    public uint Idx { get; set; }

    [Name("type")]
    public SkillType Type { get; set; }

    [Name("targettype")]
    public SkillTargetType TargetType { get; set; }

    [Name("targetcount")]
    public uint TargetCount { get; set; }

    [Name("pervalue")]
    public int PerValue { get; set; } //pervalue / 1000.0
    [Name("requirelevel")]
    public int RequireLevel { get; set; }
    [Name("quota")]
    public int Quota { get; set; }
    [Name("tileres")]
    public string TileRes { get; set; }
}

[System.Serializable]
public class MonsterPatternData
{
    [Name("idx")]
    public uint Idx { get; set; }
    [Name("type")]
    public MonsterPatternType Type { get; set; }
    [Name("skillidx"), TypeConverter(typeof(UIntArrayConverter))]
    public uint[] SkillIdx { get; set; }
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
public class ClientData
{
    public string ClientId = string.Empty;

    public static ClientData CreateClientData()
    {
        ClientData data = new ClientData();

        data.ClientId = $"tpc-{System.Guid.NewGuid().ToString()}";

        return data;
    }
}

[System.Serializable]
public class DumpData
{
    //dump
}

[System.Serializable]
public class APIResponseData<T>
{
    [JsonProperty("response")]
    public ResponseType response;
    [JsonProperty("data")]
    public T data;
}

[System.Serializable]
public class UserData
{
    public int UserId;
    public string ClientId;
    public int Level;
    public int Exp;
    public int MapIdx;
    public int StageIdx; //현재 진행
    public int SavedMapIdx;
    public int SavedStageIdx; //가장 마지막으로 저장된 위치

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
    public int UserId;
    public Dictionary<int, ClearData> ClearDatas;

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
public class LoginData
{
    public UserData userData;
    public List<ClearData> stageClearData;
    public List<SkillSlotData> userSkillData;
}

[System.Serializable]
public class UserLevelExpData
{
    public int Level;
    public int Exp;
}

[System.Serializable]
public class SkillSlotData
{
    public int SkillIdx;
    public int Slot;
    public int Quota;
}

[System.Serializable]
public class UserSkillData
{
    public List<SkillSlotData> SkillSlots;

    public int GetSkillIdx(int slot)
    {
        return SkillSlots.Where((o) => o.Slot == slot).Select((s) => s.SkillIdx).FirstOrDefault();
    }

    public uint[] GetEquipedSkills()
    {
        return SkillSlots.Where((o) => o.Slot > 0 && o.Quota > 0).Select((s) => (uint)s.SkillIdx).ToArray();
    }

    public uint GetEquipedSkill()
    {
        var skills = GetEquipedSkills();

        if(skills != null && skills.Length > 0)
        {
            return skills[UnityEngine.Random.Range(0, skills.Length)];
        }

        return 0;
    }
}

[System.Serializable]
public class EnterUserStageData
{
    public int MapIdx;
    public int StageIdx;
}

[System.Serializable]
public class UsedUserSkillData
{
    public int SkillIdx;
    public int Count;
}

public static class Commons
{
    // Resource keys (addressables) - centralize keys to avoid magic strings

    public readonly static string ResKey_AlterMsg = "AlterMsg";

    public readonly static string ResKey_PlayerUnit = "PlayerUnit";
    public readonly static string ResKey_MonsterUnit = "MonsterUnit";
    public readonly static string ResKey_Tile = "Tile";
    public readonly static string ResKey_Board = "Board";
    public readonly static string ResKey_HPUI = "HPUI";
    public readonly static string ResKey_PortraitUI = "PortraitUI";
    public readonly static string ResKey_DamageFont = "DamageFont";
    public readonly static string ResKey_HitEffect = "HitEffect";
    public readonly static string ResKey_TargetLine = "ToTargetLine";

    public readonly static string ResKey_BaseTile = "BaseTile";
    public readonly static string ResKey_BlockTile = "BlockTile";
    public readonly static string ResKey_AttackTile = "AttackTile"; 
    public readonly static string ResKey_GuardTile = "GuardTile";
    public readonly static string ResKey_ShieldTile = "ShieldTile";
    public readonly static string ResKey_SkillTile = "SkillTile";
    public readonly static string ResKey_StartTile = "StartTile";
    public readonly static string ResKey_EndTile = "EndTile";

    public readonly static string ResKey_PortraitUIs = "Portraits";
    public readonly static string ResKey_StageUIs = "StageUIs";
    public readonly static string ResKey_WinBG = "WinBG";
    public readonly static string ResKey_DefeatBG = "DefeatBG";
    public readonly static string ResKey_WinText = "WinText";
    public readonly static string ResKey_DefeatText = "DefeatText";

    public readonly static string ResKey_Atlas_Tile = "AT_Tile";

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

    public readonly static uint DEFAULT_PLAYER_IDX = 2001;

    public class Util
    {
        public static DataTableType GetDataTableType(uint idx)
        {
            return (DataTableType)(idx / 1000);
        }

        public static uint GetDataInnerId(uint idx)
        {
            return idx % 1000;
        }

        public static uint CreateDataIdx(DataTableType type, uint innerId)
        {
            return ((uint)type * 1000) + (innerId);
        }

        public static List<T> ParseFromCSV<T>(string csvText)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",", // 구분자 설정
                PrepareHeaderForMatch = args => args.Header,
            };

            using (var reader = new StringReader(csvText))
            using (var csv = new CsvReader(reader, config))
            {
                // GetRecords는 스트리밍 방식으로 데이터를 읽어 객체 리스트로 반환
                return csv.GetRecords<T>().ToList();
            }
        }

        public static int GetRandom(int min, int max, int exclusive = int.MaxValue)
        {
            int r = UnityEngine.Random.Range(min, max);
            bool isValid = min <= exclusive && exclusive <= max;

            while (isValid && r == exclusive)
            {
                r = UnityEngine.Random.Range(min, max);
            }

            return r;
        }

        public static int GetRandom(int min, int max, int[] exclusives = null)
        {
            int r = UnityEngine.Random.Range(min, max);
            bool isValid = exclusives != null && exclusives.Length > 0;
            bool isFull = exclusives != null && max - min <= exclusives.Count(x => min <= x && x <= max);

            while (isValid && !isFull && exclusives.Contains(r))
            {
                r = UnityEngine.Random.Range(min, max);
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

        public static Vector3 CalcBezierPoint_Quadratic(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;

            Vector3 point = uu * p0;

            point += 2 * u * t * p1;
            point += tt * p2;

            return point;
        }

        public static string ToJson<T>(T data)
        {
            string json = JsonConvert.SerializeObject(data); // true: 가독성 좋게 들여쓰기

            return json;
        }

        public static T FromJson<T>(string json)
        {
            T data = default;

            if (json != string.Empty)
            {
                data = JsonConvert.DeserializeObject<T>(json);
            }

            return data;
        }
    }

    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        protected void Awake()
        {
            if (Instance == null)
            {
                Instance = this as T;
                
                DontDestroyOnLoad(gameObject);
                OnSingletonAwake();
            }
            else if(Instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected void OnDestroy()
        {
            if (Instance == this)
            {
                OnSingletonDestroyed();

                Instance = null;
            }
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
                    T newObj = UnityEngine.Object.Instantiate(prefab, parent);
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
                UnityEngine.Object.Destroy(obj.gameObject);
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


        public void Clear()
        {
            while (pool.Count > 0)
            {
                PooledObject pooled = pool.Dequeue();

                if(pooled.Instance is GameObject gobj)
                {
                    if (isAddressable)
                    {
                        ResourceManager.Instance.ReleaseInstance(gobj);
                    }
                    else
                    {
                        GameObject.Destroy(gobj);
                    }
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
                T newObj = UnityEngine.Object.Instantiate(prefab, parent);
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

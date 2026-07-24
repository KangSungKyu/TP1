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

}

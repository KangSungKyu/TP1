using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using static Commons;

public class DataTableManager : Singleton<DataTableManager>
{
    private readonly Dictionary<DataTableType, IDataLoad> dataList = new Dictionary<DataTableType, IDataLoad>()
    {
        { DataTableType.TextData, new TextDataForm() },
        { DataTableType.UnitData, new UnitDataForm() },
        { DataTableType.LevelBaseData, new LevelBaseDataForm() },
        { DataTableType.MonsterData, new MonsterDataForm() },
        { DataTableType.AnimationData, new AnimationDataForm() },
        { DataTableType.MapData, new MapDataForm() },
        { DataTableType.StageData, new StageDataForm() },
        { DataTableType.RewardData, new RewardDataForm() },
        { DataTableType.SkillData, new SkillDataForm() },
        { DataTableType.MonsterPatternData, new MonsterPatternDataForm() },
    };

    public T GetDB<T>(uint idx) where T : class, IDataLoad
    {
        DataTableType dtt = Util.GetDataTableType(idx);

        if(idx <= 0)
        {
            dtt = dataList.Where((o)=>o.Value is T).Select((s) => s.Key).FirstOrDefault();
        }

        return GetDB<T>(dtt);
    }

    public T GetDB<T>(DataTableType dataTableType) where T : class, IDataLoad
    {
        if(dataList.ContainsKey(dataTableType))
        {
            return dataList[dataTableType] as T;
        }

        return null;
    }

    public UnitData GetUnitData(uint idx)
    {
        var db = GetDB<UnitDataForm>(idx)?.DB;

        if (db.ContainsKey(idx))
        {
            return db[idx];
        }

        return default(UnitData);
    }

    public MonsterData GetMonsterData(uint idx)
    {
        var db = GetDB<MonsterDataForm>(idx)?.DB;

        if (db.ContainsKey(idx))
        {
            return db[idx];
        }

        return default(MonsterData);
    }

    public StageData GetStageData(uint idx)
    {
        var db = GetDB<StageDataForm>(idx)?.DB;

        if (db.ContainsKey(idx))
        {
            return db[idx];
        }

        return default(StageData);
    }

    public string GetText(uint idx)
    {
        var db = GetDB<TextDataForm>(idx)?.DB;

        if (db.ContainsKey(idx))
        {
            return db[idx].Text;
        }

        return string.Empty;
    }

    public AnimationData GetAnimationData(uint idx)
    {
        var db = GetDB<AnimationDataForm>(idx)?.DB;

        if (db.ContainsKey(idx))
        {
            return db[idx];
        }

        return default(AnimationData);
    }

    public LevelBaseData GetLevelBaseData(uint level)
    {
        var db = GetDB<LevelBaseDataForm>(DataTableType.LevelBaseData)?.DB;

        if (db != null)
        {
            return db.Where((o) => o.Value.Level == level).Select((s) => s.Value).FirstOrDefault();
        }

        return default(LevelBaseData);
    }

    public RewardData GetRewardData(uint idx)
    {
        var db = GetDB<RewardDataForm>(idx)?.DB;

        if (db.ContainsKey(idx))
        {
            return db[idx];
        }

        return default(RewardData);
    }

    public MapData GetMapData(uint idx)
    {
        var db = GetDB<MapDataForm>(idx)?.DB;

        if(db.ContainsKey(idx))
        {
            return db[idx];
        }

        return default(MapData);
    }

    public SkillData GetSkillData(uint idx)
    {
        var db = GetDB<SkillDataForm>(idx)?.DB;

        if (db.ContainsKey(idx))
        {
            return db[idx];
        }

        return default(SkillData);
    }

    public MonsterPatternData GetMonsterPatternData(uint idx)
    {
        var db = GetDB<MonsterPatternDataForm>(idx)?.DB;

        if(db.ContainsKey(idx))
        {
            return db[idx];
        }

        return default(MonsterPatternData);
    }

    protected override void OnSingletonAwake()
    {
        base.OnSingletonAwake();

        StartCoroutine(IEPreloadScriptableObjects());
    }

    protected override void OnSingletonDestroyed()
    {
        foreach(var pair in dataList)
        {
            pair.Value?.Release();
        }

        dataList.Clear();

        base.OnSingletonDestroyed();
    }

    private IEnumerator IEPreloadScriptableObjects()
    {
        // 'Data' 라벨을 가진 에셋들만 로드
        //csv파일이라 수정필요
        //idx에서 테이블 종류를 구분
        string targetLabel = "Data";

        var locationsHandle = Addressables.LoadResourceLocationsAsync(targetLabel, typeof(TextAsset));

        yield return locationsHandle;

        if (locationsHandle.Status == AsyncOperationStatus.Succeeded)
        {
            ResourceManager.Instance.LoadAssetsAsync<TextAsset>(locationsHandle.Result, (asset) =>
            {
                using (var reader = new StringReader(asset.text))
                {
                    string firstLine = reader.ReadLine();
                    string secondLine = reader.ReadLine();
                    string[] headers = secondLine.Split(',');

                    DataTableType dataTableType = Commons.Util.GetDataTableType(uint.Parse(headers[0]));

                    if(dataList.ContainsKey(dataTableType))
                    {
                        dataList[dataTableType].LoadData(asset.text);

                        Debug.Log($"Data Loaded: {asset.name}");
                    }
                }
            });
        }

        Addressables.Release(locationsHandle);
    }

}
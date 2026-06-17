using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using static Commons;

public class SODataTable : Singleton<SODataTable>
{
    private Dictionary<Type, IDataLoad> dataList = new Dictionary<Type, IDataLoad>();

    public T GetDB<T>() where T : class, IDataLoad
    {
        if(dataList.ContainsKey(typeof(T)))
        {
            return dataList[typeof(T)] as T;
        }

        return null;
    }

    public UnitData GetUnitData(uint idx)
    {
        var db = GetDB<UnitDataForm>()?.DB;

        if (db.ContainsKey(idx))
        {
            return db[idx];
        }

        return default(UnitData);
    }

    public MonsterData GetMonsterData(uint idx)
    {
        var db = GetDB<MonsterDataForm>()?.DB;

        if (db.ContainsKey(idx))
        {
            return db[idx];
        }

        return default(MonsterData);
    }

    public StageData GetStageData(uint idx)
    {
        var db = GetDB<StageDataForm>()?.DB;

        if (db.ContainsKey(idx))
        {
            return db[idx];
        }

        return default(StageData);
    }

    public string GetText(uint idx)
    {
        var db = GetDB<TextDataForm>()?.DB;

        if (db.ContainsKey(idx))
        {
            return db[idx].Text;
        }

        return string.Empty;
    }

    public AnimationData GetAnimationData(uint idx)
    {
        var db = GetDB<AnimationDataForm>()?.DB;

        if (db.ContainsKey(idx))
        {
            return db[idx];
        }

        return default(AnimationData);
    }

    public LevelBaseData GetLevelBaseData(uint level)
    {
        var db = GetDB<LevelBaseDataForm>()?.DB;

        if (db != null)
        {
            return db.Where((o) => o.Value.Level == level).Select((s) => s.Value).FirstOrDefault();
        }

        return default(LevelBaseData);
    }

    public RewardData GetRewardData(uint idx)
    {
        var db = GetDB<RewardDataForm>()?.DB;

        if (db.ContainsKey(idx))
        {
            return db[idx];
        }

        return default(RewardData);
    }

    public MapData GetMapData(uint idx)
    {
        var db = GetDB<MapDataForm>()?.DB;

        if(db.ContainsKey(idx))
        {
            return db[idx];
        }

        return default(MapData);
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
        string targetLabel = "Data";

        var locationsHandle = Addressables.LoadResourceLocationsAsync(targetLabel, typeof(ScriptableObject));
        yield return locationsHandle;

        if (locationsHandle.Status == AsyncOperationStatus.Succeeded)
        {
            ResourceManager.Instance.LoadAssetsAsync<ScriptableObject>(locationsHandle.Result, (asset) =>
            {
                if(asset is IDataLoad)
                {
                    IDataLoad dl = (asset as IDataLoad);

                    if (!dataList.ContainsKey(dl.GetType()))
                    { 
                        dl.LoadData();

                        dataList.Add(dl.GetType(), dl);
                    }
                }

                Debug.Log($"Data Loaded: {asset.name}");
            });
        }

        Addressables.Release(locationsHandle);
    }

}
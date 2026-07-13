매니저 상세 문서
=================

이 문서는 ResourceManager, DataTableManager, SaveLoadManager의 책임과 현재 워크스페이스에 구현된 공개 API(메서드)를 정리합니다.

1) ResourceManager
------------------
책임
- Addressables 초기화/카탈로그 업데이트/다운로드
- 에셋 로드(비동기 콜백 + Task 기반), 프리팹 인스턴스화, 핸들 보관 및 해제

주요 공개 멤버 / 메서드 (현재 구현)
- IEnumerator Init(Action onComplete = null)
- IEnumerator IEStartDownload(System.Action onResourceLoad)
- void LoadAssetAsync<T>(string key, System.Action<T> onLoaded) where T : class
- T GetResource<T>(string key) where T : class
- Sprite GetSpriteFromAtlas(string atlasKey, string key)
- Task<T> LoadAssetAsyncTask<T>(string key) where T : class
- void LoadAssetsAsync<T>(IList<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation> locList, System.Action<T> onComp)
- Task<GameObject> InstantiateAsyncTask(string key, UnityEngine.Transform parent = null, UnityEngine.Vector3? position = null, UnityEngine.Quaternion? rotation = null)
- void ReleaseInstance(GameObject go)
- void Release(string key)
- void ReleaseAll()

주의사항 / 권장사항
- LoadAssetAsyncTask/InstantiateAsyncTask는 Addressables 실패 시 예외/null을 반환할 수 있으므로 호출 측에서 null/예외 체크 권장
- ReleaseAll은 현재 보유한 모든 핸들을 해제하므로 씬 전환 또는 애플리케이션 종료 시 호출 권장

사용 예시
```csharp
Sprite bg = await ResourceManager.Instance.LoadAssetAsyncTask<Sprite>(mapdata.BgSprite);
GameObject go = await ResourceManager.Instance.InstantiateAsyncTask(prefabKey, parent);
ResourceManager.Instance.Release("someKey");
```

2) DataTableManager
--------------------
책임
- CSV/TextAsset 기반 데이터 테이블 로드 및 제공
- 다양한 DataTableType에 따른 IDataLoad 구현체 관리

주요 공개 멤버 / 메서드 (현재 구현)
- T GetDB<T>(uint idx) where T : class, IDataLoad
- T GetDB<T>(DataTableType dataTableType) where T : class, IDataLoad
- int GetDataCount<T>(DataTableType dataTableType) where T : class, IDataLoad
- UnitData GetUnitData(uint idx)
- MonsterData GetMonsterData(uint idx)
- StageData GetStageData(uint idx)
- string GetText(uint idx)
- MapData GetMapData(uint idx)
- SkillData GetSkillData(uint idx)
- MonsterPatternData GetMonsterPatternData(uint idx)
- AnimationData GetAnimationData(uint idx)
- LevelBaseData GetLevelBaseData(uint level)

주의사항 / 권장사항
- 데이터는 Addressables의 'Data' 라벨을 통해 로드되며, 각 CSV/TextAsset의 헤더로부터 DataTableType을 판별합니다. 에셋 라벨/형식을 확인하세요.
- GetDB(uint idx) 오버로드는 idx로 데이터 테이블 종류를 유추하므로, 명확하게 타입으로 조회할 수 있으면 GetDB<T>(DataTableType) 사용 권장

사용 예시
```csharp
int mapCount = DataTableManager.Instance.GetDataCount<MapDataForm>(DataTableType.MapData);
MapData m = DataTableManager.Instance.GetMapData(idx);
SkillData sd = DataTableManager.Instance.GetSkillData(skillIdx);
```

3) SaveLoadManager
-------------------
책임
- 로컬 파일에 ClientData 등 저장/로드
- 서버와의 동기화를 위한 GameNetworkManager 호출 래핑 (유저 데이터, 스테이지 클리어 데이터 등)

주요 공개 멤버 / 메서드 (현재 구현)
- ClientData ClientData
- UserData UserData
- StageClearData StageClearData
- UserSkillData UserSkillData
- void SaveClientData()
- void LoadClientData()
- void SaveUserData(System.Action<string> onComp, System.Action<string> onFail)
- void LoadUserData(System.Action onComp, System.Action<string> onFail)
- void SaveStageClearData(int stageIdx, int clearState, System.Action<string> onComp, System.Action<string> onFail)
- void LoadStageClearData(System.Action onComp, System.Action<string> onFail)
- void LoadUserSkillData(System.Action onComp, System.Action<string> onFail)
- void Save<T>(string fileName, T data) where T : class, new()
- T Load<T>(string fileName) where T : class, new()
- bool Find(string fileName)

주의사항 / 권장사항
- 파일 저장 경로는 Application.persistentDataPath를 사용합니다.
- LoadUserData/LoadStageClearData/LoadUserSkillData 등은 GameNetworkManager의 API 호출 콜백을 통해 JSON을 파싱합니다. 네트워크 실패 케이스(onFail 콜백)를 처리하세요.

사용 예시
```csharp
SaveLoadManager.Instance.SaveClientData();
SaveLoadManager.Instance.LoadUserData(() => { /* 완료 */ }, (err) => { /* 실패 */ });
```

추가 제안
- 각 매니저의 public API에 대한 간단한 유닛/플레이모드 테스트 작성 권장
- ResourceManager의 핸들 누수 추적을 위한 로깅/검증 추가 검토

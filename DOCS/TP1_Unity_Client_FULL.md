# TP1 Unity 클라이언트 — 전체 코드/리소스 분석

경로: C:\Users\PC\Documents\GitHub\TP1

요약
- 프로젝트 주요 구성: Addressables 기반 리소스 로딩(ResourceManager), 데이터 테이블(DataTableManager), 풀(Factory+SimplePool), 씬 관리(GameSceneManager), 네트워크(GameNetworkManager), 저장/로드(SaveLoadManager), 전투 시스템(BattleStage, BBoard, BTile, Unit계열).
- 싱글톤 기반 매니저들은 Commons.Singleton<T>를 상속해 Awake에서 DontDestroyOnLoad 처리됨(영속화 가능). 단, serialized field로 씬 오브젝트를 참조하면 씬 전환 시 참조 무효화 가능.

파일 / 클래스별 역할(핵심만 요약)
- Assets/Scripts/Utils/Commons.cs
  - 공용 상수, enum, 데이터 모델, Commons.Singleton<T> 구현( Awake에서 DontDestroyOnLoad ), SimplePool 유틸 포함.
  - 주의: Singleton은 DontDestroyOnLoad로 생존하지만 파생 클래스가 씬별 GameObject를 SerializeField로 참조하면 참조 깨짐 발생 가능.

- Assets/Scripts/Manager/GameNetworkManager.cs
  - 서버 통신(HTTP) 담당. Singleton으로 영속화됨. UnityWebRequest를 코루틴으로 사용.
  - progressCanvas 같은 Canvas 참조를 SerializeField에 가지고 있으므로 로드된 씬에 따라 null이 될 수 있음.

- Assets/Scripts/Manager/ResourceManager.cs
  - Addressables 초기화/다운로드/로딩/인스턴스화/릴리즈를 담당. 핸들들을 보관해 ReleaseAll 가능.
  - OnSingletonDestroyed에서 ReleaseAll 주석 처리되어 있어 앱 종료 시 리소스 누수 포인트가 될 수 있음.

- Assets/Scripts/Manager/GameSceneManager.cs
  - Addressables.LoadSceneAsync(..., LoadSceneMode.Single) 사용. LoadSceneMode.Single는 기존 씬의 (DontDestroyOnLoad 아닌) 모든 객체를 언로드함.
  - 로딩 씬(Commons.SceneName_Loading)으로 먼저 전환 후 실제 씬 로드.

- Assets/Scripts/Manager/DataTableManager.cs
  - CSV(TextAsset)로 데이터 읽어 IDataLoad 파서들에 위임. 에셋 라벨 'Data'로 Addressables에서 읽음.

- Assets/Scripts/Manager/SaveLoadManager.cs
  - 로컬 파일 저장/로드 및 GameNetworkManager와의 연동(로그인/스킬/스테이지 데이터) 담당.

- Assets/Scripts/Utils/Factory.cs
  - 게임 오브젝트/GUI 풀 관리. SimplePool<T>로 Addressables에서 인스턴스 생성 후 재사용.
  - 풀과 씬별 컨테이너(RectTransform, Transform)를 serialized field로 가짐 — 싱글톤화할 경우 씬 전환과 관련된 참조 유지 문제가 발생할 수 있음.

- 전투 관련
  - Assets/Scripts/Content/BattleStage.cs: 전투 루프, 유닛/보드 초기화, 퍼즐 결과 처리, 코루틴 관리. 씬에 배치되는 컴포넌트(씬 전용).
  - Assets/Scripts/Board/BBorad.cs, BTile.cs: 보드 생성, 타일 배치, 경로 탐색(BFS), 타이머, 드로잉 상태 관리.
  - Assets/Scripts/Unit/UnitBase.cs, PlayerUnit.cs, MonsterUnit.cs: 유닛 상태/ATB/애니메이션/데미지 처리.

- UI/유틸
  - HpBar, StageUI, StageMapUI, AlterMsg(알림) 등: Addressables/Factory를 통해 인스턴스화.

리소스/메모리 관련 취약점 포인트 및 권장 조치
1) 싱글톤이 씬 오브젝트(SerializeField) 참조를 가질 때
   - 문제: Singleton<T>는 DontDestroyOnLoad로 생존하지만, serialized로 연결된 씬 내부 오브젝트는 씬 언로드 시 파괴되어 null이 되거나 잘못된 참조를 남깁니다. NullRef 또는 동작 불능으로 이어질 수 있음.
   - 권장: Persistent 매니저는 씬-독립적인 데이터/핸들만 보관하고, 씬에 종속적인 참조(UI 컨테이너, Transform 등)는 씬 로드 이벤트에서 바인딩하거나 매번 Find/Rebind 하세요.

2) Addressables 인스턴스/핸들 관리
   - 문제: ResourceManager에서 로드/인스턴스 핸들을 보관하지만 OnSingletonDestroyed에서 ReleaseAll이 주석처리되어 있음. 앱 종료나 씬 릴리즈 시 릴리즈 누락 가능.
   - 권장: 적절한 시점(예: 애플리케이션 종료, 로그아웃, 메모리 압박 시)에서 ReleaseAll 호출. Addressables API 예외 처리 강화.

3) LoadSceneMode.Single 사용과 DontDestroyOnLoad의 상호작용
   - LoadSceneMode.Single은 현재 씬을 언로드합니다. 영속화를 원하면 DontDestroyOnLoad로 유지해야 하지만, 위에서 말한 '씬 참조 보관' 패턴 때문에 반대로 문제가 생길 수 있음.
   - 권장: 씬 전용 매니저(BattleStage 등)는 씬이 바뀌면 의도적으로 Release/Dispose 하도록 설계. 전역 매니저는 씬 재바인딩 루틴 제공.

4) Pool/Coroutine 정리 누락
   - 문제: BattleStage는 코루틴 리스트(coGC 등)를 관리하고 ReleaseStage에서 정리하지만, 씬 전환 시 ReleaseStage가 호출되지 않으면 코루틴이 남아 메모리/행동이 계속될 수 있음.
   - 권장: 씬 언로드 시점(OnDisable/OnDestroy/SceneManager.sceneUnloaded 이벤트)에서 반드시 ReleaseStage 같은 정리 루틴을 호출하도록 보장.

특이사항 및 빠른 점검 리스트(버그 원인 추적 가이드)
1. BattleManager/전투 관련 객체가 씬 전환 후 사라지는가?
   - 해당 매니저가 Commons.Singleton<T>를 상속하는지 확인. 상속하지 않으면 DontDestroyOnLoad가 적용되지 않아 파괴됨.
   - 상속중이라도 serialized로 다른 씬 오브젝트(예: progressCanvas, uiContainer)를 참조하면 씬 전환 후 Null 발생.

2. 씬 전환 루틴 확인
   - GameSceneManager.LoadScene: Loading 씬 로드 후 Addressables.LoadSceneAsync(..., LoadSceneMode.Single)로 전환. 이때 기존 씬의 씬 전용 오브젝트는 언로드.

3. 풀(Pool)에서 소유/릴리즈 상태 점검
   - Factory.SimplePool가 소유/풀 상태를 추적. 인스턴스 Release 누락, 혹은 서로 다른 풀에서 생성된 객체를 Release하려 할 때 로그 경고 확인.

권장 수정 예시(요약)
- 전역 매니저에서 씬 특정 참조를 제거하고, 씬 로드시점에 재바인딩(rebind)하도록 구현.
- ResourceManager.OnSingletonDestroyed 또는 Application.quitting 이벤트에서 ReleaseAll 호출.
- 씬 언로드 이벤트에서 BattleStage.ReleaseStage 호출 보장.

다음 단계 제안
1. BattleManager(또는 BattleStage)를 씬 전환 시 유지하고 싶다면: 해당 매니저를 Commons.Singleton<T>로 만들고, 내부에서 씬-특정 참조는 null/재설정 가능하게 처리하세요.
2. 원하시면 제가 특정 클래스(현재 BattleStage, Factory, ResourceManager, GameNetworkManager 등)의 변경 패치를 만들어 드리겠습니다. 어느 부분을 우선하길 원하나요?

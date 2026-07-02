# TP1 Unity 클라이언트 — 게임 분석 및 Work-flow

요약
- 장르: 턴 기반 ATB(Active Time Battle) + 퍼즐 보드 조작 요소가 결합된 액션/전략 게임.
- 핵심 플레이: 플레이어와 몬스터가 ATB게이지에 따라 턴을 얻고, 플레이어는 보드 위에서 경로를 그려 공격/스킬/방어를 실행한다.
- 기술 스택: Unity + Addressables, UniRx(리액티브), Newtonsoft.Json, Addressables 기반 풀(Factory + SimplePool).

게임 주요 구성 요소
- 전투(BattleStage, BBoard, BTile)
  - BBoard: 격자 보드에서 시작/종료 지점을 정하고 PerlinNoise 기반으로 타일(공격/방어/스킬/블록 등) 배치.
  - 플레이어는 경로를 그려 엔드포인트에 도달하면 해당 경로에 따른 상태(공격력, 방어, 쉘드 크러시 등)를 획득.
  - 몬스터는 공격 패턴(몬스터 패턴 데이터)에 따라 방어 보드를 생성하고 퍼즐로 대응.

- 유닛(UnitBase, PlayerUnit, MonsterUnit)
  - ATB 시스템: 스피드 기반으로 게이지가 쌓여 턴 발생(Subject로 알림). 턴마다 보드 조작 또는 몬스터의 보드 생성.
  - 애니메이션/데미지 처리 및 HP UI 연동.

- 데이터 및 리소스
  - DataTableManager: CSV(TextAsset)로 구성된 데이터 테이블(유닛, 스테이지, 스킬 등)을 Addressables에서 로드.
  - ResourceManager: Addressables 초기화/다운로드/로드/인스턴스화 및 핸들 관리.
  - Factory + SimplePool: 게임 오브젝트/UI 풀링(플레이어, 몬스터, 보드, 타일, HP UI, 포트레이트, 데미지 폰트 등).

- 씬/매니저
  - GameSceneManager: 로딩 씬 → 목적 씬(Addressables.LoadSceneAsync, LoadSceneMode.Single).
  - SaveLoadManager: 로컬 파일 저장/로드 및 GameNetworkManager와 연계해 서버 동기화.
  - GameNetworkManager: HTTP 기반 REST 호출(로그인/유저데이터/스테이지 기록/스킬 업데이트 등).

게임플레이 흐름(사용자 관점)
1. InitScene: Addressables/리소스 초기화, DataTable 로드, 팩토리/풀 초기화.
2. MainScene: 사용자 인터페이스(스테이지 선택, 프로필 등) 표시 — SaveLoadManager로 로그인 및 서버에서 유저 데이터 로드.
3. StageSelectScene: 스테이지 선택 후 EnterUserStage 요청 및 로컬/서버 상태 업데이트.
4. Battle 씬(전투): BattleStage 초기화 → 플레이어/몬스터 유닛 생성 → 보드 생성 및 퍼즐 플레이 → 전투 루프 종료(클리어/패배).
5. 전투 종료 후 결과 저장(로컬/서버), 보상 처리, 메인/선택 씬으로 복귀.

개발자 Work-flow (개발·디버그 시)
1. 에디터에서 Play로 전체 초기화 테스트.
2. ResourceManager.Init -> Addressables 체크 및 다운로드 확인.
3. DataTableManager가 Data 라벨로 CSV 로드하는지 확인(에셋 라벨 설정 필수).
4. SaveLoadManager(ClientData) 생성 및 GameNetworkManager와의 통신(로그인/스테이지 데이터) 테스트.
5. StageSelect에서 씬 전환 시 GameSceneManager.LoadScene 호출 확인(Loading 씬 → Target 씬).
6. Battle 재현: Factory.PreWarm으로 풀 초기화, BattleStage.InitStage를 통한 보드/유닛 생성 검증.
7. 씬 전환/종료 시 ReleaseStage와 Factory.Release가 호출되어 리소스와 코루틴이 정리되는지 확인.

운영/통합 체크리스트(메모리·씬 전환 문제 방지)
- 전역 매니저(Commons.Singleton<T>)는 DontDestroyOnLoad 적용: 씬 전용 참조(Transform, Canvas 등)는 재바인딩 로직 필요.
- GameSceneManager.LoadScene이 LoadSceneMode.Single을 사용하므로 씬 전용 오브젝트는 언로드됨. 전역 상태는 싱글톤으로 관리하되 UI/컨테이너는 씬 로드 후 다시 할당.
- BattleStage.ReleaseStage가 항상 호출되도록 씬 전환 경로(Back 버튼, SceneManager.sceneUnloaded 이벤트 등)에서 보장.
- Addressables 핸들(instantiate/loadHandles)은 적절히 Release되어야 함(ResourceManager.Release/ReleaseAll 호출 시점 검토).

권장 디버그 절차(씬 전환 후 오브젝트 소실 문제 조사)
1. 씬 전환 직전과 직후에 문제 객체(예: BattleManager,BattleStage 관련)의 존재 여부와 Instance(싱글톤) 상태 로그 출력.
2. 싱글톤이 Awake에서 DontDestroyOnLoad를 호출하는지 확인(Commons.Singleton 구현에 있음).
3. 문제 발생 시: serialized로 연결된 Canvas/Transform이 null이 되는지 확인 — 필요하면 OnSceneLoaded에서 재바인딩.
4. 코루틴/구독(UniRx) 남아있는지 확인: ReleaseStage 미호출로 인한 코루틴 잔류가 있으면 로그로 추적.

결론
- TP1 클라이언트는 'ATB + 퍼즐 보드' 조합의 턴 기반 전투 게임입니다. Addressables와 풀을 적극 사용해 리소스를 동적으로 관리하며, 서버 동기화를 통해 유저/스테이지 데이터를 유지합니다.
- 씬 전환 및 전역 매니저 설계(특히 serialized 씬 참조와 DontDestroyOnLoad 조합)가 메모리/NullRef 문제의 주요 원인입니다. 재바인딩과 Release 루틴 보강을 권장합니다.

파일 저장 경로: DOCS/TP1_Game_Analysis_AND_Workflow.md

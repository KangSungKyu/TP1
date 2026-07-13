프로젝트 개요
===========

루트: TP1 (Unity 프로젝트)

주요 폴더 (Assets)
- Assets/Scripts
  - Scene: InitScene, LoadingScene, MainScene, StageSelectScene 등
  - Manager: GameSceneManager, GameNetworkManager, ResourceManager, DataTableManager, SaveLoadManager, Factory 등
  - UI: StageUI, StageMapUI, HpBar, AlterMsg 등
  - Unit: UnitBase, PlayerUnit, MonsterUnit, UnitCalculator
  - Board: BBoard, BTile
  - Content: GameContent, BattleContent, BattleStage
  - DataForm: MapDataForm, StageDataForm, MonsterDataForm 등 (데이터 로더)
  - Utils: Commons(데이터 모델 정의, Singleton 기반), Factory, CollectionExtensions 등
  - Plugins: UniRx, DOTween, Addressables 관련 플러그인

핵심 책임/패턴
- 싱글톤: Commons.Singleton<T> 기반으로 Manager 계층은 싱글톤 패턴으로 구현되어 있음 (예: GameSceneManager, ResourceManager, SaveLoadManager, DataTableManager)
- 데이터 로드: DataForm 계층과 DataTableManager를 통해 MapData, StageData, MonsterData 등 로드 (Addressables 'Data' 라벨 사용)
- 리소스: Addressables + ResourceManager로 프리팹/스프라이트 로드 및 인스턴스화. ResourceManager는 로드 핸들 관리 및 Release API 제공
- 씬 전환: GameSceneManager.LoadScene(AssetReference)으로 비동기 씬 로드
- 유틸/팩토리: Factory를 통해 UI/유닛/이펙트 등의 생성/재활용 관리

주요 클래스 관계 (요약)
- StageSelectScene
  - 의존: Factory, SaveLoadManager, DataTableManager, ResourceManager, GameSceneManager
  - 역할: Map 데이터만큼 StageMapUI 프리팹을 로드/인스턴스화하고 페이지 방식으로 표시

- ResourceManager
  - 역할: Addressables 초기화/다운로드, 에셋 로드(Task/콜백), Instantiate/Release, SpriteAtlas 요청 처리

- DataTableManager
  - 역할: DataForm 구현체를 통해 테이블형 데이터 제공 (MapData, StageData, SkillData 등)

- SaveLoadManager
  - 역할: 로컬 파일 저장/로드(ClientData 등) 및 GameNetworkManager를 통한 서버 동기화 래핑

- UnitBase <- PlayerUnit, MonsterUnit
  - 역할: 게임플레이 유닛의 공통 기능 정의

- BBoard, BTile
  - 역할: 보드와 타일을 표현, 퍼즐/전투 로직의 물리적 구성

간단한 UML (PlantUML) - 편의상 텍스트로 제공
-----------------------------------
@startuml
class StageSelectScene {
  - int stageMapIdx
  - List<StageMapUI> stageMapUIList
  + Start()
  + LoadMap(int)
}
class ResourceManager
class DataTableManager
class SaveLoadManager
class GameSceneManager
StageSelectScene --> ResourceManager : Load/Instantiate
StageSelectScene --> DataTableManager : GetMapData
StageSelectScene --> SaveLoadManager : UserData
StageSelectScene --> GameSceneManager : LoadScene(AssetReference)
ResourceManager --> Addressables
DataTableManager --> DataForm
SaveLoadManager ..> GameNetworkManager
UnitBase <|-- PlayerUnit
UnitBase <|-- MonsterUnit
@enduml

권장 추가 문서/검토 포인트
- 주요 매니저들(ResourceManager, DataTableManager, SaveLoadManager)의 초기화/파괴(라이프사이클) 및 Addressables 카탈로그/다운로드 흐름을 문서화
- Addressables 사용 방식(동기/비동기 패턴), 리소스 핸들 생명주기, 풀링 전략(Factory) 검토
- 테스트: PlayMode/Editor 테스트(Assets/Tests) 확장 권장

요약
- 프로젝트는 Unity + Addressables + 데이터 테이블 구조를 따름
- Manager 계층(싱글톤)으로 전역 상태/리소스 관리를 수행하며, Scene 스크립트는 이들을 조합해 UI/씬 전환 및 게임플레이를 구현

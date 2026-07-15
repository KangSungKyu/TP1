# TP1 (유니티 게임 프로젝트)

간단 한줄: 퍼즐+ATB 기반 전투 시스템을 구현한 유니티 클라이언트 프로젝트

요약
- 목적: 포트폴리오용으로 전투 시스템(ATB, 퍼즐 보드, 유닛 액션, UI)과 아키텍처/성능 개선 사례를 보여주기 위한 샘플 프로젝트
- 주요 역할: 전투 스테이지 관리, 보드 생성/관리, 유닛 ATB 처리, 퍼즐 결과 처리

데모
- WebGL 또는 로컬 빌드로 플레이 가능 (프로젝트 열어 Play). 짧은 데모 영상/GIF를 상단에 추가 권장.

빠른 시작
1. 권장 Unity 에디터: 2021.3 LTS 이상
2. 프로젝트 폴더 열기: Unity Hub에서 이 폴더 선택
3. Assets/Scenes에서 메인 씬을 열고 Play 버튼으로 실행

빌드
- WebGL: File > Build Settings > WebGL 선택 후 Build
- Windows: Build Settings > PC, Mac & Linux Standalone > Build

프로젝트 구조(주요 폴더)
- Assets/Scripts/Content: 전투 관련 핵심 스크립트 (BattleStage 등)
- Assets/Prefabs: 유닛/이펙트/UI 프리팹
- Assets/Resources 또는 Data: 데이터 테이블, 스크립터블 오브젝트

핵심 파일
- Assets/Scripts/Content/BattleStage.cs — 전투 진행 제어(ATB 루프, 보드 생성/갱신, 퍼즐 결과 처리)
- Factory, UnitBase, PlayerUnit, MonsterUnit — 오브젝트 생성/관리 및 유닛 행동
- BBoard, BTile — 퍼즐 보드와 타일 로직

BattleStage.cs 리뷰(핵심 요약)
- 장점
  - 기능이 잘 모여 있어 전투 흐름(ATB→보드 생성→퍼즐 처리→액션 재생)이 한눈에 들어옴
  - UniRx 사용으로 상태(커서, 카운트) 변경을 선언적으로 처리

- 주의/개선 포인트 (우선순위 높은 항목)
  1) Null/범위 검증: board/cursor 접근 시 범위/널 체크 필요 (예: ReleaseStage, OnMonsterDeath) — 이미 일부 보호 적용 권장
  2) 책임 분리: BattleStage가 너무 많은 책임(유닛 생성·UI·입력·전투 로직)을 가짐 → 리팩터링: 입력 처리, 보드 팝업, 전투 루프를 별도 컴포넌트로 분리
  3) 메모리/GC: Update에서 resultQueue 처리 시 매 프레임 Coroutine을 시작하면 액션 폭증 가능 — 프레임당 처리량 제한 또는 코루틴 풀 활용 권장
  4) 이벤트/구독 정리: Awake에서 구독한 Input/UniRx는 OnDestroy/Release에서 확실히 해제해야 함(Leak 방지)
  5) 안전한 캐스팅: currentBoard.Owner를 직접 캐스트하기보다 as와 null 체크 권장
  6) 성능: LINQ와 람다 사용이 많은 부분은 핫패스(프레임 루프)에서 비용이 될 수 있음 — 프로파일링 후 HotPath 최적화

적용한 작은 수정
- ReleaseStage에서 playerInput null-검사 추가
- OnMonsterDeath에서 board cursor 범위 및 null 검사 추가

권장 다음 작업
1. README에 데모 영상/GIF 추가
2. BattleStage 리팩터 제안서(작업 분해) 작성 및 우선순위 작업 적용
3. WebGL 빌드용 GitHub Actions 워크플로 추가

문의
- 추가로 BattleStage.cs 전체 리팩터(책임 분리, 테스트 추가)를 진행할 수 있습니다. 원하시면 자동으로 변경할 부분 목록과 패치를 바로 생성하겠습니다.

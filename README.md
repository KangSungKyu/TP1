# 🎮 퍼즐 턴제 RPG 프로젝트 (TP1)

> **퍼즐 턴제 RPG 게임** 프로젝트입니다.

---

## 📌 목차
1. [프로젝트 개요](#-프로젝트-개요)
2. [기술 스택 및 개발 환경](#-기술-스택-및-개발-환경)
3. [핵심 시스템 아키텍처](#-핵심-시스템-아키텍처)
4. [주요 리팩토링 & 핵심 기술 구현](#-주요-리팩토링--핵심-기술-구현)
5. [디렉토리 구조](#-디렉토리-구조)

---

## 📖 프로젝트 개요

본 프로젝트는 퍼즐 기믹과 ATB(Active Time Battle) 기반의 턴제 전투 시스템을 결합한 RPG 게임입니다.  
전투 턴 루프, UI 애니메이션, 리소스 다운로드 및 네트워크 통신 등 **게임 전반의 비동기 흐름을 C# async/await (UniTask) 표준으로 일관되게 구조화**하였으며, 메인 스레드 블로킹 방지 및 가비지 컬렉션(GC) 최소화를 최우선으로 설계했습니다.

---

## 🛠 기술 스택 및 개발 환경

- **Engine & Language**: Unity (6.4), C#
- **Asynchronous & Reactive**:
  - **UniTask (Cysharp)**: 프로젝트 전역 비동기 패턴 통일 (Coroutine 100% 마이그레이션 완료)
  - **UniRx**: `ReactiveProperty` 기반 상태 및 UI 반응형 바인딩
- **Asset Management**: Unity Addressables (원격 리소스 다운로드, 핫픽스 대응 및 메모리 해제 보장)
- **Tweening & Animation**: DOTween
- **Input System**: New Unity Input System
- **Architecture**: Singleton Pattern, Service/Factory Pattern, Async Pipeline

---

## ⚡ 핵심 시스템 아키텍처

```
[InitScene] ──(Addressables Init & Catalog Update)──► [ResourceManager]
                                                             │ (UniTask Async Download)
                                                             ▼
[BattleStage] ◄───(ATB Gauge & Queue)───── [Puzzle Board (BBoardManager)]
      │
      ├─► Player/Monster Turn  ──► UniTask-based Turn Loop (`RunUnitTurnAsync`)
      ├─► Puzzle Path Complete ──► Damage & Skill Engine (`ProcPuzzleResultAsync`)
      └─► Battle Result UI    ──► DOTween + UniTask Animation (`StageResultPanel`)
```

### 1. 퍼즐 & ATB 기반 턴제 전투 (BattleStage & BBoardManager)
- 유닛(플레이어/몬스터)별 ATB 게이지가 차오르면 Ready Queue에 엔큐(Enqueue)되어 순차적으로 턴을 소모하는 비동기 전투 루프.
- 퍼즐 판(Offensive/Defensive Board)에서 완성된 노드 드래그 경로에 따라 공격, 기술, 방어 결과 데이터(`PuzzleResult`)를 동적으로 계산.

### 2. 반응형 데이터 바인딩 (UniRx)
- `ReactiveProperty<int>`를 활용하여 생존 플레이어 수 / 몬스터 수를 관측.
- 수치 변경 시 승리/패배 조건 이벤트를 자동 감지하여 타이틀/결과 씬 로직을 깔끔하게 분리.

---

## 💡 주요 리팩토링 & 핵심 기술 구현

### 🚀 1. Unity Coroutine → UniTask 100% 비동기 마이그레이션
- **문제점**: 기존 코루틴 기반 구조는 반환값 전달의 어려움, `yield return new`로 인한 프레임 단위 가비지 컬렉션(GC Alloc) 발생, 씬 전환 시 비동기 예외 처리(Cancellation) 어려움 존재.
- **해결 방안**:
  - 프로젝트 내 모든 코루틴(`IEnumerator`) 구조를 `async UniTask` 및 `CancellationToken` 기반으로 전환.
  - `BattleStage.BattleLoopAsync`, `ResourceManager.InitAsync`, `StageResultPanel.ShowAsync` 등 전역 파이프라인 정립.
  - **결과**: 가비지 할당 소모 감소, 코드 가독성 향상, 씬 파괴 시 안전한 cancellation 지원.

### 📦 2. Addressables & 리소스 해제 관리 (ResourceManager)
- `Addressables.CheckForCatalogUpdates` 및 `DownloadDependenciesAsync`를 UniTask 비동기로 통합.
- 다운로드 진행률(`IProgress<float>`) 수신 파이프라인 구현으로 UI Progress Bar 연동 용이성 제공.
- 리소스 로드/생성 핸들(`AsyncOperationHandle`)을 딕셔너리로 추적하여 `Release` 시 메모리 누수를 방지하는 안전 메커니즘 구축.

### 🎨 3. UI 및 DOTween과 UniTask의 결합 (StageResultPanel)
- UI 패널 활성화(`PanelBase`) 시 `DOTween` 애니메이션과 `UniTask.AsyncWaitForCompletion()`을 조합하여 비동기 트윈 애니메이션 완료 후 UI 조작이 가능하도록 구현.

---

## 📁 디렉토리 구조

```
Assets/Scripts/
├── Board/          # 퍼즐 타일 및 타일 배치, 노드 드래그 관련 로직
├── Content/        # 전투 스테이지(BattleStage) 및 전투 비동기 루프
├── DataForm/       # 게임 내 데이터 구조체 및 API 데이터 모델
├── Effect/         # 히트 이펙트, 폰트 및 연출 시스템
├── Manager/        # 비동기 시스템 매니저 (Resource, Network, DataTable, Board, Scene 등)
├── Scene/          # 초기화(InitScene) 및 각 씬 제어기
├── UI/             # PanelBase 상속 패널 UI, Stage UI, HP Bar 등
├── Unit/           # 플레이어/몬스터 유닛 상속 구조 (UnitBase, PlayerUnit, MonsterUnit)
└── Utils/          # Collection 확장 메서드 및 공통 상수 (Commons)
```

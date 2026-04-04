# TaskManager

콘솔 기반 TUI(Terminal User Interface) 태스크 관리 앱. .NET 8.0 / C# 으로 작성.

## 빌드 및 실행

```bash
cd TaskManager
dotnet build
dotnet run
```

## 기술 스택

- **언어/런타임:** C# / .NET 8.0
- **UI:** 순수 콘솔 TUI (외부 UI 라이브러리 없음)
- **직렬화:** System.Text.Json (단축키 설정 저장)
- **데이터:** CSV (task_registry.csv), JSON (hotkeys.json)

## 프로젝트 구조

```
TaskManager/
├── Program.cs                        # 진입점
├── TaskManager.csproj
├── config/
│   ├── task_registry.csv             # 태스크 데이터
│   └── hotkeys.json                  # 단축키 설정 (자동 생성)
└── src/
    ├── core/navigation/              # 화면 전환 / 이벤트 루프
    │   ├── IView.cs                  # 뷰 인터페이스
    │   ├── IViewNavigator.cs
    │   ├── IViewFactory.cs
    │   ├── ViewId.cs                 # 화면 ID 열거형
    │   ├── ViewNavigator.cs          # 스택 기반 네비게이터 + 입력 루프
    │   └── ViewFactory.cs            # 뷰 팩토리 (의존성 주입)
    ├── model/                        # 도메인 모델
    │   ├── TaskItem.cs
    │   ├── TaskStore.cs              # 인메모리 태스크 컬렉션
    │   ├── TaskEditSession.cs        # 편집 세션 상태
    │   ├── HotKeyAction.cs           # 단축키 액션 열거형
    │   ├── HotKeyGesture.cs          # 키 조합 값 타입 (IEquatable)
    │   └── HotKeyConfig.cs           # 단축키 매핑 + 유효성 검사
    ├── infra/hotkeys/
    │   └── HotKeyConfigStore.cs      # JSON 파일 로드/저장
    └── view/                         # TUI 화면 구현
        ├── MainMenuView.cs
        ├── HotKeyHelper.cs           # 단축키 유틸리티 (정적 클래스)
        ├── hotkeys/
        │   ├── HotKeyMenuView.cs
        │   ├── HotKeyEditView.cs     # 상태 머신 기반 키 재할당
        │   └── KeyInputTestView.cs   # 키 입력 디버그 도구
        └── tasks/
            ├── TaskMenuView.cs
            ├── TaskListView.cs
            └── TaskEditorView.cs
```

## 아키텍처

- **네비게이션:** `ViewNavigator`가 `Stack<IView>`로 화면을 관리. Push/Pop 방식
- **이벤트 루프:** `ViewNavigator.Run()` → 키 입력 → 글로벌 처리 → `IView.HandleKey()` → `InvalidateRequested` 이벤트 → 다시 렌더
- **의존성 주입:** `ViewFactory`가 공유 모델(`HotKeyConfig`, `TaskStore`, `TaskEditSession`)을 생성자 주입으로 전달
- **글로벌 단축키:** `Ctrl+D` = 종료, `Alt+Enter` = 뒤로가기 (뷰에서 처리 안 함)

## 코딩 컨벤션

- 네임스페이스는 디렉토리 구조를 따름 (`src/core/navigation/` → `TaskManager.Core.Navigation`)
- private 필드는 언더스코어 접두사 (`_viewNavigator`, `_hotKeyConfig`)
- null 체크는 `ArgumentNullException.ThrowIfNull()` 사용
- UI는 박스 문자(`┌─┐│└┘▶`) + 콘솔 색상으로 구성
- 메뉴 설명은 영문/한글 병기

## 주요 설계 패턴

| 패턴 | 적용 위치 |
|---|---|
| Factory | `ViewFactory` |
| Navigation Stack | `ViewNavigator` |
| State Machine | `HotKeyEditView` (Browse/Capture/ConfirmDuplicate) |
| Value Object | `HotKeyGesture` |
| Observer | `IView.InvalidateRequested` 이벤트 |

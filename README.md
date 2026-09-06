# 작은 정원 · Merge Board MVP

Unity uGUI로 만든 7열 × 9행 머지 보드입니다. 씨앗을 생성하고 합성해 주문을 완료하면 코인을 받습니다. 보드·코인·주문 완료 상태는 로컬 JSON으로 저장됩니다.

![실제 플레이: 생성, 합성, Get 제출, Animator 피드백](docs/media/merge-board-mvp.gif)

실제 Play Mode에서 uGUI 이벤트로 세 주문을 완료한 기록입니다. GIF는 360×640, 약 27초이며 원본 캡처를 5fps로 묶었습니다.

## 실행 방법

1. Unity Hub에서 이 저장소를 **Unity 6000.3.21f1**로 엽니다. 검증 환경은 Windows입니다.
2. 패키지 해결과 컴파일이 끝날 때까지 기다립니다. 공식 **uGUI 2.0.0**을 사용합니다.
3. [MergeBoard.unity](Assets/MergeBoard/Scenes/MergeBoard.unity)를 엽니다.
4. Play를 누릅니다. 첫 실행은 씨앗 4개, 코인 0, 미완료 주문 3개로 시작합니다.

Game View를 1080×1920 또는 같은 세로 비율로 두면 전체 배치를 편하게 볼 수 있습니다. 다른 창 크기에서는 Canvas가 전체 보드를 화면 안에 맞춥니다. 한국어는 실행 시 OS의 맑은 고딕을 사용하므로 별도 글꼴 파일을 배포하지 않습니다.

Windows 실행 파일은 `File > Build Profiles`에서 Windows를 선택하고 현재 MVP 장면을 포함해 빌드합니다. 이번 작업의 검증용 Development Build는 로컬 `Builds/MergeBoard/MergeBoard.exe`에 있습니다. `Builds/`는 Git에 포함하지 않습니다.

## 조작 방법

| 조작 | 결과 |
| --- | --- |
| `씨앗 만들기` 클릭 | 무작위 빈 칸에 씨앗 하나 생성, 2초 충전 시작 |
| 아이템을 빈 칸으로 드래그 | 아이템 이동 |
| 씨앗 두 개를 겹치기 | 새싹 하나로 합성 |
| 새싹 두 개를 겹치기 | 꽃 하나로 합성 |
| 다른 단계·꽃끼리·보드 밖에 드롭 | 상태를 바꾸지 않고 원위치로 복귀, 안내 표시 |
| 활성화된 주문 `Get` 클릭 | 필요한 수량을 보드에서 함께 소비하고 보상 지급 |

주문은 새싹 1개 → 20코인, 꽃 1개 → 60코인, 꽃 2개 → 150코인입니다. 세 주문을 모두 완료하면 총 230코인입니다. 주문은 교체되지 않으며 완료한 주문의 버튼은 비활성화됩니다. 필요한 수량이 부족할 때도 `Get`은 비활성화됩니다.

클릭·합성·주문 제출·보상 피드백은 **Animator**로 재생합니다. 주문 제출 시 소비한 아이템의 표시가 보드에서 해당 슬롯으로 날아갑니다. 상태 변경과 저장은 애니메이션 시작 전에 끝나므로 빠른 재클릭이나 실행 종료로 보상이 중복되지 않습니다.

## 구현 구조와 UI 편집

| 파일·영역 | 책임 |
| --- | --- |
| [BoardModel / GameState / OrderModel / SaveData](Assets/MergeBoard/Scripts/Model) | 보드, 아이템 단계, 고정 주문, 코인, 저장 포맷 |
| [MergeGameController](Assets/MergeBoard/Scripts/MergeGameController.cs) | 이동·합성·생성·Get 제출·보상·상태 변경 직후 저장 요청 |
| [BoardView / CellView / OrderView / HUDView](Assets/MergeBoard/Scripts/View) | uGUI 표시와 입력 전달 |
| [LocalSaveService](Assets/MergeBoard/Scripts/LocalSaveService.cs) | JSON 검증·읽기·임시 파일 기록 후 교체 |
| [AnimatorFeedback](Assets/MergeBoard/Scripts/View/AnimatorFeedback.cs) | Animator 진행도를 동적 비행 목적지에 적용하고 임시 표시 정리 |
| [MergeGameBootstrap](Assets/MergeBoard/Scripts/MergeGameBootstrap.cs) | 시작 시 복원, 장면 UI와 Controller 연결 |

장면의 `머지 게임 > Canvas > 작은 정원` 아래에서 RectTransform과 Button을 편집할 수 있습니다. 상단 HUD, 보드 위 가로 주문 슬롯, 중앙·하단 보드 배치는 사용자가 제작한 `pkmerge-client`의 Game 씬을 참고했습니다. 해당 프로젝트의 코드·구조·아트는 가져오지 않았습니다.

[AnimationClip 폴더](Assets/MergeBoard/Animations)에서 `Click`, `Merge`, `Flight`, `Reward`를, [AnimatorController](Assets/MergeBoard/Resources/MergeFeedback.controller)에서 상태 연결을 편집합니다. 비행·보상 클립은 `AnimatorFeedback.progress`를 0에서 1로 변경합니다.

## 저장과 복원

저장 파일은 `Application.persistentDataPath/merge-board-v1.json`입니다. Windows에서는 Unity Player Settings의 회사명·제품명에 따라 `%USERPROFILE%/AppData/LocalLow/<회사명>/<제품명>/` 아래에 생성됩니다.

- 생성·이동·합성·주문 완료 성공 직후 저장하고 시작할 때 한 번 복원합니다.
- 저장 대상은 63칸의 아이템 단계, 코인, 고정 주문 ID와 완료 여부입니다.
- 드래그 표시, 애니메이션, 생성기 쿨다운은 저장하지 않습니다.
- 파일 누락·손상·미지원 버전·잘못된 데이터는 기본 상태로 시작합니다.
- 쓰기 실패는 화면에 안내하며 현재 플레이 상태와 기존 저장 파일을 유지합니다.

## 검증 결과

2026-09-06, Unity 6000.3.21f1 / Windows 기준입니다.

| 검증 | 결과 |
| --- | --- |
| unity-verify: Editor 연결·컴파일·Console | 성공, Error 0건·관련 Warning 0건 |
| 규칙 검증 | 63칸·초기 배치·좌표 경계·이동·2단계 합성·최종 단계 거절 PASS |
| 생성기·주문 검증 | 2초 경계·빈/가득 찬 보드·수량 부족·정확한 소비·중복 보상 방지 PASS |
| Play Mode 전체 루프 | 실제 uGUI 이벤트와 시간 경과로 세 주문 완료·230코인 PASS |
| Animator 검증 | 클릭·합성 상태 재생, 비행·보상 진행도, 효과 정리 PASS |
| 저장 실패 경로 | 변경 직후 파일 일치, 손상·버전·칸·단계·주문·코인 검증, 쓰기 실패 시 기존 파일 보존 PASS |
| Play Mode 종료·재진입 | 보드·코인·주문 상태 일치 PASS |
| Windows Development Build | 빌드 성공, Error 0건·Warning 0건 |
| Windows 실행 파일 두 번 실행 | 격리 파일의 씨앗 위치·230코인·주문 3개 완료 상태 일치 PASS |
| mvp-review | 최종 Critical 없음·Warning 없음 |

Windows 실행 파일의 재시작 검증은 `-batchmode -nographics`로 수행했습니다. 실제 UI와 애니메이션의 시각 확인 및 GIF 캡처는 Editor Play Mode에서 수행했습니다. 모바일·다른 OS는 검증 범위에서 제외했습니다.

## 검증 재현

프로젝트가 열린 상태에서 저장소 루트의 PowerShell로 실행합니다. `unity-cli`와 저장소의 Connector 연결이 필요합니다.

```powershell
unity-cli status
unity-cli editor refresh --compile
unity-cli console --filter all --stacktrace short
unity-cli exec 'MergeBoard.Editor.MergeBoardVerification.VerifyRules(); return true;'
unity-cli exec 'MergeBoard.Editor.SaveVerification.VerifySave(); return true;'
```

전체 플레이 검증은 개인 저장과 분리된 초기 상태로 시작합니다. 명령은 한 줄씩 실행하고 Editor 전환이 끝난 뒤 다음 줄을 실행합니다.

```powershell
unity-cli exec 'MergeBoard.Editor.SaveVerification.PrepareIsolatedPlay(); return true;'
unity-cli editor play --wait
unity-cli exec 'MergeBoard.Editor.PlayLoopVerification.Begin(); return true;'
unity-cli exec 'return MergeBoard.Editor.PlayLoopVerification.Status;'
```

약 30초 후 상태가 `PASS`가 되면 아래 순서로 복원까지 확인합니다. 캡처는 `Logs/PlayCapture/`에 남습니다.

```powershell
unity-cli editor stop
unity-cli editor play --wait
unity-cli exec 'MergeBoard.Editor.SaveVerification.VerifyPlayReload(); return true;'
unity-cli editor stop
unity-cli exec 'MergeBoard.Editor.SaveVerification.EndIsolatedPlay(); return true;'
```

개발 빌드의 독립 프로세스 검증은 [verify-standalone.ps1](docs/tools/verify-standalone.ps1)에 실행 파일과 격리 저장 파일을 전달합니다. 일반 빌드에는 검증 경로 재정의와 복원 상태 로그가 포함되지 않습니다.

```powershell
.\docs\tools\verify-standalone.ps1 -PlayerPath .\Builds\MergeBoard\MergeBoard.exe -SavePath <격리된-progress.json-경로>
.\docs\tools\encode-preview.ps1 -FrameDirectory <캡처-폴더> -OutputPath .\docs\media\merge-board-mvp.gif
```

## 범위와 작업 기록

기획 기준은 [MVP_SPEC.md](MVP_SPEC.md), 구현 설계는 [설계 문서](docs/design/merge-board-mvp-design.md)입니다. 변수·함수 이름은 영어, 설명과 필요한 XML summary는 한국어로 작성했습니다.

서버·로그인·결제·광고·상점·인벤토리·튜토리얼·이벤트·추가 아이템·주문 무한 갱신·밸런싱·고품질 아트·사운드는 의도적으로 제외했습니다.

| 커밋 | 내용 |
| --- | --- |
| `b3192e9` | 설계 문서 단독 추가 |
| `ac1a255` | Get 제출 방식과 Animator 설계 확정 |
| `c91f1a4` | 7×9 보드와 초기 아이템 모델 |
| `48bb3f5` | 드래그 이동·합성, 영어 식별자·summary |
| `1e4fb86` | uGUI 전환, 생성기·주문·보상 |
| `0c3181c` | 로컬 저장·복원과 실패 검증 |
| `899c158` | Animator 피드백과 전체 플레이·빌드 검증 |

모든 Unity 에셋은 대응하는 `.meta`와 함께 관리합니다. `Library/`, `Logs/`, `Temp/`, `Obj/`, `UserSettings/`, `Builds/`는 커밋하지 않습니다.

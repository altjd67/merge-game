---
name: unity-verify
description: Unity 프로젝트의 C# 컴파일과 Editor Console 오류를 점검한다. C# 또는 Unity 에셋 변경 후 검증할 때 사용한다.
---

# Unity Verify

Unity Editor에 연결해 컴파일 결과와 Console을 확인한다. 이 스킬은 문제를 자동으로 수정하거나 커밋하지 않는다.

## 사전 조건

- 대상 Unity 프로젝트가 열려 있어야 한다.
- Unity CLI Connector 패키지가 설치되어 있어야 한다.
- `unity-cli status`가 대상 에디터를 인식하지 못하면, 실행 결과를 보고하고 중단한다. 에디터 종료·재시작은 사용자가 직접 수행한다.

## 검증 절차

1. `unity-cli status`로 연결 대상과 에디터 상태를 확인한다.
2. C# 변경이 있으면 `unity-cli editor refresh --compile`로 컴파일을 요청한다.
3. `unity-cli console --filter all --stacktrace short`로 Console을 확인한다.
4. Error는 파일·줄·스택을 근거로 보고한다. Warning은 이번 변경과 관련 있거나 동작에 영향을 줄 때만 보고한다.
5. 컴파일 또는 Console Error가 있으면 수정 전까지 검증 실패로 판정한다. 기존 오류인지 이번 변경으로 추가된 오류인지 구분할 근거가 없으면 불확실성을 명시한다.

## 결과 형식

```text
## Unity Verify

- 에디터 연결: 성공 | 실패
- 컴파일: 성공 | 실패 | 미실행
- Console Error: N건
- 관련 Warning: N건

### 발견 사항
- [심각도] 파일:줄 또는 Console 메시지
  - 영향: ...
  - 근거: ...

### 확인 범위
- ...
```

오류가 없으면 `발견 사항: 없음`을 명시한다. 연결이 실패한 경우에는 컴파일과 Console을 성공으로 간주하지 않는다.

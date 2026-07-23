# Learning: Unity PlayMode 스크린샷 증적 유효성

## Context

`-batchmode -nographics` 캡처 테스트가 통과하고 PNG도 생성됐지만, 실제 프레임은 균일한 회색이라 레이아웃 증적으로 사용할 수 없었다.

## Reusable insight

스크린샷 생성 성공과 시각 증적 유효성을 분리한다. 캡처 증적은 graphics-enabled batch run, 결과 XML, 파일·해상도·픽셀 변화 확인, 직접 시각 검사까지 통과해야 하며, 패널 간격과 타이포그래피는 별도의 결정적 PlayMode 어설션으로 보증한다.

## Evidence

- `.flow/tasks/저장-ux-보강/qa.md`
- `unity/Assets/Tests/PlayMode/SampleSceneP0PlayModeTests.cs`
- [재사용 가능한 상세 지침](../../../docs/solutions/best-practices/unity-playmode-screenshot-evidence-validity.md)

## Applies when

- Unity batch-mode PlayMode 테스트가 렌더된 PNG를 QA·리뷰·종료 증적으로 생성할 때.

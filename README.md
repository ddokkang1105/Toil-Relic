# Toil-Relic

C# 콘솔 기반 1인 턴제 사냥 게임 초안.
- 노역(사냥)으로 `잡템`과 `보물 재료`를 얻는다.
- 재료를 모아 `보물`을 제작한다.

## 실행(예시)
```bash
# .NET SDK 설치 후
cd src/ToilRelic

dotnet run
```

## 게임 목표
- 사냥을 반복하며 보물 재료와 잡템을 모은다.
- 제작을 통해 보물을 완성하고 점수를 올린다.

## 폴더 구조
```
Toil-Relic/
  README.md
  unity/
    UNITY_SETUP.md
    Assets/
      Scripts/
  src/
    ToilRelic/
      ToilRelic.csproj
      Program.cs
      Game.cs
      Models/
      Systems/
      Util/
```

## Unity 전환 초안
- Unity용 스크립트 초안은 `unity/Assets/Scripts` 에 있습니다.
- 설정 방법은 `unity/UNITY_SETUP.md` 를 참고하세요.

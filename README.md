# 멀티 던전 (가제)

2인 협동 턴제 던전 크롤러. 플레이어당 영웅 4명, 4열 대열 전투, 지하왕국 탐험.

- 엔진: Unity
- 컨셉 문서: https://claude.ai/artifact/WQYZUHKCQu31rPJ7XuzbG6

Unity 프로젝트는 Unity Hub에서 이 폴더를 열어 생성합니다(Assets, Packages, ProjectSettings 커밋).

## 시작하기

1. Unity Hub에서 이 폴더를 추가해 엽니다(Unity 6 LTS, 다른 6000.x 버전도 가능).
2. 처음 열면 `Assets/Scenes/Title.unity`가 자동으로 만들어지고 열립니다. 다시 만들려면 메뉴 `멀티던전 > 씬 다시 만들기`.
3. Title 씬에서 Play → 게임 시작 → 원정 선택 지도. 지역 아이콘이나 원정 표식(색 = 난이도, 글자 = 길이)을 눌러 원정을 고릅니다.

## 언어 (한국어 / 영어)

모든 화면 글자는 `Assets/Resources/Localization/strings.csv`(열: key, ko, en)에서 관리합니다. 씬의 글자에는 `LocalizedText` 컴포넌트가 붙어 있고, Key를 바꾸면 다른 문장이 나옵니다. 언어를 추가하려면 CSV에 열을 하나 더하세요. 게임에서는 설정 > 언어로 바꿉니다.

## 씬과 UI

Title, WorldMap 씬의 버튼과 패널은 씬에 실제 오브젝트로 배치되어 있어 Hierarchy에서 옮기거나 고칠 수 있습니다. 버튼 동작은 각 버튼 인스펙터의 OnClick에 연결되어 있습니다. 원정 표식과 원정 목록처럼 원정마다 바뀌는 부분만 실행 중에 채웁니다.

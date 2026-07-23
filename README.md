# Presenter — 발표 보조 플로팅 마스코트 앱

## 1. 소개

**Presenter**는 화면 공유(WebEx/Teams 등)나 빔프로젝터로 발표할 때, 항상 위에 떠 있는
작은 마스코트 캐릭터 하나로 레이저 포인터·화면 그리기·발표 시간 타이머를 바로 쓸 수 있게 해주는
Windows용 플로팅 유틸리티입니다.

- **마스코트**: 오랑우탄을 모티브로 한 자체 창작 픽셀아트 캐릭터. 드래그로 화면 어디든 옮길 수 있고, 클릭하면 기능 메뉴가 열립니다.
- **프레젠테이션 모드**: 마우스를 따라다니는 레이저 포인터. 색상·크기를 메뉴에서 바로 골라 쓸 수 있습니다.
- **그리기 모드**: 자유곡선·직선·정사각형·직사각형·원형 도구로 화면 위에 직접 그립니다. 색상·두께도 선택 가능합니다.
- **타이머**: 발표 시간을 분 단위로 설정해 카운트다운하고, 창 투명도를 조절할 수 있으며, 종료 임박 경고와 시간 종료 알림을 화면에 띄웁니다.
- **현재 시간 표시**: 마스코트 머리 위에 24시간제 시계(HH:mm:ss)를 켜고 끌 수 있습니다.
- 레이저 포인터·그리기·타이머 모두 **듀얼 모니터** 환경을 지원합니다(타이머 관련 알림은 타이머 창이 있는 모니터 기준으로 표시).
- **숨기기**: 마스코트만 화면에서 감추고 트레이 아이콘(마스코트 얼굴 모양)은 그대로 남겨둡니다. 트레이 아이콘 더블클릭 또는 우클릭 → "보이기"로 다시 꺼낼 수 있습니다.
- **종료 확인**: 마스코트 메뉴/트레이 메뉴의 "종료"를 누르면 한 번 더 확인한 뒤에만 프로세스까지 완전히 종료합니다.

자세한 기획 배경과 로드맵은 [`Presenter_PRD_v0.2.md`](Presenter_PRD_v0.2.md)를 참고하세요.

## 2. 소스 코드

이 저장소에는 Presenter의 **전체 소스 코드가 그대로 공개되어 있습니다.** 별도로 감춰진 빌드
스크립트나 서버 구성 요소 없이, 이 폴더 안의 코드만으로 동일한 결과물을 빌드할 수 있습니다.

주요 파일 구성:

| 파일 | 역할 |
|---|---|
| `MascotWindow.xaml(.cs)` | 마스코트 창, 드래그/클릭 처리, 기능 메뉴 |
| `MascotFace.cs` | 마스코트 픽셀아트 렌더링, 유휴 애니메이션 |
| `OverlayWindow.xaml(.cs)` | 레이저 포인터·그리기·타이머 알림이 표시되는 전체 화면 투명 오버레이 |
| `TimerWindow.xaml(.cs)` | 발표 타이머 창 (분 설정, 카운트다운, 투명도 슬라이더) |
| `HelpWindow.xaml(.cs)` | 앱 내 "사용법" 안내 창 |
| `App.xaml(.cs)` | 앱 진입점, 시스템 트레이 아이콘(보이기/숨기기/종료 확인) |
| `TrayIconFactory.cs` | 마스코트 얼굴을 오프스크린 렌더링해 트레이 아이콘/앱 아이콘(`Assets/AppIcon.ico`)으로 변환 |
| `Assets/AppIcon.ico` | 마스코트 얼굴 기반 앱 아이콘(다중 해상도) — exe/시작 메뉴/바탕화면 바로 가기에 사용 |
| `NativeMethods.cs` | 오버레이 클릭 통과(Click-through) 처리를 위한 최소한의 Win32 P/Invoke |
| `installer/Presenter.wxs` | 설치형 배포 파일(PresenterSetup.msi)을 만드는 WiX Toolset 스크립트 |

코드를 자유롭게 읽고, 수정하고, 다시 빌드해서 써도 됩니다.

## 3. 설치형 배포 파일(PresenterSetup.msi) 사용법

`installer/Output/PresenterSetup.msi`는 별도의 .NET 런타임 설치 없이 바로 쓸 수 있는
**MSI 설치 패키지**입니다(만드는 방법은 4-3절 참고).

1. `PresenterSetup.msi`를 더블클릭해서 설치를 시작합니다 (관리자 권한 불필요 — 현재 사용자 계정에만 설치됩니다).
2. **"Windows의 PC 보호" 경고가 뜰 수 있습니다.** 아직 코드 서명이 되어 있지 않아 Windows
   SmartScreen이 "알 수 없는 게시자" 경고를 띄우는 것이며, 악성코드가 아닙니다.
   경고 창에서 **"추가 정보" → "실행"**을 눌러 계속 진행하면 됩니다.
3. 설치 마법사(다음 → 설치 → 완료)를 그대로 따라가면 됩니다. 별도 옵션 선택 없이
   Windows 시작 시 자동 실행, 시작 메뉴/바탕화면 바로 가기가 함께 설치됩니다. 마지막 완료
   화면의 **"Presenter 실행" 체크박스**가 켜진 채로 "마침"을 누르면 설치 직후 바로 실행됩니다.
4. 설치가 끝나면 `%LocalAppData%\Presenter\Presenter.exe`에 설치되고, 시작 메뉴 바로 가기와
   시스템 트레이에 마스코트 얼굴 모양의 작은 아이콘이 생깁니다.
5. 마스코트를 클릭해 메뉴를 열고 원하는 기능(프레젠테이션/그리기/타이머 등)을 선택하면 됩니다.
   자세한 사용법은 메뉴의 **"사용법"** 항목에서도 확인할 수 있습니다.
6. **숨기기**: 마스코트 메뉴의 "숨기기"를 누르면 화면에서만 사라지고, 트레이 아이콘은 그대로
   남습니다. 트레이 아이콘을 더블클릭하거나 우클릭 → "보이기"로 다시 꺼낼 수 있습니다.
7. **종료**: 마스코트 메뉴 또는 트레이 아이콘 우클릭의 "종료"를 누르면 한 번 더 종료 여부를
   확인한 뒤 프로세스까지 완전히 꺼집니다.
8. **제거**: Windows 설정 → 앱 → 설치된 앱에서 "Presenter"를 찾아 제거하거나,
   `PresenterSetup.msi`를 다시 더블클릭해 제거를 선택하면 됩니다.

> 자동 업데이트 기능은 없으므로, 새 버전이 나오면 `PresenterSetup.msi`를 다시 받아 재설치해야 합니다.

## 4. 소스에서 직접 빌드하기 (개발자용)

### 요구 사항

- Windows 10/11 (WPF는 Windows 전용)
- Visual Studio 2022 (17.8+) — 워크로드: **.NET desktop development**
- .NET 8 SDK (Visual Studio 설치 시 함께 설치됨)

### 빌드 및 실행

1. 이 폴더를 통째로 압축 해제
2. Visual Studio에서 `Presenter.csproj` 더블클릭 (또는 "폴더 열기")
3. 상단 실행 버튼(▶ Presenter) 클릭 → 빌드 후 자동 실행

또는 커맨드라인(Windows, .NET 8 SDK 설치된 환경):

```powershell
cd Presenter
dotnet run
```

### 배포용(Self-contained) 실행 파일 만들기

```powershell
dotnet publish Presenter.csproj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true -o publish\win-x64
```

`.NET 8 런타임 설치 여부와 상관없이 바로 실행 가능한 단일 `Presenter.exe`가
`publish/win-x64/`에 생성됩니다.

### 설치 파일(PresenterSetup.msi) 만들기

1. [WiX Toolset](https://wixtoolset.org/) v5를 .NET 도구로 설치합니다.

```powershell
dotnet tool install --global wix --version 5.0.2
wix extension add WixToolset.UI.wixext/5.0.2
wix extension add WixToolset.Util.wixext/5.0.2
```

2. 위의 `dotnet publish` 명령으로 `publish/win-x64/Presenter.exe`를 먼저 만듭니다.
3. `installer/Presenter.wxs`를 빌드합니다.

```powershell
cd installer
wix build Presenter.wxs -ext WixToolset.UI.wixext -ext WixToolset.Util.wixext -arch x64 -o Output\PresenterSetup.msi
```

결과물은 `installer/Output/PresenterSetup.msi`에 생성됩니다. 관리자 권한 없이 현재 사용자
계정에만 설치되도록 구성되어 있고(`Scope="perUser"`, 설치 위치 `%LocalAppData%\Presenter`),
"Windows 시작 시 자동 실행" 레지스트리 등록과 시작 메뉴/바탕화면 바로 가기가 모두 포함되며
제거 시 함께 삭제됩니다.

> WiX v7부터는 유료 후원(Open Source Maintenance Fee) 관련 EULA 동의가 CLI에 강제되어
> v5를 사용합니다. v5는 이 제약 없이 동일한 문법으로 빌드할 수 있습니다.

## 5. 알려진 제약

- 텍스트 도구 없음
- RGB 컬러피커 없음 (프리셋 색상만 선택 가능)
- 화면 캡처(영역/스크롤) 없음
- 코드 서명 미적용 (3번 항목의 SmartScreen 경고 참고)
- 모니터별 디스플레이 배율(DPI)이 서로 다른 멀티 모니터 환경은 검증되지 않음 (동일 배율 환경 기준으로 동작 확인)
- 자동 업데이트 기능 없음 (새 버전은 설치 파일을 다시 받아 재설치)

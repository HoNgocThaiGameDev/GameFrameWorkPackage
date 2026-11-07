# Cấu trúc dự án

Framework được quản lý trực tiếp tại `Assets/NebulaSoft Core`, cập nhật qua Git tag và changelog. Các thay đổi thư mục ngày 2026-11-07 giữ GUID, namespace, tên class, save key và phiên bản dữ liệu. Không cần migration save chỉ vì thay đổi vị trí file.

## Quyền sở hữu

| Thư mục | Trách nhiệm |
| --- | --- |
| `Assets/NebulaSoft Core/Foundation/Runtime` | Contract nhỏ dùng chung như clock, callback, static unload attribute |
| `Assets/NebulaSoft Core/Modules/<Module>` | Dịch vụ tổng quát như Save, Currency, UI, Audio, Tween, Pool |
| `Assets/NebulaSoft Core/Features/<Feature>/Runtime` | Logic feature tùy chọn như Quest, DailyReward, Lives |
| `Assets/NebulaSoft Core/Integration` | InitModule và công cụ cần phối hợp nhiều assembly |
| `Assets/NebulaSoft Core/Utilities/Runtime`, `Editor` | Helper runtime và helper editor |
| `Assets/NebulaSoft Core/Integrations/Firebase/Runtime` | SDK adapter dùng chung, assembly NebulaSoft.Firebase |
| `Assets/NebulaSoft Core/Shared` | Prefab, animation, hình ảnh thực sự thuộc framework |
| `Assets/NebulaSoft Core/Editor` | Công cụ dùng chung cho framework và chuẩn bị dự án |
| `Assets/NebulaSoft Core/Examples` | Ví dụ nhỏ minh họa API core |
| `Assets/Project Files/Data` | Database, catalog và settings riêng của game |
| `Assets/Project Files/Game/Scripts` | Bootstrap, scene controller, contract session và level của game |
| `Assets/Project Files/Game/Features/<Feature>` | Script, prefab, art và adapter tiến độ của một feature trong game |
| `Assets/Project Files/Game/UI` | HUD, thành phần UI, popup, font và art dùng chung giữa các feature của game |
| `Assets/Project Files/Game/Integration` | Firebase và Ads adapter riêng của game |
| `Assets/Tests/Editor` | Regression test và test editor có chủ động vào Play Mode |
| `tools/ModuleAudit`, `tools/PlayerCompileProbe` | Audit source và kiểm tra contract bằng harness độc lập |
| `tools/Documentation` | Source generator tài liệu |
| `docs`, `output` | Source hướng dẫn và tài liệu/report đã xuất |

`Addon/ThirdParty`, `Plugins`, `GoogleMobileAds`, `ExternalDependencyManager`, TextMesh Pro và các thư mục SDK tiếp tục theo cấu trúc vendor. Settings mà SDK tự tìm trong `Assets/Resources` giữ nguyên vị trí.

## Thêm module hoặc feature

Đặt tên type theo trách nhiệm và vị trí sở hữu trong [quy ước đặt tên class và file](modules/naming.md).

Module mới đặt code chạy trong `Modules/<Module>/Runtime`, công cụ trong `Editor`, prefab trong `Prefabs`, và Resources chỉ khi cần tải bằng resource key. Chỉ tạo thư mục khi có nội dung. Module lớn có thể thêm `Contracts`, `Configuration`, `Providers`, `Unity` theo ranh giới dependency như Ads và Analytics hiện có.

Với một feature như Quest, logic tái sử dụng thuộc `NebulaSoft Core/Features/Quest/Runtime`. UI và adapter nối tiến độ gameplay thuộc `Project Files/Game/Features/Quest/Scripts`; prefab và art nằm cạnh đó. Quest database của game tiếp tục ở `Project Files/Data/Quest`.

UI dùng chung trong một game thuộc `Game/UI`. Chỉ đưa vào core khi nó không cần dữ liệu, tên scene hoặc controller riêng của game. Core prefab không tham chiếu asset của `Project Files`.

## Assembly

Cloud và Analytics runtime sở hữu contract độc lập với Unity và Firebase. Assembly `NebulaSoft.Firebase` chứa lời gọi SDK, define và factory tạo provider. [Kiến trúc và nâng SDK](firebase-integration.md).

Giữ nguyên tên và GUID các asmdef hiện có. Save, Pool, Foundation, Ads, Analytics runtime cùng các assembly helper hiện có tiếp tục giữ ranh giới dependency. Tween bổ sung `NebulaSoft.Tween.Runtime` và `NebulaSoft.Tween.Editor`; `TweenInitModule` nằm tại `Integration/Tween/Runtime` để runtime không phụ thuộc bootstrap.

Một số module và feature vẫn trong `Assembly-CSharp`. Thư mục `Runtime` thể hiện trách nhiệm; chỉ asmdef mới tạo ranh giới biên dịch. Không đặt asmdef chung lên cả core vì có các nhóm dependency hiện tại cần tách trước, gồm SDK adapter, initializer và reward có managed reference lưu tên assembly. Analytics giữ nhóm `Legacy` để phân biệt API cũ với runtime service đã tách assembly.

Khi thêm asmdef, kiểm tra dependency trước, tách `Editor` bằng assembly chỉ dành cho Editor, và kiểm tra tương thích serialized managed reference. Không đổi assembly của model save hoặc reward chỉ để khớp hình thức thư mục.

## Bản đồ thay đổi

| Vị trí cũ | Vị trí hiện tại |
| --- | --- |
| `Assets/Framework/WhiteSprite.*` | `Assets/NebulaSoft Core/Shared/Images/Primitives/WhiteSprite.*` |
| `Assets/Resources/UI/btn_close.png` | `Assets/NebulaSoft Core/Modules/Monetization/Resources/UI/btn_close.png` |
| `NebulaSoft Core/Core Resources` | `NebulaSoft Core/Shared` |
| `NebulaSoft Core/Utils & Extensions` | `NebulaSoft Core/Utilities` |
| `NebulaSoft Core/Modules/<Module>/Scripts` | `Runtime` và `Editor` trong chính module |
| `NebulaSoft Core/Scripts` | Module tương ứng, `Editor`, hoặc `Core.cs` tại root core |
| `Assets/Addon/UI` | `Project Files/Game/Features` và `Project Files/Game/UI` |
| `Game/Scripts/Quest`, `Daily Reward`, `Lives System` | `Game/Features/Quest`, `DailyReward`, `Lives` |
| `Game/Scripts/Firebase`, `Ads` | `Game/Integration/Firebase/Runtime`, `Ads/Runtime` |
| `Game/Scripts/UI`, `Game/Prefabs/UI` | `Game/UI/Scripts`, `Game/UI/Prefabs` |
| `docs/tools`, `tmp/build_framework_docx.py` | `tools/Documentation` |

Resource key `UI/btn_close` giữ nguyên. Các asset được di chuyển cùng metadata, không tạo bản sao với GUID mới. Các báo cáo audit lịch sử và PDF/DOCX đã xuất có thể chứa đường dẫn trước thay đổi; hướng dẫn Markdown hiện tại là nguồn để cập nhật tài liệu.

## Kiểm tra sau khi đổi thư mục

Đối chiếu GUID trước/sau và hash nội dung prefab/scene, kiểm tra dependency của core, refresh/compile script trong Editor và chạy regression test. Không dùng đổi thư mục làm lý do ghi lại settings, scene, prefab hoặc save của người chơi. Player build và kiểm thử thiết bị do chủ dự án thực hiện.

Kết quả ngày 2026-11-07 trên Unity 6000.6.8f1

| Kiểm tra | Kết quả |
| --- | --- |
| Asset di chuyển | 621 file giữ GUID; không có GUID trùng trong Assets |
| Nội dung asset | Prefab, scene và settings có trước đợt sắp xếp giữ hash nội dung |
| Dependency core | 608 asset core được quét; không tham chiếu asset của game mẫu |
| Prefab core và game | 71 prefab tải được, không có missing script |
| Resource của dummy ads | `UI/btn_close` tiếp tục tải sprite từ module Monetization |
| Assembly | Tween runtime/editor, Save runtime và Pool runtime biên dịch; không chứa source game hay tham chiếu assembly mặc định |
| Regression test | 201/201 EditMode test đạt, không skip |
| Harness độc lập | 10/10 audit assertion đạt |

Test IAP đã mở rộng quét toàn bộ `Project Files/Game` để tiếp tục kiểm tra các reward holder sau khi prefab store chuyển sang `Game/Features/Store`. Không chạy player build trong đợt này.

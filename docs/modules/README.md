# Sổ tay triển khai module

Tài liệu cho người triển khai game mới trên GameFrameWorkPackage. Mỗi mục chỉ ra module sở hữu gì, cần gắn asset hoặc component nào, cách gọi từ game và ca cần kiểm tra. Nội dung đối chiếu với workspace Unity 6000.6.8f1 ngày 2026-11-07.

[Quy ước đặt tên class và file](naming.md) phân biệt tên dùng chung của core, tên adapter và tên thuộc gameplay của từng title.

## Tìm module

| Nhóm | Module và phần mở rộng |
| --- | --- |
| Khởi tạo và cấu hình dự án | [Initializer](startup.md#initializer), [CoreSettings](startup.md#coresettings), [Defines](startup.md#defines), [Scenes](startup.md#scenes), [Screen](startup.md#screen), [User](startup.md#user), [Foundation](startup.md#foundation) |
| Dữ liệu và kinh tế | [Save](data.md#save), [RemoteConfig](data.md#remoteconfig), [Currency](data.md#currency), [Reward](data.md#reward), [Skins](data.md#skins) |
| Giao diện và feedback | [UI](presentation.md#ui), [Tween](presentation.md#tween), [Pool](presentation.md#pool), [Audio](presentation.md#audio), [Haptic](presentation.md#haptic), [FloatingText](presentation.md#floatingtext), [Particles](presentation.md#particles) |
| SDK và doanh thu | [Ads](services.md#ads), [Monetization](services.md#monetization), [IAP](services.md#iap), [Analytics](services.md#analytics), [Firebase progress](services.md#firebase-progress) |
| Feature tùy chọn | [Lives](features.md#lives), [Quest](features.md#quest), [DailyReward](features.md#dailyreward), [LegacyPowerUps](features.md#legacypowerups), [Game mẫu](features.md#lớp-game-mẫu) |
| Công cụ và kiểu dùng chung | [Inspector](tools.md#inspector), [CachedComponents](tools.md#cachedcomponents), [Collections](tools.md#collections), [Types](tools.md#types), [Utilities](tools.md#utilities) |

## Đặt code và dữ liệu ở đâu

| Thành phần | Vị trí |
| --- | --- |
| Dịch vụ dùng chung | `Assets/NebulaSoft Core/Modules/<Module>/Runtime` |
| Inspector, drawer và công cụ editor | `Editor` của module hoặc `NebulaSoft Core/Editor` nếu dùng xuyên module |
| Logic retention tái sử dụng | `NebulaSoft Core/Features/<Feature>/Runtime` |
| Điểm nối giữa runtime và bootstrap | `NebulaSoft Core/Integration` hoặc `Modules/Initializer/Runtime/Modules` |
| Rule, adapter, UI và prefab của title | `Project Files/Game/Scripts`, `Game/Integration`, `Game/Features` hoặc `Game/UI` theo trách nhiệm |
| Catalog, level data và cấu hình riêng của game | `Project Files/Data` |
| Regression test | `Assets/Tests/Editor` |

Save, Pool, Tween, Ads, Analytics runtime, Cloud runtime, Firebase SDK adapter và một số helper có assembly riêng. Cloud/Analytics contract không tham chiếu Firebase; adapter SDK thuộc NebulaSoft.Firebase. [Nâng Firebase SDK](../firebase-integration.md). Nhiều module khác vẫn thuộc `Assembly-CSharp`. Khi thêm asmdef, đối chiếu dependency và kiểu serialized trước khi chuyển code. Giữ file `.meta` khi di chuyển asset, và giữ các ID đã phát hành dùng để đọc save hoặc tìm sản phẩm. [Cấu trúc dự án](../project-structure.md)

## Luồng tích hợp

1. Mở scene `Init`. Dùng `Project Init Settings.asset` để khai báo module và kéo asset cấu hình.
2. Đặt code board, input và rule ở lớp game. Adapter của game gọi API core sau khi module tương ứng đã khởi tạo.
3. Đọc trạng thái vào view khi mở scene/page. Gắn event khi view bắt đầu sử dụng và tháo event khi view kết thúc.
4. Lưu state bằng model của feature. Thử với save mới, save cũ và trường hợp tắt module tùy chọn trước khi gắn thêm SDK.

Ví dụ C# trong sổ tay dùng namespace `NebulaSoft`, `UnityEngine` và namespace bổ sung được ghi ở từng mục. Đoạn gọi API ngắn đặt trong method hoặc callback phù hợp của component game. Tên như `rewardedPlacementId`, `worldPosition` và `clip` là giá trị game cung cấp.

Để xem hierarchy và cách nối board vào ba scene, đọc [hướng dẫn triển khai game](../framework-developer-guide.md). Để đi sâu vào lưu dữ liệu và SDK, dùng các tài liệu chuyên đề [Save](../save-module.md), [Ads](../ads-module.md), [IAP](../iap-module.md) và [Analytics](../analytics-module.md).

[Remote Config chuyên sâu](../remote-config-module.md) mô tả thứ tự nguồn, cache theo endpoint, schema, validator, fallback và cách áp tuning vào từng ván.

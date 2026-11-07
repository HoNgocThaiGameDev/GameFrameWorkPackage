# Quy ước đặt tên class và file

[Sổ tay module](README.md)

## Phạm vi sử dụng tên

Tên trong `NebulaSoft Core` mô tả năng lực dùng chung. Dùng `QuestService` cho tiến độ nhiệm vụ, `CurrencyDatabase` cho định nghĩa tiền tệ và `AdsService` cho luồng quảng cáo. Kiểu mô tả board, booster, offer hoặc màn hình của một title đặt ở `Project Files/Game`. Ví dụ `PuzzleRemoteConfigData` và `CoinSafeProgress` thuộc lớp game. [Cấu trúc dự án](../project-structure.md) · [PuzzleRemoteConfigData](../../Assets/Project%20Files/Game/Scripts/Remote%20Config/PuzzleRemoteConfigData.cs)

SDK có thể xuất hiện trong tên adapter ở biên tích hợp, như `AdsKitProvider`. Game gọi contract dùng chung, như `IAdsService`. Adapter của game dịch sự kiện gameplay thành dữ liệu core cần, như `QuestGameProgressAdapter`. [AdsKitProvider](../../Assets/NebulaSoft%20Core/Modules/Ads/Providers/AdsKit/AdsKitProvider.cs) · [QuestGameProgressAdapter](../../Assets/Project%20Files/Game/Features/Quest/Scripts/QuestGameProgressAdapter.cs)

## Tên theo trách nhiệm

Dùng danh từ hoặc cụm danh từ PascalCase cho type, động từ cho method và tiền tố `I` cho interface. Giữ cách viết đã dùng trong repo như `UI`, `SDK`, `IAP`. Viết đầy đủ tên miền nghiệp vụ thay cho chữ viết tắt khó hiểu như `PU` ở API mới. [Hướng dẫn đặt tên kiểu của .NET](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-classes-structs-and-interfaces)

| Tên | Dùng khi | Ví dụ |
| --- | --- | --- |
| `*Service` và `I*Service` | Sở hữu nghiệp vụ, state hoặc điều phối một dịch vụ | `QuestService`, `AdsService`, `IAdsService` |
| `*Controller` | Điều phối luồng của một scene hoặc nhóm UI | `UIController`, `MenuController` |
| `*View`, `*Page`, `*Presenter` | View hiển thị dữ liệu, Page là màn hình trong router, Presenter nối view với use case | `QuestElementView`, `SettingsPage` cho UI mới |
| `*InitModule` | Đăng ký, validate, khởi tạo và giải phóng dịch vụ | `AudioInitModule`, `QuestInitModule` |
| `*Provider` và `I*Provider` | Backend thay thế được | `AdsKitProvider`, `IAnalyticsProvider` |
| `*Adapter` hoặc `*Wrapper` | Adapter dịch contract giữa hai lớp, wrapper gọi API nền tảng hoặc SDK | `QuestGameProgressAdapter`, `AndroidHapticWrapper` |
| `*Settings` | Cấu hình hệ thống hoặc feature | `AnalyticsSettings`, `IAPSettings` |
| `*Definition`, `*Catalog`, `*Database` | Một định nghĩa, danh mục item theo ID, hoặc asset tập hợp dữ liệu | `QuestDefinition`, `AdsCatalog`, `CurrencyDatabase` |
| `*Save`, `*State`, `*Result` | Dữ liệu lưu, trạng thái hiện hành, hoặc kết quả một thao tác | `LivesSave`, `DailyRewardState`, `AdResult` |
| `*Store` | Biên đọc và ghi dữ liệu | `IDailyRewardStateStore` |
| `*Editor`, `*Drawer`, `*Window` | Inspector, property drawer hoặc cửa sổ Unity Editor | `CurrencyDatabaseEditor`, `CurrencyAmountPropertyDrawer`, `DefineManagerWindow` |

Đặt file theo tên type chính. Với `MonoBehaviour` và `ScriptableObject` gắn trong Unity, dùng tên file trùng class chính. Các file gom enum hoặc DTO nhỏ như `AdModels.cs` và `QuestEnums.cs` có thể mang tên nhóm rõ ràng. Khi đặt tên mới, ưu tiên mô tả phần việc cụ thể trước khi chọn hậu tố rộng như `Manager`, `Handler` hoặc `Helper`. `PoolManager` là ví dụ tên hợp lý vì nó quản lý nhiều pool và container.

## Kết quả rà soát

Trong `Modules` và `Features`, các type được rà không mang tên title như PicturePuzzle, FlowerHarvest hoặc HyperGunner. Tên chung như `GameLoading`, `LevelTracker` và `QuestService` mô tả khái niệm có thể dùng ở nhiều game. Hai nhóm cần xử lý là giả định gameplay trong contract và tên legacy chưa nhất quán.

| Hiện tại | Hướng chuẩn hóa | Lý do |
| --- | --- | --- |
| `PUType`, `QuestReward` và `DailyRewardGrant` chứa lựa chọn `Hammer`, `Merge` hoặc `FreezeTimer` | Dùng reward ID/definition trong core, để adapter của title ánh xạ sang booster và cấp thưởng. Giữ mapping số enum legacy khi đọc asset/save cũ. | Đây là dependency dữ liệu của game mẫu, dù tên `QuestReward` và `DailyRewardGrant` đã đủ chung. |
| `QuestGoalType.SpendCoins`, `UsePowerUp` và `QuestGoTarget.PowerUp` | Core nhận goal ID/action ID từ cấu hình. Adapter game map hành vi tiêu currency, dùng booster hoặc điều hướng. | Game khác có thể dùng Gem, Energy hoặc không có booster. |
| `NotchSaveArea` trong `NotchSafeArea.cs` | Tên mới đề xuất `SafeAreaLayout` cho API layout. | Sửa lỗi `Save/Safe` và thể hiện chức năng áp safe area. Nhiều view đang gọi API cũ. |
| `MD_AudioClips.cs`, `MD_CurrencyData.cs`, `MD_CurrencyType.cs` | `AudioClips.cs`, `CurrencyData.cs`, `CurrencyType.cs` | Chuẩn hóa tên file theo type. Di chuyển cùng `.meta` và cập nhật chỗ tham chiếu đường dẫn. |
| `SDKTaskBehavior.cs`, `CurrencyRewardHolder.cs`, `TweenExtension.cs` | `ISDKTaskBehavior.cs`, `CurrencyRewardsHolder.cs`, `TweenExtensions.cs` | File và type chính lệch tiền tố hoặc số nhiều. |
| `User`, `Serializer`, `GameLoading` | Tên ứng viên `PlayerIdentityService`, `SaveFileStore`, `StartupLoadingController` | Tên rõ vai trò hơn khi thêm API mới hoặc thực hiện migration có chủ đích. |
| `UnityCallbackReciever`, `MonoBehaviorInspector` | `SaveLifecycleReceiver`, `MonoBehaviourInspector` | Chuẩn chính tả và làm rõ nơi sở hữu callback. Kiểm tra caller trước khi đổi type public. |

Nguồn đối chiếu cho các trường hợp chính. [PUType](../../Assets/NebulaSoft%20Core/Features/LegacyPowerUps/Runtime/PUType.cs) · [QuestEnums](../../Assets/NebulaSoft%20Core/Features/Quest/Runtime/QuestEnums.cs) · [DailyRewardModels](../../Assets/NebulaSoft%20Core/Features/DailyReward/Runtime/DailyRewardModels.cs) · [NotchSafeArea](../../Assets/NebulaSoft%20Core/Modules/UI/Runtime/NotchSafeArea.cs) · [SaveController](../../Assets/NebulaSoft%20Core/Modules/Save/Runtime/SaveController.cs)

## Đổi tên sau khi đã có dữ liệu

1. Với đổi tên file, chuyển file cùng `.meta` để giữ GUID. Cập nhật path trong test, tool, csproj và generator có liên quan.
2. Với đổi class, namespace hoặc assembly, tìm caller và kiểu serialized trước. Asset reward có `SerializeReference` lưu tên class, namespace và assembly. Dùng migration phù hợp hoặc API chuyển tiếp cho type đã dùng, rồi mở lại asset và kiểm tra không có reference thiếu.
3. Với model save, giữ ID của `SaveModel` hoặc key explicit đang phát hành. Model mặc định có thể dùng `typeof(T).FullName`, nên đổi type có thể khiến lần đọc tìm key khác. Đăng ký mapping từ key cũ khi cần. Với enum đã serialize, giữ số của từng giá trị và map dữ liệu khi chuyển sang ID string.
4. Kiểm tra upgrade bằng prefab/scene và bản sao save cũ. Với cloud, kiểm tra cả model version, key và dữ liệu sau restore.

Thứ tự xử lý đề xuất là tách các khái niệm booster/goal khỏi contract mới của core, chuẩn hóa tên file lệch với type, sau đó mới xem xét đổi tên public type khi có lợi ích rõ. [GlobalSave](../../Assets/NebulaSoft%20Core/Modules/Save/Runtime/GlobalSave.cs) · [Asset reward có managed reference](../../Assets/Project%20Files/Data/Rewards/IAP/Reward%20Starter%20Pack.asset) · [Save và migration](data.md#save)

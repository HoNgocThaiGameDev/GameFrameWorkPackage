# SDK, doanh thu và đồng bộ

[Sổ tay module](README.md)

## Ads

`Contracts` định nghĩa `IAdsService`, `IAdsProvider` và kết quả request. `Runtime/AdsService` quản lý placement, trạng thái và callback. `Configuration` chứa catalog, placement và provider config. `Unity/AdsModule` nối service vào vòng đời Unity. `Providers` chứa Dummy và AdsKit. `AdsManager` trong Monetization là facade mà game mẫu đang dùng. [Source Ads](../../Assets/NebulaSoft%20Core/Modules/Ads) · [Source facade](../../Assets/NebulaSoft%20Core/Modules/Monetization/Runtime/Advertisement/AdsManager.cs)

1. Tạo placement có ID ổn định cho banner, interstitial hoặc rewarded. Thêm placement vào `AdsCatalog` và gắn catalog, provider, placement mặc định trong `AdsModuleSettings`.
2. Gắn settings vào **Monetization/Ads** của `Project Init Settings`. Dùng Dummy provider để kiểm tra flow của game. Khi dùng AdsKit, map từng placement sang SDK placement trong `AdsKitProviderConfig` và đối chiếu SDK/define đang bật.
3. Gọi `ValidateSettings()` khi kiểm tra cấu hình. Khi game gọi provider, đọc kết quả `AdResult` và phân biệt rewarded với đóng quảng cáo. Cấp reward ở service của game sau kết quả `RewardGranted`.

Ví dụ presenter nhận `IAdsService` từ composition. Biến `rewardedPlacementId` là ID trong catalog, `cancellationToken` thuộc vòng đời của luồng gọi.

```csharp
var result = await ads.ShowAsync(rewardedPlacementId, cancellationToken);
if (result.RewardGranted)
{
    // Controller game thực hiện quyền tiếp tục hoặc claim.
}
```

Nếu dùng facade cũ, game gọi `AdsManager.ShowRewardBasedVideo(onResult, "continue")` và chỉ xử lý grant khi `onResult` nhận true. Layout đọc banner state/kích thước do module cung cấp. Tách quyền No Ads khỏi rewarded người chơi tự chọn.

**Khi thêm provider.** Viết `IAdsProvider` trong adapter, trả đúng request ID trong `AdEvent`, rồi tạo `AdsProviderConfig` để dựng instance. Giải phóng native listener và cancel request khi provider dispose. Dùng `AdsService` làm nơi chuẩn hóa trạng thái và duplicate callback. [IAdsProvider](../../Assets/NebulaSoft%20Core/Modules/Ads/Contracts/IAdsProvider.cs)

**Kiểm tra.** Dùng `AdsModuleTests`, Dummy flow và AdsKit test mode. Thử reward callback lặp, đóng không thưởng, request đồng thời, banner ẩn khi No Ads và scene unload. [Cấu hình mẫu](../../Assets/Project%20Files/Data/Ads/AdsModuleSettings.asset) · [Ads chuyên sâu](../ads-module.md)

## Monetization

`MonetizationSettings` chứa cờ hoạt động và reference cấu hình Ads/IAP của luồng hiện có. `MonetizationSDKBehavior` chạy sau bước SDK task và gọi `Monetization.Init`, `AdsManager.Init`, `IAPManager.Init`. Cấu hình riêng của `AdsInitModule` và `IAPInitModule` được đăng ký từ InitModule trước bước SDK. [Source Monetization](../../Assets/NebulaSoft%20Core/Modules/Monetization/Runtime) · [SDK behavior](../../Assets/NebulaSoft%20Core/Modules/Monetization/Runtime/MonetizationSDKBehavior.cs)

1. Kiểm tra `SDKs.prefab` trong flow Init, component `MonetizationSDKBehavior` và reference `Monetization Settings.asset`.
2. Đặt các init module Ads/IAP và task consent cần thiết trong composition. Thiết lập provider/target của title, sau đó kiểm tra lần lượt trạng thái readiness của từng service.
3. Game đọc trạng thái từ facade hoặc interface để bật nút và offer. Với No Ads, dùng entitlement qua `AdsManager.SetNoAdsEntitlement`. Rule riêng như vị trí offer, khi nào hiện interstitial và gói quà nằm ở game.

**Kiểm tra.** Khởi động với Dummy, kiểm tra consent task lỗi/timeout và khi SDK chưa sẵn sàng. Startup có thể vào Menu trước khi provider async sẵn sàng. [Prefab SDKs](../../Assets/Project%20Files/Game/Prefabs/SDKs.prefab) · [Initializer](startup.md#initializer)

## IAP

`IAPSettings` là catalog các `IAPItem`. `IAPManager` quản lý readiness, purchase/restore và lưu tiến độ fulfillment. `IAPWrapper` chuyển yêu cầu sang Dummy hoặc Unity Purchasing adapter. `IAPRewardsHolder` và `IAPButton` nối prefab offer vào catalog. [Runtime Purchasing](../../Assets/NebulaSoft%20Core/Modules/Monetization/Runtime/Purchasing)

1. Trong `IAP Settings.asset`, thêm sản phẩm với key cố định, loại sản phẩm, ID Android/iOS và `RewardsSet`. Giữ key và mapping legacy đã phát hành khi đổi vị trí hoặc tên hiển thị.
2. Gắn catalog vào **IAP** trong registry và giữ catalog trùng với reference của Monetization Settings nếu cả hai đường cấu hình được dùng.
3. Trên prefab offer, gắn `IAPRewardsHolder`, chọn product, kéo purchase button và gắn label giá vào `IAPButton`. Với UI riêng, bind readiness và event hoàn tất/thất bại, rồi gọi API theo key.

```csharp
var price = IAPManager.GetProductLocalPriceString("coins_2500");
IAPManager.BuyProduct("coins_2500");
// Từ nút Restore của game
IAPManager.RestorePurchases();
```

`GetIAPItemByKey` nhận key catalog. Overload `GetIAPItem(string)` nhận store ID. Grant thuộc adapter và manager, nên callback UI chỉ cập nhật trình bày. Adapter gọi `TryFulfillPurchase` với định danh giao dịch ổn định, ghi checkpoint từng reward trước khi xác nhận giao dịch thành công. Restore dùng cho entitlement và behavior restore đã cấu hình.

**Kiểm tra.** Dùng Dummy để kiểm tra price placeholder, khóa nút, duplicate transaction và retry qua reload. Dùng store sandbox để kiểm tra giá thật, deferred order và restore. [Catalog mẫu](../../Assets/Project%20Files/Data/IAP%20Settings.asset) · [IAP chuyên sâu](../iap-module.md) · [IAPModuleTests](../../Assets/Tests/Editor/IAPModuleTests.cs)

## Analytics

`Runtime` chứa `IAnalyticsService`, `IAnalyticsProvider`, service có buffer và bộ event chuẩn. `Unity` chứa settings, InitModule và component quản lý vòng đời. `Providers` chứa adapter. `Legacy` giữ các API và event cũ, trong đó `AnalyticsController.Events` là điểm gọi thuận tiện cho game. [Source Analytics](../../Assets/NebulaSoft%20Core/Modules/Analytics)

1. Thêm **Analytics** trong registry và gắn settings. Dùng Console provider để kiểm tra event khi phát triển.
2. Controller gọi event sau action đã thực hiện. Lấy ID level, attempt, số move và thời lượng từ session của game. Chọn một owner báo start/end để tránh gửi lặp khi scene load lại.
3. Khi thêm provider, triển khai `IAnalyticsProvider` cho init, collection, user property, log và dispose. Tạo `AnalyticsProviderConfig` trả instance mới, tạo asset và gán vào settings; không cần sửa enum hoặc module core. Factory được ưu tiên hơn lựa chọn backend legacy.

Settings cho phép chọn riêng bridge Ads/IAP/UI và capacity queue startup. Mặc định All/128 giữ hành vi của asset cũ. Đổi user ID xóa queue và các property đã biết của tài khoản trước, kể cả anonymous → login; gọi SetUserId trước rồi đặt property mới và gửi event. Nếu áp context vào SDK lỗi, service chặn gửi tới khi caller áp lại context thành công. `AnalyticsController.Diagnostics` cho biết event đã gửi, overflow, bị bỏ do đổi tài khoản/tắt collection/shutdown và lỗi gửi; SentCount chỉ xác nhận lời gọi provider thành công.

Mỗi lượt initialization có timeout từ settings, mặc định 30 giây. Service trả false khi cancellation đến ngay cả khi provider mất callback, giữ queue cho lượt retry. Module retry khi resume hoặc qua `RetryInitialization()`. Shutdown qua InitModule giải phóng service/bridge thuộc module đó. [Lifecycle và quyền sở hữu](../analytics-module.md#quyền-sở-hữu-và-kết-quả-api)

```csharp
AnalyticsController.Events.LevelStart(levelNumber, attemptNumber);
AnalyticsController.Events.LevelEnd(
    levelNumber, success, durationSeconds, attemptNumber, moveCount);
```

Adapter Firebase Analytics dùng `FIREBASE_ANALYTICS` khi SDK đã có. Đối chiếu event tự ghi của SDK và event game để tránh đếm doanh thu hoặc impression hai lần. [Analytics settings mẫu](../../Assets/Project%20Files/Data/Analytics/Analytics%20Settings.asset) · [Analytics chuyên sâu](../analytics-module.md)

**Kiểm tra.** Dùng `AnalyticsModuleTests` cho queue và retry. Chạy start/win/fail/retry với Console provider, kiểm tra số event và tham số trước khi bật adapter.

## Firebase progress

`FirebaseAuthHandler` sở hữu phiên đăng nhập. `FirebaseProfileHandler` tạo snapshot và điều phối Local/Cloud. `FirebasePlayerNameRegistry` thực hiện transaction ghi player. `SaveCloudData` kiểm tra phiên bản payload độc lập với Firebase SDK. Logic của title ở `Game/Integration/Firebase/Runtime` dùng ICloudAuthProvider/ICloudDocumentStore và snapshot của framework. SDK adapter chung thuộc assembly NebulaSoft.Firebase. [Ranh giới SDK và nâng version](../firebase-integration.md). [Adapter Firebase](../../Assets/Project%20Files/Game/Integration/Firebase/Runtime) · [SaveCloudData](../../Assets/NebulaSoft%20Core/Modules/Save/Runtime/SaveCloudData.cs)

1. Cấu hình Auth/Firestore cho title và bật `FIREBASE` khi SDK tương ứng có mặt. Gắn flow tài khoản vào adapter của game.
2. Với model cần sync, đặt `[SaveModel("stable.id", CloudSync = true)]` và khởi tạo model trước export. Game tiếp tục đọc/ghi qua `SaveController`. Tích hợp thêm mapping nếu model thuộc luồng field legacy do profile handler quản lý riêng.
3. Kiểm tra tài khoản hiện hành trước khi áp kết quả async. Khi chọn Cloud, kiểm tra version, khôi phục model và cập nhật view phụ thuộc. Giữ key và schema đã phát hành khi thay đổi model.

**Kiểm tra.** Thử người chơi mới, thiết bị mới, đổi tài khoản, offline, old save, bản game khác version và model chưa hỗ trợ. Dùng test payload cho migration/validation, emulator hoặc sandbox cho transaction thật. Các giới hạn về JSON cùng version và conflict mặc định được mô tả ở phần Save. [Restore và cloud](data.md#restore-backup-và-cloud) · [FirebaseProgressSnapshotTests](../../Assets/Tests/Editor/FirebaseProgressSnapshotTests.cs)

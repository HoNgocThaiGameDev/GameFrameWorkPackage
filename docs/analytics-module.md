# Analytics module 1.0.0

Module Analytics dùng chung cho casual/puzzle, thuộc `Assets/NebulaSoft Core/Modules/Analytics`. Module đã được đăng ký trong `Project Init Settings.asset`, ngay sau Save, và trỏ tới `Assets/Project Files/Data/Analytics/Analytics Settings.asset`.

## Cấu trúc và điểm tham khảo HyperGunner

HyperGunner đặt các hàm `LogLogin`, `LogLevelStart`, `LogLevelEnd`, currency, IAP và ads trong `FirebaseAnalyticsHandler`. Gameplay gọi wrapper; wrapper trực tiếp tạo `Firebase.Analytics.Parameter` và gọi `FirebaseAnalytics.LogEvent`. Wrapper phụ thuộc SDK và được bật bằng `FIREBASE`.

Framework giữ cách gọi event ngắn nhưng tách việc vận chuyển ra khỏi gameplay:

```text
Project Init Settings → AnalyticsInitModule → AnalyticsModule
                                                ├─ Analytics Settings
Gameplay → AnalyticsController.Events           ├─ Monetization/UI bridge
         → AnalyticsEvents → AnalyticsService → IAnalyticsProvider
                                                ├─ ConsoleProvider
                                                └─ FirebaseProvider
```

- `Runtime/`: assembly `NebulaSoft.Analytics.Runtime`, không phụ thuộc Unity, Firebase, Ads, IAP hoặc kiểu gameplay. Chứa dữ liệu event, service, API event và `LevelTracker`.
- `Unity/`: module, settings và bridge với callback của Ads/IAP/UI.
- `Providers/`: các adapter. Chỉ `FirebaseProvider.cs` tham chiếu Firebase Analytics.
- Các API legacy `AnalyticsController.EventFired`, `OnCurrencySource`, `OnCurrencySink` và module tùy chỉnh cũ vẫn tồn tại. Currency được chuyển sang service mới. Legacy IAP/ad request chỉ tiếp tục thông báo `EventFired`; các event tự động của service mới lấy từ callback chuẩn của Ads/IAP để tránh đếm trùng hoặc báo impression khi show thất bại.

## Quy ước tên script

Tên file trùng với type chính, dùng PascalCase và ưu tiên ngắn, dễ đọc. Namespace/thư mục Analytics cung cấp ngữ cảnh nên các payload dùng `CurrencyEvent`, `PurchaseEvent`…; không lặp cả tiền tố `Analytics` và hậu tố `EventData`. Interface dùng tiền tố `I`, adapter dùng hậu tố `Provider`. File partial dùng `TênType.PhầnChứcNăng.cs`.

| Script | Vai trò |
|---|---|
| `AnalyticsEvents.cs` | API tạo và gửi bộ event dùng chung cho game |
| `LevelTracker.cs` | Theo dõi một lượt chơi, kết quả và thời gian pause |
| `AnalyticsService.cs`, `IAnalyticsService.cs` | Điều phối khởi tạo, queue và trạng thái collection |
| `IAnalyticsProvider.cs` | Contract cho adapter của dịch vụ analytics |
| `ConsoleProvider.cs`, `FirebaseProvider.cs` | Adapter gửi tới từng provider |
| `AnalyticsInitModule.cs`, `AnalyticsModule.cs` | Đăng ký khởi tạo và quản lý vòng đời trong Unity |
| `AnalyticsBridge.cs` | Nối Ads, IAP và UI của framework với analytics |
| `AnalyticsSettings.cs`, `AnalyticsBackend.cs` | Cấu hình module và lựa chọn provider |
| `AnalyticsEventType.cs`, `AnalyticsModules.Registration.cs` | Enum và danh sách đăng ký cho API event cũ |
| `CurrencyEvent.cs`, `LevelEvent.cs` | Payload currency/level cho API cũ |
| `PurchaseEvent.cs`, `PurchaseFailEvent.cs` | Payload mua hàng và lỗi mua hàng |
| `RewardedAdEvent.cs`, `InterstitialEvent.cs` | Payload quảng cáo với tên format đầy đủ |

Các caller trong project dùng tên trong bảng trên. Đổi tên giữ nguyên `.meta`/GUID, payload và tên event Firebase. `AnalyticsSettings` có khai báo chuyển tên để Unity nhận settings của phiên bản trước.

## Cấu hình trong Unity

1. Chọn `Assets/Project Files/Data/Project Init Settings.asset`.
2. Tìm module **Analytics**, xem trường **Settings**. Với settings asset khác, thêm module bằng **Add Module → Analytics**.
3. Tạo settings mới bằng **Create → NebulaSoft → Analytics → Settings** nếu cần.
4. Chọn provider **Console** hoặc **Firebase**, và trạng thái collection ban đầu.

Template hiện dùng **Console**. Trong Editor, module luôn dùng Console kể cả khi settings chọn Firebase. Không cần tài khoản hoặc SDK Firebase để chạy/test template.

Để gửi từ bản Android/iOS:

1. Import `FirebaseAnalytics.unitypackage` từ Firebase Unity SDK, cùng phiên bản với Firebase App/Auth/Firestore của game. Adapter đã được kiểm tra biên dịch với SDK **13.4.0** từ HyperGunner; không sao chép SDK hoặc thông tin app của HyperGunner vào template.
2. Cấu hình Firebase project và Google Analytics cho application ID của game; thêm file cấu hình platform của chính game đó.
3. Bật scripting define **`FIREBASE_ANALYTICS`** cho target Android/iOS sau khi SDK có mặt. Define `FIREBASE` của Auth/Firestore không tự bật adapter Analytics.
4. Chọn provider **Firebase** trong Analytics Settings, resolve dependency native và build thiết bị.

Firebase provider tự kiểm tra dependency; Analytics không cần chờ người chơi đăng nhập Firebase Auth. Nếu chưa cài SDK/define nhưng chọn Firebase cho player build, module báo warning rõ ràng; không giả vờ gửi thành công.

## Event và tham số

Level trong API là số người chơi nhìn thấy, bắt đầu từ **1**. Giá trị số gửi dưới dạng integer/double, không chuyển thành chuỗi. Tên event/parameter phải tuân thủ quy tắc Firebase: bắt đầu bằng chữ ASCII, tối đa 40 ký tự, chỉ chữ/số/underscore và không dùng tiền tố `firebase_`, `google_`, `ga_`. Tối đa 25 parameter; giá trị chuỗi được giới hạn 100 ký tự.

| API trên `AnalyticsController.Events` | Event | Thông tin chính |
|---|---|---|
| `LevelStart` | `level_start` | level, level_name, attempt, level_type |
| `LevelEnd` | `level_end` | level, success (0/1), duration (giây), attempt, moves, boosters_used, reason |
| `LevelFail` | `level_fail` | level, reason; có thể tiếp tục bằng revive |
| `LevelRetry`, `LevelRevive` | `level_retry`, `level_revive` | level, nguồn revive |
| `TutorialBegin`, `TutorialStep`, `TutorialComplete` | `tutorial_begin`, `tutorial_step`, `tutorial_complete` | tutorial_id, step |
| `BoosterUsed`, `BoosterPurchased` | `booster_used`, `booster_purchased` | booster_id, level, remaining hoặc quantity/source |
| `EarnCurrency`, `SpendCurrency` | `earn_virtual_currency`, `spend_virtual_currency` | virtual_currency_name, value, source/item_name |
| `RewardClaimed` | `reward_claimed` | source, reward_id, quantity |
| `IapAttempt`, `IapCompleted`, `IapFailed` | `iap_attempt`, `iap_purchase`, `iap_failed` | item_id, reason nếu thất bại |
| `ScreenView`, `Login`, `SettingChanged` | `screen_view`, `login`, `setting_change` | tên màn hình, cách đăng nhập, setting/value |
| `AdRequested`, `AdShown` | `ad_request`, `ad_show` | placement, ad_format, ad_platform khi có |
| `AdImpression`, `AdClicked`, `AdClosed` | `ad_impression_custom`, `ad_click_custom`, `ad_close` | placement, format/platform |
| `AdRewarded`, `AdFailed` | `ad_reward`, `ad_fail` | placement, stage load/show, reason |

Các event IAP ở đây đo funnel vận hành, không tự ghi `purchase`/`in_app_purchase` hoặc gán doanh thu từ giá giả của Dummy Store. Firebase có cơ chế tự thu thập purchase/AdMob revenue; cấu hình và kiểm tra doanh thu theo SDK/store thực tế. `ad_show` và `ad_impression_custom` khác nhau: bridge chỉ phát impression khi provider báo `AdEventKind.Impression`, không suy ra từ yêu cầu show. Event custom này tách khỏi `ad_impression` của SDK, không ghi doanh thu hoặc làm tăng bộ đếm event doanh thu đó.

## Dùng từ gameplay

```csharp
using NebulaSoft;
using NebulaSoft.Analytics;
using UnityEngine;

// Một session cho một lượt chơi. Game sở hữu cách đếm attempt và điều kiện thắng/thua.
var run = new LevelTracker(AnalyticsController.Events,
    () => Time.realtimeSinceStartupAsDouble);
run.Start(level: 12, attempt: 2);
run.Move();
run.Booster("hammer", remaining: 1);
run.Fail("timeout");                  // Chưa đóng lượt nếu còn cơ hội revive.
run.Revive("rewarded_ad");
run.End(success: true);                // level_end cuối cùng chỉ được phát một lần.

AnalyticsController.Events.TutorialBegin("intro");
AnalyticsController.Events.TutorialStep("intro", 2);
AnalyticsController.Events.TutorialComplete("intro");
AnalyticsController.Events.BoosterPurchased("hammer", 3, "shop");
AnalyticsController.Events.Login("anonymous");
AnalyticsController.Events.SettingChanged("sound", "off");
```

Khi dùng session, để session sở hữu các event level của lượt đó. `Start` và `End` được bảo vệ khỏi gọi lặp. `SetPaused(true/false)` loại thời gian app chạy nền khỏi duration. Khi bỏ màn, gọi `End(false, "quit")`; khi retry, ghi `LevelRetry` rồi đóng lượt cũ. Game mới tự nối `Move` và `Booster` vào các thao tác thật; template không có board/booster gameplay để suy ra những thao tác đó.

Sample `GameController` tự gọi `ActivateGame` khi vào màn chơi và đã nối start/end/fail/revive/retry/quit cùng application pause. Board của game mới có thể gọi `GameController.AnalyticsSession?.Move()` / `.Booster(...)` để cập nhật cùng lượt chơi. Nếu game cần chờ board hoặc tutorial trước khi bắt đầu, chuyển lời gọi `ActivateGame` từ `Start` tới thời điểm sẵn sàng đó. Daily Reward, quest/milestone claim, currency, UI page opened, IAP và Ads có các điểm tích hợp sẵn. Các tutorial/booster riêng của game mới dùng API trên sau khi thao tác thực sự thành công.

## Vòng đời, collection và startup

- `AnalyticsService` giữ tối đa **128** event chờ khởi tạo, bỏ event cũ nhất khi đầy. Dữ liệu event được chụp ngay khi tạo; thay dictionary đầu vào sau đó không đổi event đã xếp hàng.
- Service khởi tạo một lần, gửi FIFO sau khi provider sẵn sàng. Lỗi provider được báo nhưng không ngắt gameplay hoặc chặn event sau.
- `AnalyticsController.SetCollectionEnabled(false)` xóa queue và bỏ event mới cho tới khi bật lại. Ứng dụng nối API này với quyết định collection/consent hiện có. Khi bật lại, không phát lại event đã bỏ.
- Có `SetUserId`/`SetUserProperty`; caller cung cấp ID nội bộ, không gửi email, tên thật hoặc token làm event parameter. Set user ID về `null` khi cần xóa ngữ cảnh tài khoản.
- Module hủy đăng ký Ads/IAP/UI và dispose service khi owner bị hủy. Callback khởi tạo hoàn tất muộn không gửi các event của owner đã dispose.
- Queue trước initialization chỉ ở RAM. Sau khi nhận event, Firebase SDK sở hữu upload/offline buffering. Event quit là best effort; không suy ra retention hoặc một lần thoát app từ việc chắc chắn nhận được quit event khi hệ điều hành kill process.

## Kiểm chứng

`Assets/Tests/Editor/AnalyticsModuleTests.cs` kiểm tra schema, snapshot, queue, initialization, collection, user context, disposal, lỗi provider, session revive/quit/pause, Ads callback, IAP failure, legacy currency và registry. `AnalyticsPlayModeTests.cs` vào Play Mode, tạo component qua đúng `AnalyticsInitModule` trong registry, xác nhận queue được gửi qua Console và service được dispose sau khi Unity hoàn tất hủy GameObject.

Trong Unity Test Runner chạy nhóm `NebulaSoft.Tests.AnalyticsModuleTests`. Chạy thêm các nhóm Ads/IAP và scene smoke vì module có bridge với chúng.

Đã kiểm tra trên Unity **6000.3.12f1** ngày **15/10/2026**: **78/78** test thuộc Analytics, Ads, IAP và scene smoke pass; thêm **1/1** test lifecycle trong Play Mode pass. Nhánh adapter `FIREBASE_ANALYTICS` được biên dịch riêng với DLL Firebase **13.4.0**: **0 error, 0 warning**. Các SDK Firebase và cấu hình ứng dụng không được thêm vào template trong thay đổi này.

Sau khi rút gọn tên, chạy lại **79/79 test pass**, không skipped; cả **28** script khớp tên type (hoặc tên partial type). Giữ nguyên `.meta` của **13** script được đổi tên và settings asset vẫn giữ GUID/cấu hình cũ. Adapter Firebase biên dịch lại với SDK **13.4.0**: **0 error, 0 warning**.

Để xác nhận dữ liệu thiết bị, bật Firebase DebugView cho build của chính game và kiểm tra event/parameter. Test Editor/compile adapter không chứng minh dữ liệu đã xuất hiện trên Firebase Console.

Tài liệu Firebase: [Unity events](https://firebase.google.com/docs/analytics/unity/events), [Unity Analytics API](https://firebase.google.com/docs/reference/unity/class/firebase/analytics/firebase-analytics), [ad revenue](https://firebase.google.com/docs/analytics/measure-ad-revenue).

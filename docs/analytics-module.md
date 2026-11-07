# Analytics

Module Analytics dùng chung cho casual/puzzle, thuộc `Assets/NebulaSoft Core/Modules/Analytics`. Module đã được đăng ký trong `Project Init Settings.asset`, ngay sau Save, và trỏ tới `Assets/Project Files/Data/Analytics/Analytics Settings.asset`.

## Cấu trúc

Gameplay gửi event qua API dùng chung và provider thực hiện vận chuyển.

```text
Project Init Settings → AnalyticsInitModule → AnalyticsModule
                                                ├─ AnalyticsSettings
                                                │    └─ AnalyticsProviderConfig → provider instance
Gameplay → AnalyticsController.Events           ├─ AnalyticsBridge (Ads / IAP / UI tùy chọn)
         → AnalyticsEvents → AnalyticsService → IAnalyticsProvider
                                                ├─ ConsoleProvider
                                                ├─ FirebaseProvider
                                                └─ Adapter của game
```

- `Runtime/`: assembly `NebulaSoft.Analytics.Runtime`, không phụ thuộc Unity, Firebase, Ads, IAP hoặc kiểu gameplay. Chứa dữ liệu event, service, API event và `LevelTracker`.
- `Unity/`: module, settings và bridge với callback của Ads/IAP/UI.
- `Providers/`: Console adapter và factory asset. FirebaseProvider nằm ở `Integrations/Firebase/Runtime`, assembly NebulaSoft.Firebase; chỉ adapter đó tham chiếu Firebase Analytics. [Ranh giới SDK và nâng version](firebase-integration.md).
- `Legacy/` giữ facade `AnalyticsController`, payload và module của API cũ.
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
| `AnalyticsSettings.cs`, `AnalyticsProviderConfig.cs` | Cấu hình module và factory asset tạo provider |
| `ConsoleAnalyticsProviderConfig.cs`, `FirebaseAnalyticsProviderConfig.cs` | Factory asset có sẵn |
| `AnalyticsBackend.cs` | Mapping enum được giữ cho settings cũ |
| `AnalyticsBridgeOptions.cs` | Bật/tắt từng bridge tự động |
| `AnalyticsDiagnostics.cs` | Snapshot thống kê gửi, bỏ event và lỗi khởi tạo |
| `AnalyticsEventType.cs`, `AnalyticsModules.Registration.cs` | Enum và danh sách đăng ký cho API event cũ |
| `CurrencyEvent.cs`, `LevelEvent.cs` | Payload currency/level cho API cũ |
| `PurchaseEvent.cs`, `PurchaseFailEvent.cs` | Payload mua hàng và lỗi mua hàng |
| `RewardedAdEvent.cs`, `InterstitialEvent.cs` | Payload quảng cáo với tên format đầy đủ |

Các caller trong project dùng tên trong bảng trên. Đổi tên giữ nguyên `.meta`/GUID, payload và tên event Firebase. `AnalyticsSettings` có khai báo chuyển tên để Unity nhận settings của phiên bản trước.

## Cấu hình trong Unity

1. Chọn `Assets/Project Files/Data/Project Init Settings.asset`.
2. Tìm module **Analytics**, xem trường **Settings**. Với settings asset khác, thêm module bằng **Add Module → Analytics**.
3. Tạo settings mới bằng **Create → NebulaSoft → Analytics → Settings** nếu cần.
4. Tạo factory bằng **Create → NebulaSoft → Analytics → Providers → Console/Firebase**, gán vào **Provider Config**. Factory được ưu tiên khi đã gán; trường **Provider** tiếp tục phục vụ asset cũ chưa có factory.
5. Chọn **Automatic Bridges**: `Ads`, `IAP`, `UI`, kết hợp các flag hoặc `None`. Mặc định `All` giữ hành vi cũ. Các flag chỉ điều khiển callback tự động, API gửi event thủ công vẫn hoạt động.
6. Đặt **Pending Event Capacity** (mặc định 128, tối thiểu 1), trạng thái collection ban đầu và **Initialization Timeout Seconds** (mặc định 30 giây).

Template hiện dùng **Console** với settings cũ. Factory Firebase và mapping enum dùng Console trong Editor. Factory tùy chỉnh có thể override `AllowInEditor => true` để test adapter thật hoặc fake trong Editor; mặc định false. Không cần tài khoản hoặc SDK Firebase để chạy/test template.

Để gửi từ bản Android/iOS:

1. Import `FirebaseAnalytics.unitypackage` từ Firebase Unity SDK, cùng phiên bản với Firebase App/Auth/Firestore của game.
2. Cấu hình Firebase project và Google Analytics cho application ID của game; thêm file cấu hình platform của chính game đó.
3. Bật scripting define **`FIREBASE_ANALYTICS`** cho target Android/iOS sau khi SDK có mặt. Define `FIREBASE` của Auth/Firestore không tự bật adapter Analytics.
4. Gán factory **Firebase** vào Analytics Settings; với asset cũ chưa có factory có thể tiếp tục chọn enum **Firebase**. Resolve dependency native và build thiết bị.

Firebase provider tự kiểm tra dependency; Analytics không cần chờ người chơi đăng nhập Firebase Auth. Nếu chưa cài SDK/define nhưng chọn Firebase cho player build, module báo warning rõ ràng; không giả vờ gửi thành công.

## Event và tham số

Level trong API là số người chơi nhìn thấy, bắt đầu từ **1**. Giá trị số gửi dưới dạng integer/double, không chuyển thành chuỗi. Schema chung giữ giới hạn tương thích Firebase: tên bắt đầu bằng chữ ASCII, tối đa 40 ký tự, chỉ chữ/số/underscore và không dùng tiền tố `firebase_`, `google_`, `ga_`. Tối đa 25 parameter; giá trị chuỗi được giới hạn 100 ký tự. Adapter backend khác cần mapping schema này sang API của SDK; không tự nới contract của event đã phát hành theo backend đang chọn.

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

- `AnalyticsService` giữ event chờ theo **Pending Event Capacity**, bỏ event cũ nhất khi đầy. Trước bootstrap, facade tạo queue mặc định **128**; khi module configure, áp capacity từ settings và bỏ bớt event cũ nhất nếu cần. Tăng capacity sau đó không khôi phục event đã bỏ. Dữ liệu event được chụp ngay khi tạo; thay dictionary đầu vào sau đó không đổi event đã xếp hàng.
- Service khởi tạo một lần, gửi FIFO sau khi provider sẵn sàng. Lỗi LogEvent được báo nhưng không ngắt gameplay hoặc chặn event sau; lỗi áp user context sẽ chặn gửi để bảo vệ attribution.
- `AnalyticsController.SetCollectionEnabled(false)` xóa queue và bỏ event mới cho tới khi bật lại. Ứng dụng nối API này với quyết định collection/consent hiện có. Khi bật lại, không phát lại event đã bỏ.
- Có `SetUserId`/`SetUserProperty`; caller cung cấp ID nội bộ. Khi ID thay đổi, **xóa queue đang chờ và đặt toàn bộ user property đã biết về null** trước khi gửi event tiếp. Chính sách áp dụng cả anonymous (`null`) → ID và ID → `null`. Gọi lại cùng ID giữ queue/property. Auth flow nên gọi SetUserId trước, sau đó đặt property của tài khoản mới và gửi Login.
- Nếu provider lỗi khi áp user context, service giữ `IsReady` cho trạng thái initialization nhưng chặn LogEvent, trả false và tăng `SendFailureCount`. Sau khi SDK phục hồi, gọi lại SetUserId hoặc SetUserProperty để áp lại toàn bộ context. Property đã xóa được giữ dạng null trong context để provider mới khi retry cũng nhận lệnh xóa.
- Module hủy đăng ký Ads/IAP/UI và dispose service khi owner bị hủy. Callback khởi tạo hoàn tất muộn không gửi các event của owner đã dispose.
- Mỗi lượt khởi tạo có timeout từ settings. Service hoàn tất task trả về với false khi bị cancel, kể cả provider bỏ qua token. Lượt cũ giữ reference provider của chính nó, kiểm tra cancellation trước khi công bố ready và giữ event chờ cho lượt retry.
- Resume gọi lại initialization nếu service chưa ready. UI/công cụ debug có thể gọi `AnalyticsModule.RetryInitialization()` trên component hiện hành. Module đang initialize hoặc đã ready bỏ qua yêu cầu lặp.
- Queue trước initialization chỉ ở RAM. Sau khi nhận event, Firebase SDK sở hữu upload/offline buffering. Event quit là best effort; không suy ra retention hoặc một lần thoát app từ việc chắc chắn nhận được quit event khi hệ điều hành kill process.

## Kiểm chứng

Ca regression bổ sung ngày 2026-11-07 kiểm tra provider mất callback, cancellation trả kết quả ngay, retry bằng provider khác, callback cũ tới muộn và dispose khi initialization chưa hoàn tất. Kết quả chạy và phạm vi kiểm tra được cập nhật ở cuối tài liệu.

`Assets/Tests/Editor/AnalyticsModuleTests.cs` kiểm tra schema, snapshot, queue, initialization, collection, user context, disposal, lỗi provider, session revive/quit/pause, Ads callback, IAP failure, legacy currency và registry. `AnalyticsPlayModeTests.cs` vào Play Mode, tạo component qua đúng `AnalyticsInitModule` trong registry, xác nhận queue được gửi qua Console và service được dispose sau khi Unity hoàn tất hủy GameObject.

Trong Unity Test Runner chạy nhóm `NebulaSoft.Tests.AnalyticsModuleTests`. Chạy thêm các nhóm Ads/IAP và scene smoke vì module có bridge với chúng.

Đã kiểm tra trên Unity **6000.3.12f1** ngày **15/10/2026**: **78/78** test thuộc Analytics, Ads, IAP và scene smoke pass; thêm **1/1** test lifecycle trong Play Mode pass. Nhánh adapter `FIREBASE_ANALYTICS` được biên dịch riêng với DLL Firebase **13.4.0**: **0 error, 0 warning**. Các SDK Firebase và cấu hình ứng dụng không được thêm vào template trong thay đổi này.

## Quyền sở hữu và kết quả API

| Thao tác | Hành vi |
| --- | --- |
| `InitializeAsync(provider, token)` | Trả cùng task khi đang khởi tạo hoặc đã ready. Chuyển quyền quản lý provider cho service khi bắt đầu một lượt được chấp nhận. |
| Cancel initialization | Task của caller nhận false. Queue giữ event còn chờ để retry. Provider phải hợp tác với token để dừng công việc native của nó. |
| Retry sau lỗi/cancel | Lượt mới thay provider trước và dispose provider cũ khi đó là instance khác. Callback hoàn tất của lượt cũ bị bỏ qua. |
| `Track(event)` trả true | Event được đưa vào queue hoặc provider đã chấp nhận lời gọi gửi. Backend có thể còn đang upload. |
| `Track(event)` trả false | Collection tắt, service disposed, event null, context chưa áp thành công hoặc provider ném lỗi lúc gửi. |
| Queue đầy | Bỏ event cũ nhất trước khi thêm event mới. Cấu hình trong settings hoặc constructor service; `ConfigureQueueCapacity` áp dụng ngay và giữ event mới nhất. |
| Provider lỗi lúc gửi | Báo qua `onError` và trả false cho lần gửi trực tiếp. Event đã lấy khỏi startup queue không được tự xếp lại. |
| `Dispose()` | Cancel initialization, bỏ queue và user properties, dispose provider một lần. |

Gọi service trên Unity main thread. SDK callback từ thread khác cần được adapter chuyển về luồng phù hợp trước khi gọi core. Cancellation có thể hoàn tất task từ một thread khác, còn phần tiếp tục thao tác Unity phải giữ đúng synchronization context. Nếu provider đang chờ một Task không bao giờ kết thúc, cancellation giải phóng caller nhưng công việc phía provider chỉ dừng khi adapter/SDK hỗ trợ cancellation hoặc dispose.

Caller tạo provider khác trong lúc service đang initialize cần tự quản lý instance chưa được service nhận. Composition chuẩn tạo một `AnalyticsModule` cho vòng đời app và dùng lần retry của module.

## Triển khai session cho game mới

1. Quyết định ai sở hữu start/end. Nếu dùng `GameController.AnalyticsSession` hiện có, board chỉ gửi Move/Booster vào session đó.
2. Với controller riêng, tạo `LevelTracker` bằng bộ event và một clock đơn điệu như `Time.realtimeSinceStartupAsDouble`.
3. Gọi Start khi người chơi có quyền tương tác. Gọi Fail khi hiện cơ hội revive, Revive khi game thực sự phục hồi và End đúng một lần khi thắng, thua cuối hoặc quit.
4. Forward pause/resume vào `SetPaused`. Khi game có nhiều nguyên nhân pause, controller tổng hợp chúng trước khi gọi tracker.

Ví dụ trong component sở hữu một ván.

```csharp
private NebulaSoft.Analytics.LevelTracker tracker;

private void BeginAttempt(int level, int attempt)
{
    tracker = new NebulaSoft.Analytics.LevelTracker(
        NebulaSoft.AnalyticsController.Events,
        () => UnityEngine.Time.realtimeSinceStartupAsDouble);
    tracker.Start(level, attempt);
}

private void OnApplicationPause(bool paused)
{
    tracker?.SetPaused(paused);
}

private void OnDestroy()
{
    tracker?.End(false, "quit");
}
```

Theo dõi thời điểm hoàn tất thao tác. Currency đã phát Earn/Spend qua bridge hiện có thì controller dùng đường đó để tránh báo hai lần cùng mutation. Ads bridge dùng Started/Impression/RewardGranted từ provider. Mỗi layer ghi event về phần việc nó sở hữu.

## Thêm event riêng và provider mới

Event custom dùng tên ổn định và tham số nguyên thủy, ví dụ dưới đây gửi mục tiêu level. `AnalyticsEvent` chụp dictionary ngay lúc tạo, chuyển int sang long, bool sang 0/1 và float sang double. NaN/Infinity, object lồng nhau và tên sai bị từ chối. Field string dài hơn 100 ký tự được cắt tại tầng core.

```csharp
var parameters = new System.Collections.Generic.Dictionary<string, object>
{
    ["level"] = 12,
    ["goal_id"] = "collect_blue",
    ["completed"] = true
};
bool accepted = NebulaSoft.AnalyticsController.Track(
    new NebulaSoft.Analytics.AnalyticsEvent("goal_completed", parameters));
```

Provider mới được thêm bằng adapter và factory asset:

1. Cài `IAnalyticsProvider`: InitializeAsync, SetCollectionEnabled, SetUserId, SetUserProperty, LogEvent và Dispose. Đặt adapter SDK riêng của game trong thư mục integration của game; adapter dùng chung nhiều game có thể đặt ở `Providers/<Backend>`.
2. Tạo class `<Backend>AnalyticsProviderConfig : AnalyticsProviderConfig`, thêm `[CreateAssetMenu]`. Override `CreateProvider()` để trả **instance mới cho mỗi lượt khởi tạo**. Chỉ lưu cấu hình serialized trong asset; không lưu provider đang chạy vào ScriptableObject.
3. Tạo asset config, gán vào `AnalyticsSettings.ProviderConfig`. Module tự gọi factory cho lần đầu và retry. Không cần thêm enum hoặc sửa nhánh trong AnalyticsModule.
4. Nếu cần chạy SDK đó trong Editor, override `AllowInEditor => true`; nếu không, Editor dùng Console. Đặt tham chiếu SDK và điều kiện define ở adapter/factory, để runtime thuần tiếp tục không biết SDK.
5. Test service bằng fake provider, sau đó kiểm tra adapter trong môi trường SDK thật.

[Factory base](../Assets/NebulaSoft%20Core/Modules/Analytics/Unity/AnalyticsProviderConfig.cs) và [Console factory](../Assets/NebulaSoft%20Core/Modules/Analytics/Providers/ConsoleAnalyticsProviderConfig.cs) là ví dụ tối thiểu đã dùng trong project. Firebase factory luôn tồn tại khi SDK chưa được import, để giữ reference asset; lúc tạo provider trên player sẽ báo lỗi rõ nếu thiếu define.

| Contract adapter | Yêu cầu |
| --- | --- |
| InitializeAsync | Chỉ hoàn tất khi SDK sẵn sàng; chuyển lỗi thành exception, tôn trọng token nếu SDK hỗ trợ. |
| SetUserId / SetUserProperty | Chấp nhận null để xóa; không bỏ qua lệnh xóa tài khoản cũ. |
| SetCollectionEnabled | Áp trạng thái collection vào SDK, kể cả khi giá trị ban đầu là false. |
| LogEvent | Mapping kiểu long/double/string và tên event; chỉ return sau khi SDK nhận lời gọi hoặc throw khi gửi thất bại. Không tự gọi lại event game. |
| Dispose | Hủy callback/event/công việc mà adapter sở hữu; không dispose tài nguyên native dùng chung với module khác. |

Transport, batch và offline persistence thuộc SDK/adapter. Core hiện có một provider cho một service; nếu game cần nhiều backend, triển khai provider tổng hợp với chính sách lỗi và consent rõ ràng rồi tạo nó qua factory. Module chưa có sẵn cơ chế fan-out nhiều backend.

## Diagnostics và event bị bỏ

Đọc `AnalyticsController.Diagnostics` hoặc `((IAnalyticsDiagnostics)service).Diagnostics` khi dùng service có hỗ trợ. Snapshot là struct chỉ đọc, không gửi thêm event analytics và không ghi payload/user ID. Interface `IAnalyticsService` được giữ nguyên để provider/caller tùy chỉnh đang tích hợp không phải triển khai API mới.

| Counter | Ý nghĩa trong vòng đời service hiện tại |
| --- | --- |
| PendingCount | Số event còn chờ initialization. |
| AcceptedCount | Số Track đã vào queue hoặc gửi trực tiếp thành công. Event đã accept vẫn có thể bị bỏ sau đó. |
| SentCount | Số LogEvent return thành công; không phải xác nhận backend đã lưu. |
| QueueOverflowCount | Event cũ bị bỏ do queue đầy hoặc giảm capacity. |
| IdentityDiscardCount | Event chờ bị bỏ khi user ID thay đổi. |
| CollectionDiscardCount | Event chờ bị bỏ khi tắt collection. |
| ShutdownDiscardCount | Event chờ bị bỏ khi dispose service. |
| RejectedCount | Track bị từ chối vì collection tắt, disposed hoặc event null. |
| SendFailureCount | LogEvent ném lỗi hoặc gửi bị chặn do context chưa áp thành công; gồm cả lần flush queue. |
| InitializationFailureCount | Lượt được service nhận nhưng thất bại/cancel, mỗi lượt tính một lần, kể cả provider mất callback. Lỗi factory trước khi service nhận lượt được module log riêng. |

Counter được reset khi tạo service mới. Facade trả snapshot mặc định nếu không có service và không tạo service khi chỉ đọc diagnostics; sau Shutdown muốn xem counter cuối phải giữ reference service cũ. Các counter phản ánh giai đoạn khác nhau nên không cộng tất cả thành tổng event. Provider gửi lỗi không được tự retry trong core vì SDK có thể đã nhận event trước khi throw; transport retry thuộc adapter.

## Xử lý lỗi khi tích hợp

| Triệu chứng | Kiểm tra |
| --- | --- |
| Chỉ thấy Console log khi chọn Firebase | Factory/enum Firebase dùng Console trong Editor. Kiểm tra Provider Config vì có ưu tiên cao hơn enum. Test Firebase trong player với đúng define. |
| PendingCount tăng, IsReady false | Kiểm tra initialization, timeout và dependency SDK. Khi có thể retry, gọi RetryInitialization hoặc resume app. |
| Event mất trong lúc offline | Phân biệt queue startup trong RAM và persistence của SDK. Đọc QueueOverflowCount, IdentityDiscardCount, CollectionDiscardCount và SendFailureCount. |
| Event tăng gấp đôi | Kiểm tra bridge tự ghi, caller manual và việc tạo nhiều session/module. |
| Event bị bỏ sau đổi tài khoản | Queue bị xóa có chủ đích để không gán event cũ cho user mới. Gọi SetUserId trước khi gửi event và đặt property mới. Kiểm tra IdentityDiscardCount. |
| Event Ads/IAP/UI tự động không xuất hiện | Kiểm tra flag Automatic Bridges của module và callback nguồn thực sự đã phát. |
| Backend không thấy event dù Track true | Đối chiếu provider, project/app ID, collection, transport và công cụ debug của backend. |

Tài liệu tích hợp ngắn nằm trong [sổ tay Analytics](modules/services.md#analytics). Runtime source ở [AnalyticsService](../Assets/NebulaSoft%20Core/Modules/Analytics/Runtime/AnalyticsService.cs), composition ở [AnalyticsModule](../Assets/NebulaSoft%20Core/Modules/Analytics/Unity/AnalyticsModule.cs).

Sau khi rút gọn tên, chạy lại **79/79 test pass**, không skipped; cả **28** script khớp tên type (hoặc tên partial type). Giữ nguyên `.meta` của **13** script được đổi tên và settings asset vẫn giữ GUID/cấu hình cũ. Adapter Firebase biên dịch lại với SDK **13.4.0**: **0 error, 0 warning**.

Để xác nhận dữ liệu thiết bị, bật Firebase DebugView cho build của chính game và kiểm tra event/parameter. Test Editor/compile adapter không chứng minh dữ liệu đã xuất hiện trên Firebase Console.

Tài liệu Firebase: [Unity events](https://firebase.google.com/docs/analytics/unity/events), [Unity Analytics API](https://firebase.google.com/docs/reference/unity/class/firebase/analytics/firebase-analytics), [ad revenue](https://firebase.google.com/docs/analytics/measure-ad-revenue).

Đợt kiểm tra Analytics ngày 2026-11-07 trước khi tách SDK trên Unity 6000.6.8f1 đạt **223/223 EditMode test**, không skip, gồm **37/37 Analytics test**; ModuleAudit đạt **10/10 assertion**. Bổ sung 11 ca về identity/anonymous/logout, context lỗi, diagnostics, factory, queue settings và bridge tùy chọn; các ca cancellation, late callback, shutdown qua InitModule và reinitialize tiếp tục pass. Settings asset cũ chưa có field mới vẫn dùng Console, capacity 128 và toàn bộ bridge. Ví dụ C# được biên dịch với assembly hiện tại; 27 link nội bộ của hai trang Analytics/services đều tồn tại, 33 script Analytics đều có meta. Nhánh factory/adapter Firebase biên dịch với DLL SDK **14.1.0**, không có compile error; vẫn có warning hiện hữu của dự án khi biên dịch toàn assembly. Không import SDK vào Assets hoặc chạy player build. Native Firebase, upload backend và profiling trên thiết bị cần được kiểm tra trong môi trường tích hợp của title.

Sau khi tách Firebase SDK sang assembly adapter, toàn suite đạt **242/242**, gồm 37 Analytics và 19 Cloud test; managed compile với SDK 13.4.0/14.1.0 pass cho all, Analytics-only và Cloud-only. Xem [ranh giới SDK](firebase-integration.md).

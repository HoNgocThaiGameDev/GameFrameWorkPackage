# Đánh giá module GameFrameWorkPackage — 2026-11-07

## Kết quả

**15 phát hiện: 3 P1, 11 P2 và 1 P3.** Dự án có nền tảng tách Ads Contracts/Runtime và Analytics Runtime tốt, nhưng cần xử lý lỗi biên dịch player, tính nhất quán khi phát thưởng IAP và định danh save trước khi xem đây là framework dùng lại cho production.

Đây là đánh giá **working tree hiện tại**, không chỉ commit HEAD. Có nhiều thay đổi và file chưa được commit từ trước. Không sửa source production, scene, prefab, SDK, settings hoặc save người chơi trong đợt quét này.

### Phạm vi và mức bằng chứng

| Phần | Phạm vi |
| --- | --- |
| Kiểm kê tự động | 466 file C# core, 152 file C# game, 13 file C# test; tổng 631 file / 66.361 dòng |
| Asset | Quét tham chiếu script trong 118 asset/scene/prefab ngoài các thư mục SDK/vendor; đối chiếu metadata trong Assets, Packages và Library/PackageCache |
| Đọc sâu | Các đường đi bootstrap/consent, Save, Currency/Reward, IAP, Ads, Analytics, UI, Audio, Daily Reward; kiểm tra cấu trúc Quest, Pool và các test liên quan |
| Kiểm thử logic độc lập | Biên dịch trực tiếp source gốc bằng .NET 9; 10 assertion: **3 đạt, 7 thất bại tái hiện lỗi** |
| Phép thử biên dịch player | Source TutorialHelper gốc, UNITY_ANDROID, không UNITY_EDITOR: **CS0246** |
| Chưa thực hiện | Unity Test Runner, import/compile toàn dự án bằng Unity đúng version, Android/iOS build, IL2CPP, IAP sandbox, consent/ad trên thiết bị |

Project khai báo **Unity 6000.3.12f1**. Không tìm thấy executable đúng version ở các vị trí cài đặt đã kiểm tra; executable tìm được là `D:/Unity/Hub/Editor/6000.6.8f1/Editor/Unity.exe`. Không mở workspace bằng version khác để tránh tự nâng dự án. Các kết quả .NET không thay thế kết quả Unity Mono/IL2CPP.

Unity/storage/SDK trong harness là boundary stub, được ghi rõ trong source. Ví dụ, lỗi phát thưởng được tái hiện qua CurrencyController, Currency, RewardsSet, IAPItem và IAPManager gốc; lưu trữ là memory stub. Không có khẳng định đã thử thanh toán thực hoặc save file Unity.

P1 = cần ưu tiên xử lý trước build/release hoặc chuyển sang nền tảng tái sử dụng. P2 = lỗi có điều kiện kích hoạt cụ thể, cần sửa và bổ sung regression. P3 = dữ liệu dư cần dọn, chưa chứng minh gây hỏng luồng đang hoạt động. Không có P0 được xác nhận trong phạm vi đã kiểm tra.

## Các phát hiện

### AUD-00 — P1 — TutorialHelper làm lỗi biên dịch player

Nguồn: [TutorialHelper.cs:2](../../Assets/Project%20Files/Game/Scripts/Tutorial/TutorialHelper.cs#L2).

`using UnityEditor;` nằm ngoài `#if UNITY_EDITOR`, dù phần sử dụng EditorPrefs/MenuItem bên dưới đã có guard. Khi biên dịch player không có UnityEditor reference, C# báo `CS0246`. File nằm ngoài thư mục Editor và không có asmdef loại trừ player.

**Bằng chứng:** compile probe liên kết file gốc, đặt `UNITY_ANDROID`, nhận lỗi tại dòng 2. Đây là kiểm tra ranh giới compile độc lập; chưa phải log Android build Unity.

**Sửa đề xuất:** guard import UnityEditor; chỉ giữ API runtime của IsTutorialSkipped trong assembly runtime. Xác nhận bằng cả Android và iOS script compilation, không chỉ Editor.

### AUD-01 — P1 — Retry IAP có thể phát thưởng lặp sau lỗi observer của currency

Nguồn: [IAPManager.cs:237](../../Assets/NebulaSoft%20Core/Modules/Monetization/Scripts/Purchasing/IAPManager.cs#L237), [IAPItem.cs:71](../../Assets/NebulaSoft%20Core/Modules/Monetization/Scripts/Purchasing/IAPItem.cs#L71), [Currency.cs:47](../../Assets/NebulaSoft%20Core/Modules/Currency/Scripts/Currency.cs#L47).

TryFulfillPurchase gọi OnProductPurchased trước khi thêm transaction vào fulfilledInMemory và ledger. CurrencyController.Add thay đổi số coin rồi gọi OnCurrencyChanged. Nếu một observer UI ném exception, reward đã thay đổi balance nhưng transaction chưa được đánh dấu, TimesPurchased cũng chưa tăng. Lần retry cùng transaction phát lại phần thưởng đã cấp.

**Tái hiện:** reward +100 coin; observer ném lỗi ở lần đầu; bỏ observer và retry cùng ID. Kết quả **100 → 200 coin**, TimesPurchased vẫn chỉ bằng 1. Ca thường không có exception vẫn deduplicate đúng; baseline đã đạt.

**Sửa đề xuất:** tách mutation/ledger khỏi notification; cô lập từng observer; phát notification sau khi trạng thái thưởng và transaction được commit nhất quán. Cần xử lý cả reward set nhiều phần bị lỗi giữa chừng và lỗi lưu đĩa. Không chỉ bọc toàn bộ reward bằng catch rồi coi là đã thành công.

Unity IAP có thể gửi lại callback fulfillment sau gián đoạn, nên idempotency là yêu cầu thực của luồng này. [Tài liệu Unity IAP](https://docs.unity.com/en-us/iap/purchases).

### AUD-02 — P1 — Save lưu hash 32-bit làm định danh bền vững

Nguồn: [SaveController.cs:76](../../Assets/NebulaSoft%20Core/Modules/Save/Scripts/SaveController.cs#L76), [GlobalSave.cs:74](../../Assets/NebulaSoft%20Core/Modules/Save/Scripts/GlobalSave.cs#L74), [SavedDataContainer.cs:8](../../Assets/NebulaSoft%20Core/Modules/Save/Scripts/SavedDataContainer.cs#L8).

Save key và tên type bị rút xuống String.GetHashCode; container chỉ lưu int hash và tìm theo hash, không giữ string/type để phân biệt collision. Hai key khác nhau có thể trả cùng save object hoặc gặp lỗi ép kiểu. Bên cạnh collision, GetHashCode không có cam kết ổn định qua runtime/platform/version; đổi tên type cũng thay đổi định danh mặc định.

**Tái hiện:** tìm hai chuỗi collision trong process .NET rồi gọi GlobalSave.GetSaveObject gốc: hai key trả **cùng instance**, giá trị của key thứ nhất xuất hiện ở key thứ hai. Cặp chuỗi cụ thể và hash được lưu trong reproductions.json, có thể khác mỗi lần chạy.

Thử hai process .NET riêng cho `currency:0` cũng cho hash khác nhau. Đây là minh họa trên .NET 9, **không phải bằng chứng Unity hiện tại mất save sau mỗi lần mở app**. Rủi ro dài hạn của hợp đồng định danh vẫn tồn tại theo [tài liệu Microsoft](https://learn.microsoft.com/en-us/dotnet/api/system.string.gethashcode).

**Sửa đề xuất:** lưu stable string ID và schema version; tra cứu bằng string equality; có kế hoạch migration từ key/hash legacy cho từng model. Không đổi hash algorithm trực tiếp trên save đã phát hành. Một stable hash 32-bit đơn lẻ cũng chưa giải quyết collision.

### AUD-03 — P2 — IAP deferred giữ khóa mua hàng toàn cục

Nguồn: [UnityIAP5Wrapper.cs:103](../../Assets/NebulaSoft%20Core/Modules/Monetization/Scripts/Purchasing/Wrappers/UnityIAP5Wrapper.cs#L103), [IAPManager.cs:183](../../Assets/NebulaSoft%20Core/Modules/Monetization/Scripts/Purchasing/IAPManager.cs#L183).

**Điều kiện:** bật MODULE_IAP và UNITY_IAP_NEW, store trả OnPurchaseDeferred. Handler chỉ đổi/hide loading UI và ghi log; không ClearPurchaseInProgress, FinishPurchase hoặc chuyển manager sang trạng thái deferred riêng. ActivePurchaseKey tiếp tục có giá trị, chặn mọi BuyProduct/RestorePurchases trong thời gian chờ. Quyết định thanh toán có thể diễn ra rất lâu sau đó hoặc ngoài session.

**Bằng chứng:** lần theo state từ BuyProduct đến handler deferred và các điểm clear; chưa phát deferred order từ sandbox.

**Sửa đề xuất:** tách deferred order khỏi mutex cho thao tác mua hiện tại; giữ thông tin order để nhận callback trả về sau, không phát thưởng/confirm khi chưa được thanh toán. Kiểm thử cả approval và rejection sau khi đã rời màn hình store. [Trạng thái order trong Unity IAP](https://docs.unity.com/en-us/iap/purchases).

### AUD-04 — P2 — Đổi volume làm source đang phát về 0

Nguồn: [AudioSourceCase.cs:30](../../Assets/NebulaSoft%20Core/Modules/Audio/Scripts/AudioSourceCase.cs#L30).

Play nhận tham số clipVolume nhưng không gán vào field cùng tên. OverrideVolume dùng field mặc định 0 để nhân volume mới.

**Tái hiện:** Play với gain 0,8; đổi volume Sound thành 0,5. Kỳ vọng **0,4**, thực tế **0**. Harness dùng AudioSource stub để kiểm tra giá trị; chưa nghe/đo âm thanh trên thiết bị.

**Sửa đề xuất:** lưu gain của clip trong Play và kiểm tra đổi mute/unmute/slider khi source đang phát, kể cả source được tái sử dụng.

### AUD-05 — P2 — WaitForPopupsClose không kết thúc tween và có thể gọi callback mỗi frame

Nguồn: [UIController.cs:516](../../Assets/NebulaSoft%20Core/Modules/UI/Scripts/UIController.cs#L516), [SystemTweenCases.cs:32](../../Assets/NebulaSoft%20Core/Modules/Tween/Scripts/Cases/SystemTweenCases.cs#L32), [Tween.cs:158](../../Assets/NebulaSoft%20Core/Modules/Tween/Scripts/Tween.cs#L158).

DoWaitForCondition tạo tween thời lượng float.MaxValue và gọi callback mỗi update. WaitForPopupsClose chỉ kiểm tra pausePopups.Count rồi gọi callback người dùng; không Complete/Kill tween. Khi điều kiện đúng, callback tiếp tục lặp. Nó còn dùng popupWindows ở kiểm tra đầu nhưng pausePopups ở kiểm tra sau, nên popup không pause có thể còn mở mà callback đã chạy.

**Bằng chứng:** lần theo đầy đủ ba phương thức gốc. Test hiện có chỉ kiểm tra phương thức tồn tại; chưa chạy ca nhiều frame trong Unity. Chưa thấy call site production của API này trong mẫu hiện tại; đây là lỗi hợp đồng API tái sử dụng.

**Sửa đề xuất:** chọn đúng tập popup phải đợi, hoàn tất tween trước khi gọi callback, bảo đảm callback tối đa một lần; xử lý UI owner bị destroy. Regression cần mở popup, đóng, advance nhiều frame và đếm callback.

### AUD-06 — P2 — Analytics không thể retry sau lần khởi tạo thất bại

Nguồn: [AnalyticsService.cs:32](../../Assets/NebulaSoft%20Core/Modules/Analytics/Runtime/AnalyticsService.cs#L32).

InitializeAsync cache initialization task. Khi task kết thúc false do lỗi/cancellation, field vẫn được giữ; mọi lần gọi sau trả lại task thất bại cũ và không gọi provider nữa. Event tiếp tục vào startup queue nhưng không có đường phục hồi trên cùng service.

**Tái hiện:** fake provider lỗi lần đầu, có thể thành công lần sau. Gọi hai lần vẫn **false/false**, provider chỉ được gọi **1 lần**, queue còn 1 event.

**Sửa đề xuất:** phân biệt task đang chạy và task thất bại; cho retry có kiểm soát, định nghĩa ownership/dispose provider và flush queue sau khi ready. Nếu chủ đích là phải tạo lại service, cần public recovery path để AnalyticsModule thực hiện được điều đó.

### AUD-07 — P2 — Tùy chọn TFUA trong UMPTaskBehavior không được truyền vào request

Nguồn: [UMPTaskBehavior.cs:17](../../Assets/NebulaSoft%20Core/Modules/Initializer/Scripts/Loading%20Tasks/UMPTaskBehavior.cs#L17), [UMPTaskBehavior.cs:156](../../Assets/NebulaSoft%20Core/Modules/Initializer/Scripts/Loading%20Tasks/UMPTaskBehavior.cs#L156).

Field umpTagForUnderAgeOfConsent và getter có trong Inspector/API, nhưng GetRequestParameters chỉ tạo request mặc định và gán debug settings. Không có phép gán TagForUnderAgeOfConsent từ field đó. Bật tùy chọn này không thay đổi tham số request của task.

**Phạm vi áp dụng:** UMP task cũ khi được gắn/bật và có MODULE_ADMOB. Không quy kết lỗi tương tự cho provider AdsKit: AdMobConsentFormProvider trong SDK hiện có truyền IsAgeRestrictedUser sang request, và cấu hình framework AdsKit có field ageRestricted riêng. Task UMP cũ không được thấy trong Initializer.prefab hiện tại.

**Sửa đề xuất:** truyền rõ field vào request nếu tiếp tục hỗ trợ task này; hoặc loại bỏ đường consent dư có kế hoạch migration, để chỉ có một nguồn cấu hình.

### AUD-08 — P2 — UMP task cũ bỏ qua refresh khi có cached consent

Nguồn: [UMPTaskBehavior.cs:68](../../Assets/NebulaSoft%20Core/Modules/Initializer/Scripts/Loading%20Tasks/UMPTaskBehavior.cs#L68).

Nhánh CanRequestAds trả true sẽ CompleteTask rồi return trước ConsentInformation.Update. Nếu tái sử dụng task làm consent owner, các session sau có thể không kiểm tra lại yêu cầu consent/privacy options. Hướng dẫn Google yêu cầu Update mỗi lần app khởi động. [Hướng dẫn UMP của Google](https://developers.google.com/admob/unity/privacy).

**Bằng chứng:** source/control flow và đối chiếu tài liệu; chưa thử consent trên thiết bị. Tương tự AUD-07, lỗi thuộc task cũ. Provider AdsKit đang được cấu hình có gọi Update riêng, nên không kết luận luồng AdsKit hiện tại bỏ refresh.

**Sửa đề xuất:** refresh mỗi launch, có thể dùng consent cũ cho ad request khi SDK cho phép nhưng không bỏ refresh; thống nhất một consent owner và regression với cached consent, network error và yêu cầu form mới.

### AUD-09 — P2 — Parser remote config xử lý sai dấu ngoặc trong chuỗi JSON

Nguồn: [RemoteConfigController.cs:56](../../Assets/NebulaSoft%20Core/Modules/Initializer/Scripts/Remote%20Config/RemoteConfigController.cs#L56).

ParseJsonLikeString dùng regex đếm dấu ngoặc mà không hiểu JSON string/escape. Với JSON hợp lệ `{"message":{"text":"a}b","value":1}}`, value trích ra thành `{"text":"a}` và không còn hợp lệ. Những config chuỗi chứa template, JSON lồng dưới dạng string hoặc dấu ngoặc đều có thể bị cắt sai.

**Tái hiện:** gọi private parser gốc bằng reflection rồi kiểm tra đầu ra bằng System.Text.Json, không dùng Newtonsoft stub: assertion thất bại vì JSON đầu ra không hợp lệ.

**Sửa đề xuất:** parse toàn payload bằng JObject/JSON parser, lấy top-level property token và chuyển token thành raw JSON; giữ fallback cho payload cũ nếu hợp đồng hiện tại cho phép định dạng không phải JSON chuẩn. Test thêm escaped quote, brace trong string và nested object.

### AUD-10 — P2 — Daily Reward chưa khóa claim khi có callback lồng nhau

Nguồn: [DailyRewardService.cs:76](../../Assets/Project%20Files/Game/Scripts/Daily%20Reward/DailyRewardService.cs#L76), [DailyRewardModels.cs:64](../../Assets/Project%20Files/Game/Scripts/Daily%20Reward/DailyRewardModels.cs#L64).

TryClaimToday phát thưởng trước khi ghi LastClaimedUtcDateKey. Phát currency lại gọi observer đồng bộ. Observer có thể gọi trở lại TryClaimToday trong khi state vẫn claimable; cả hai call đều thành công. Không có guard reentrancy.

**Tái hiện:** callback currency gọi lại claim đúng một lần. Kết quả **outer=true, nested=true, coin=20** cho phần thưởng 10 coin/ngày. Hai call bình thường nối tiếp sau khi call trước đã trả về vẫn bị chặn đúng; baseline đã đạt. Chưa tìm thấy observer hiện tại tự gọi claim: đây là lỗi khi mở rộng tích hợp, không phải khẳng định nút CLAIM mẫu tự cấp đôi.

**Sửa đề xuất:** guard in-progress và cập nhật reward/state trong một domain transaction, notification sau commit; có rollback/retry rõ khi grant hoặc persist thất bại. Không chỉ đổi ngày claim trước rồi bỏ mặc trường hợp phát thưởng lỗi.

### AUD-11 — P2 — Đọc Daily Reward sau ngày bị bỏ lỡ gây ghi save lặp

Nguồn: [DailyRewardService.cs:128](../../Assets/Project%20Files/Game/Scripts/Daily%20Reward/DailyRewardService.cs#L128), [DailyRewardModels.cs:154](../../Assets/Project%20Files/Game/Scripts/Daily%20Reward/DailyRewardModels.cs#L154).

Khi elapsedDays > 1, NormalizeForCurrentDate luôn gán NextDayIndex=0 rồi Save, kể cả đã bằng 0 sau lần normalize trước. LastClaimedUtcDateKey vẫn là ngày cũ, nên mỗi GetState tiếp tục Save. LocalDailyRewardStateStore chuyển việc đó thành forced synchronous write.

**Tái hiện:** sau lần normalize ban đầu, gọi GetState năm lần: **thêm 5 write**, dù state không đổi. UI có gọi GetState ở các bước mở/refresh/claim; không khẳng định hiện tại gọi mỗi frame.

**Sửa đề xuất:** chỉ persist nếu state thực sự đổi; có thể theo dõi epoch đã normalize nếu cần. Regression bảo đảm GetState ổn định không ghi đĩa và behavior reset chu kỳ vẫn đúng.

### AUD-12 — P2 — Core còn phụ thuộc trực tiếp vào lớp của game mẫu

Nguồn: [AdsRewardsHolder.cs:42](../../Assets/NebulaSoft%20Core/Modules/Monetization/Scripts/Advertisement/AdsRewardsHolder.cs#L42), [IAPManager.cs:122](../../Assets/NebulaSoft%20Core/Modules/Monetization/Scripts/Purchasing/IAPManager.cs#L122), [IAPManager.cs:276](../../Assets/NebulaSoft%20Core/Modules/Monetization/Scripts/Purchasing/IAPManager.cs#L276).

AdsRewardsHolder trong core gọi LeaderboardBottomNavigationController; IAPManager gọi FirebaseIAPTransactionLogger và FirebaseNoAdsEntitlementSync, trong khi các lớp đó được định nghĩa ở Project Files. Guard FIREBASE trong implementation game chỉ làm chúng no-op; không xóa phụ thuộc compile từ core sang game.

**Hệ quả:** xóa/thay game mẫu rồi chỉ mang core sang dự án khác sẽ thiếu type. Phần lớn runtime vẫn nằm chung predefined Assembly-CSharp, nên compiler chưa bảo vệ ranh giới core/game. Ads Contracts/Runtime và Analytics Runtime đã có asmdef riêng, là phần có thể giữ làm nền để tách tiếp. [Assembly trong Unity 6000.3](https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definitions-intro.html).

**Sửa đề xuất:** đưa network popup policy và Firebase bridge về composition layer của game; core công bố interface/event hoặc injection point. Sau khi đảo phụ thuộc, thêm assembly/package boundary và một host tối thiểu không chứa Project Files để xác nhận core import/compile độc lập. Chưa thực hiện tách package trong đợt đánh giá này.

### AUD-13 — P3 — Hai dữ liệu framework/game còn script GUID không tồn tại

Nguồn: [Project Init Settings.asset:244](../../Assets/Project%20Files/Data/Project%20Init%20Settings.asset#L244), [Level Skin Database.asset:12](../../Assets/Project%20Files/Data/Skins/Level%20Skin%20Database.asset#L12).

Sau khi đối chiếu cả PackageCache, còn script GUID không resolve cho subasset NebulaSoft.AnalyticsHelperInitModule và asset Level Skin Database. Không tìm thấy các class tương ứng trong source hiện tại. Scanner cũng ghi nhận Readme.asset có GUID unresolved; giữ ở danh sách triage, không gán thành lỗi gameplay.

**Giới hạn tác động:** subasset AnalyticsHelper không nằm trong modules array hiện tại. Chưa thấy runtime consumer của Level Skin Database. Vì vậy không khẳng định chúng làm bootstrap fail. Các GUID thuộc UI/InputSystem/URP đã resolve qua PackageCache, không bị đưa nhầm vào phát hiện này. Không thấy duplicate local GUID.

**Sửa đề xuất:** kiểm tra asset trong Unity đúng version, migrate hoặc xóa orphan có chủ đích sau khi xác nhận không có consumer. Không tự gán một script khác chỉ để hết Missing Script.

### AUD-14 — P2 — AdsKit không chuyển tiếp impression sang analytics contract

Nguồn: [AdsKitProvider.cs:214](../../Assets/NebulaSoft%20Core/Modules/Ads/Providers/AdsKit/AdsKitProvider.cs#L214), [AnalyticsBridge.cs:38](../../Assets/NebulaSoft%20Core/Modules/Analytics/Unity/AnalyticsBridge.cs#L38).

Provider thực thi OnAdImpressionRecorded bằng body rỗng; không có đường Emit AdEventKind.Impression trong provider. AnalyticsBridge có lắng nghe kind này và chuyển sang ad_impression_custom, nên chỉ số custom này không nhận dữ liệu từ AdsKit mặc dù SDK có callback impression.

**Bằng chứng:** source của framework và interface/callback dispatch trong SDK đã cài. Không suy diễn thành mất báo cáo doanh thu/impression tự động của AdMob/Firebase: đây là khoảng trống của contract/event custom trong framework.

**Sửa đề xuất:** giữ mapping impression về request/banner đang hợp lệ rồi emit kind tương ứng; deduplicate, xử lý callback trễ và tránh gán nhầm impression cho request kế tiếp. Kiểm thử riêng event custom với SDK adapter và trên thiết bị.

## Đánh giá kiến trúc theo mục tiêu tái sử dụng

| Khía cạnh | Đánh giá hiện tại | Việc cần làm cụ thể |
| --- | --- | --- |
| Core không lệ thuộc gameplay | Chưa đạt hoàn toàn; AUD-12 có ví dụ compile dependency cụ thể | Đảo các phụ thuộc vào game; test host không có game mẫu |
| Module dễ ghép cho team nhỏ | InitModule và settings theo thứ tự dễ hiểu, đã có tài liệu | Thêm validation thiếu dependency/thứ tự và lỗi config trước build; chưa cần DI container lớn |
| Ads/Analytics tách SDK | Contracts/runtime độc lập là nền tốt | Hoàn thiện retry/event contract, kiểm thử adapter thật |
| Save/update version | Đã có snapshot và write coordinator; test về ordering/failure là hữu ích | Stable identity, schema/migration, fixture nâng version, recovery save lỗi; không thay storage format vội |
| Economy/reward | Có domain API chung nhưng mutation còn trộn notification | Transaction/idempotency; notification không làm hỏng domain mutation |
| Package/versioning | Source đang tổ chức theo thư mục, nhiều runtime chung Assembly-CSharp | Tách assembly theo ranh giới đã ổn định; UPM package/version/changelog sau khi standalone host đạt |
| Junior onboarding | Guide và module registration rõ | Mẫu module tối thiểu + dependency check + test case cho lỗi cấu hình thường gặp |
| Chứng nhận mobile production | Chưa có bằng chứng trong đợt này | Unity version đúng, Android/iOS/IL2CPP, store sandbox và device lifecycle |

Quest có code kiểm tra save/init và rotation tests; Save có regression về snapshot/ordering; Ads có test reward/cancel/late callback. Đây là các điểm kiểm soát hiện hữu, **không phải kết luận toàn bộ test đã pass trong phiên này**. Pool, Tween, Haptic, Skins, Firebase profile/cloud và toàn bộ vendor SDK chưa được kiểm chứng exhaustively hoặc trên thiết bị.

## Thứ tự xử lý và tiêu chí xác nhận

1. **Player build và tính đúng của dữ liệu:** AUD-00, AUD-01, AUD-02. Probe compile hết lỗi; IAP retry không cộng dư; có thiết kế migration/fixture trước khi đổi save identity.
2. **Runtime APIs đang dùng và retention:** AUD-04, AUD-06, AUD-09, AUD-10, AUD-11, AUD-14. Bảy assertion lỗi trong harness phải đạt sau sửa; bổ sung test Unity tương ứng cho boundary engine/SDK.
3. **Tích hợp theo cấu hình:** AUD-03, AUD-05, AUD-07, AUD-08. Test deferred sandbox; callback popup đúng một lần; consent task/cấu hình có một owner rõ ràng. Task/API chưa dùng hiện tại vẫn cần sửa hoặc đánh dấu ngừng hỗ trợ trước khi quảng bá là module tái sử dụng.
4. **Đóng gói framework:** AUD-12 rồi AUD-13. Core compile trong host tối thiểu không có game mẫu; clean asset scan không còn script local orphan có tác dụng runtime.

Không cần refactor toàn dự án cùng lúc. Giữ public API cũ nếu có người dùng; sửa bằng regression nhỏ, tách adapter/composition có chủ đích, rồi mới phát hành version mới.

## Công cụ và bằng chứng chạy lại

- [Hướng dẫn diagnostic](../../tools/ModuleAudit/README.md)
- [Source harness](../../tools/ModuleAudit/Program.cs), [boundary stubs](../../tools/ModuleAudit/BoundaryStubs.cs), [project liên kết source gốc](../../tools/ModuleAudit/ModuleAudit.csproj)
- [Kết quả 10 assertion](../../output/audit/2026-11-07/reproductions.json)
- [Kiểm kê và fingerprint source](../../output/audit/2026-11-07/inventory.json)
- [Player compile probe](../../tools/PlayerCompileProbe/PlayerCompileProbe.csproj)

```powershell
python tools/ModuleAudit/scan_project.py --output output/audit/2026-11-07/inventory.json
dotnet run --project tools/ModuleAudit/ModuleAudit.csproj -- --output output/audit/2026-11-07/reproductions.json
dotnet build tools/PlayerCompileProbe/PlayerCompileProbe.csproj --nologo -v minimal
```

Ở trạng thái source được đánh giá, harness và compile probe đều exit `1` vì tái hiện lỗi. Đây là bằng chứng chẩn đoán, không phải check CI đã xanh. Thông tin source SHA-256 trong inventory dùng để đối chiếu kết quả với đúng working tree.

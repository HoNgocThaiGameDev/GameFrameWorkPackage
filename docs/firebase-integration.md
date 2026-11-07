# Firebase: ranh giới SDK và nâng phiên bản

Framework giữ contract và schema dữ liệu riêng; SDK nằm sau adapter. Nâng SDK giữ API game khi adapter còn thực hiện được contract. Khi Firebase đổi API hoặc yêu cầu native, cập nhật adapter/cấu hình tích hợp tương ứng. Không bảo đảm mọi SDK tương lai sẽ tương thích mà không kiểm tra. Firebase từng thay kiểu kết quả Auth trong [release notes chính thức](https://firebase.google.com/support/release-notes/unity).

## Cấu trúc dependency

```text
Game / feature ────────────→ NebulaSoft.Cloud.Runtime
                              ICloudBackend
                              ICloudAuthProvider / CloudUser
                              ICloudDocumentStore / ICloudTransaction
                              CloudDocumentSnapshot / CloudException

Analytics service ─────────→ NebulaSoft.Analytics.Runtime
                              IAnalyticsProvider / AnalyticsEvent

NebulaSoft.Firebase ───────→ hai assembly contract trên + Firebase SDK
  FirebaseDependencies         dependency check dùng chung
  FirebaseCloudBackend         Auth adapter và composition Firestore
  FirebaseDocumentStore        snapshot, timestamp, merge, transaction, error mapping
  FirebaseProvider             Analytics adapter
```

- [Cloud runtime](../Assets/NebulaSoft%20Core/Modules/Cloud/Runtime) không tham chiếu Unity/Firebase. Test được bằng backend fake mà không import SDK.
- [SDK adapter](../Assets/NebulaSoft%20Core/Integrations/Firebase/Runtime) thuộc assembly `NebulaSoft.Firebase`. Không dùng reflection/dynamic dò API lúc chạy; compiler chỉ ra phần adapter cần sửa trước khi build IL2CPP.
- Game giữ đăng nhập, profile, tên người chơi, IAP log và entitlement tại `Game/Integration/Firebase/Runtime`. Logic này dùng port của framework, không dùng FirebaseUser, AuthResult, DocumentSnapshot, Timestamp hoặc số error code SDK.
- `FirebaseBackendFactory` là điểm tạo Cloud và Analytics provider. Nó sở hữu các define và chỉ trả interface của framework. Settings và Inspector đọc cùng factory để biết adapter có bật hay chưa.
- Các property native `FirebaseAuthHandler.Auth`, `.Firestore` và overload claim tên nhận native handle đã được bỏ. Dùng `Authentication`, `Documents` và overload nhận `ICloudDocumentStore`. Custom code từng khởi tạo `FirebaseProvider` trực tiếp dùng `FirebaseBackendFactory.CreateAnalytics()` hoặc factory asset.
- Remote Config hiện dùng HTTPS/cache/bundled JSON, không gọi Firebase Remote Config. Thay SDK không đổi đường tải config này.

`FIREBASE` bật Auth/Firestore, `FIREBASE_ANALYTICS` bật Analytics độc lập. Chỉ bật define khi SDK tương ứng đã có. Không có define thì core vẫn biên dịch, Firebase backend factory trả null và Console Analytics vẫn dùng được. Framework tiếp tục được quản lý trong Assets.

## Khởi tạo và lifetime

Inject backend qua `CloudServices.Configure(backend)` trước lần dùng đầu tiên. Flow mẫu tự lấy backend từ `FirebaseBackendFactory` khi chưa có backend; không ghi đè backend đã inject. Game riêng có thể configure và shutdown trong InitModule sở hữu backend.

`CloudServices.EnsureInitializedAsync()` chia sẻ một lượt giữa nhiều caller, timeout mặc định 30 giây; có thể truyền TimeSpan cho lượt mới. Lượt lỗi được retry, đọc `LastInitializationError` để biết nguyên nhân. Shutdown/reconfigure trả false cho caller cũ, dispose adapter một lần và bỏ kết quả initialization tới muộn. Gọi API composition trên Unity main thread.

`FirebaseDependencies` chia sẻ dependency check native giữa Analytics và Cloud, cache thành công và cho thử lại sau lỗi. Caller cancel được ngừng chờ; không khởi động chồng dependency operation native đang chạy.

Adapter không dispose singleton Firebase Auth/Firestore mà Analytics hoặc tính năng khác còn dùng. FirebaseAuthHandler reset session và shutdown CloudServices ở SubsystemRegistration, kể cả khi tắt domain reload. Port không chuyển native handle sang game.

Timeout không hủy native request nếu SDK không hỗ trợ. Một write có thể hoàn tất muộn hoặc đã commit. Giữ kiểm tra UID/session sau await; xác nhận bằng cùng transaction/document ID. Không tự retry mutation thiếu idempotency. Timeout 15 giây hiện có của profile vẫn áp dụng; các thao tác khác giữ chính sách SDK/caller hiện hành.

## Contract Auth

| Port | Trách nhiệm |
| --- | --- |
| CurrentUser | Snapshot CloudUser gồm UserId/IsAnonymous, không giữ native handle. |
| SignInAnonymouslyAsync | Trả identity sau khi SDK đăng nhập. |
| LinkFacebookAsync(token) | Link user hiện tại; báo CloudError.CredentialAlreadyInUse khi credential thuộc user khác. |
| SignInFacebookAsync(token) | Đăng nhập tài khoản của credential khi game quyết định xử lý conflict. |
| SignOut | Sign-out backend; game quản lý Local/Cloud selection và anonymous fallback. |

Port bao phủ anonymous/Facebook đang dùng. Khi cần Google/Apple/email, bổ sung contract identity và adapter theo nhu cầu thật; không đưa Credential của SDK vào game API. Adapter chụp SDK result thành CloudUser ngay khi hoàn tất, giữ result sống trong lúc chụp. Game phân loại credential conflict bằng CloudError, không dò chuỗi thông báo lỗi.

## Document và dữ liệu cũ

ICloudDocumentStore cung cấp read, set, transaction. CloudDocumentReference chỉ giữ store/path; `Collection(...).Document(...)` không tạo native reference hay thay collection ID.

| Framework | Mapping Firebase |
| --- | --- |
| CloudReadSource.Server | Source.Server; không tự chuyển sang cache khi quyết định Local/Cloud. |
| CloudWriteMode.Replace | Set document như SetAsync mặc định trước đây. |
| CloudWriteMode.Merge | SetOptions.MergeAll, giữ field/map ngoài update. |
| CloudFieldValue.ServerTimestamp | FieldValue.ServerTimestamp; không dùng giờ thiết bị thay thế. |
| CloudDocumentSnapshot | Copy map/list/byte array; field null khác field không tồn tại. |
| Timestamp trong snapshot | DateTime UTC, kể cả map/list lồng nhau. |
| CloudException.Error | Timeout, PermissionDenied, Offline, Authentication… thay số code native trong game. |

Data đọc hỗ trợ null, string, bool, int/long, float/double, DateTime, byte array và map/list của các kiểu này. Native type ngoài contract báo InvalidData, không tự bỏ field rồi ghi đè. Nếu cần GeoPoint/reference native, thêm DTO và mapping hai chiều có test trước khi dùng. Model Save vẫn là JSON versioned; thêm field không yêu cầu sửa uploader cho từng field.

Transaction callback có thể chạy nhiều lần khi contention. Đọc trước khi ghi; không grant reward, sửa balance local, phát analytics hoặc cập nhật UI trong callback. Adapter giữ cùng native transaction và từ chối document thuộc store khác.

Giữ nguyên đường dẫn Players, PlayerNames, Transactions, IAPAttempts, NoAdsEntitlements, document key, schemaVersion, saveVersion và payload model. Name claim, kiểm tra cloud version và ghi player tiếp tục cùng transaction. Merge giữ field game cũ; client cũ bị chặn khi cloud version mới hơn. Integer cloud được clamp trước khi cast int để tránh wrap. Migration dữ liệu thuộc [Save schema](save-module.md), độc lập version SDK.

## Thay hoặc thêm adapter

1. Cài ICloudBackend với Auth/Documents hoặc IAnalyticsProvider cho event transport, giữ contract đang dùng.
2. Mapping identity, result, timestamp, lỗi và parameter ngay trong adapter. Không trả object chứa native handle qua port để né compiler.
3. Configure backend từ composition/InitModule của game. Analytics dùng [provider factory asset](analytics-module.md#thêm-event-riêng-và-provider-mới).
4. Test game bằng fake port; test adapter với SDK thật. Không thay schema save để giải quyết thay đổi chữ ký API SDK.

## Nâng Firebase SDK

1. Đọc [release notes](https://firebase.google.com/support/release-notes/unity). Nâng các sản phẩm Firebase và dependency cùng phiên bản theo [troubleshooting chính thức](https://firebase.google.com/docs/unity/troubleshooting-faq).
2. Dùng file cấu hình đúng app/project ID, define cần dùng và schema cloud. Import managed DLL và native library từ cùng bộ SDK rồi kiểm tra dependency resolver. Sáu thư viện native 13.4.0 rời đã được đưa ra khỏi `Assets` để lần import mới lấy native library từ bộ SDK bạn chọn.
3. Đợi Unity compile, chạy CloudModuleTests, AnalyticsModuleTests, Save/IAP và toàn bộ EditMode suite.
4. Chạy công cụ managed compile bên dưới với DLL release muốn dùng. Cập nhật code trong assembly `NebulaSoft.Firebase` nếu SDK đổi API. Công cụ kiểm tra game và Editor bằng cách biên dịch chúng chỉ với assembly adapter, bỏ toàn bộ DLL Firebase khỏi tham chiếu của hai assembly đó.
5. Người phụ trách build kiểm tra Android IL2CPP/iOS và sandbox: anonymous/link/conflict/logout, Local/Cloud selection, save cũ, transaction retry, IAP/entitlement, DebugView, offline/background/kill/resume.

```powershell
python tools/FirebaseCompatibility/check_firebase_compatibility.py --sdk-dir Library/FirebaseCompatibility/13.4.0 --sdk-dir Library/FirebaseCompatibility/14.1.0
```

Đặt managed DLL của mỗi release trong thư mục riêng ngoài Assets. Công cụ dùng source/reference của Unity Editor hiện tại để compile adapter, game và Editor theo ba cấu hình `all`, `analytics-only` và `cloud-only`. Artifact, log và report nằm trong `Library/FirebaseCompatibilityChecks`. Thêm release khác bằng `--sdk-dir`. Xem [README công cụ](../tools/FirebaseCompatibility/README.md).

## Kiểm chứng

SDK 13.4.0 và 14.1.0 pass cả ba cấu hình, tổng cộng **18/18 lần biên dịch managed**. Trong đó 12 lần biên dịch game và Editor dùng assembly adapter mà không tham chiếu DLL Firebase. Unity đạt **244/244 EditMode test**, gồm 19 Cloud và 39 Analytics test, cùng **10/10 ModuleAudit assertion**. Các test bao phủ khởi tạo, Auth conflict, merge, chặn ghi đè cloud version mới, giá trị cloud cũ và hai cách cấu hình Firebase Analytics. Xác nhận native, rules và gửi event thực tế thuộc bước kiểm tra trên bản build thiết bị.

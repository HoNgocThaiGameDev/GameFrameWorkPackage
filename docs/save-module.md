# Save, version và migration

Framework 1.4 giữ file `save` và các save ID hiện có. File native mới dùng JSON có version. Reader đọc được fixture BinaryFormatter của bản cũ và chuyển sang JSON ở lần ghi thành công kế tiếp. Bản backup giữ lần ghi hợp lệ trước đó. WebGL tiếp tục qua wrapper lưu JSON hiện có.

## Các cấp version

| Cấp | Giá trị hiện tại | Trách nhiệm |
| --- | --- | --- |
| Native file `format/version` | `NebulaSoft.Save` / `1` | Envelope lưu trữ và metadata |
| Global save `schemaVersion` | `2` | Định danh entry bằng string, tương thích hash legacy |
| Entry `modelVersion` | Theo `SaveModelAttribute`, mặc định `0` | Schema của từng nhóm state |
| Firebase progress `saveVersion` | `4` | Các field progress legacy và payload mở rộng |
| Firebase `frameworkSave.version` | `1` | Envelope đồng bộ các model đã bật CloudSync |

Version của các model độc lập. Một thay đổi inventory không buộc phải migrate toàn bộ wallet, lives hay quest.

## Thêm nhóm dữ liệu

```csharp
[System.Serializable, SaveModel("game.inventory", 0, CloudSync = true)]
public sealed class InventorySave : ISaveObject
{
    public int Capacity = 20;
    public int Stars;
    public void Flush() { }
}

var inventory = SaveController.GetSaveObject<InventorySave>();
inventory.Stars += 1;
SaveController.MarkAsSaveIsRequired();
```

ID thuộc về dữ liệu và cần được giữ ổn định khi đổi tên class. `CloudSync = true` đưa model vào upload/download progress hiện có. Model local-only giữ `CloudSync` mặc định false. Thêm một model hoặc field mới qua cách này không cần sửa serializer hay Firebase uploader. Model phải được truy cập/khởi tạo trước khi xuất cloud snapshot.

Field bổ sung nhận giá trị khởi tạo từ constructor trước khi JSON cũ được áp dụng. Field dùng public hoặc `[SerializeField]` theo quy tắc JsonUtility. Dictionary/runtime cache cần được chuyển sang field serializable trong `Flush()`.

## Migration có thay đổi cấu trúc

Khi đổi ý nghĩa dữ liệu, rename field hoặc thay representation, tăng version của model và đăng ký từng bước trước lần `GetSaveObject` đầu tiên.

```csharp
// Cập nhật attribute của InventorySave thành version 1.
SaveSchema.RegisterMigration<InventorySave>(0, json =>
{
    json["Stars"] = json["OldStars"] ?? 0;
    json.Remove("OldStars");
});
```

Đăng ký một lần trong startup của game. Migration chạy `0 -> 1 -> 2` trên bản JSON riêng. Thiếu bước hoặc gặp version mới hơn sẽ throw trước khi publish model. Dữ liệu gốc còn nguyên để sửa migration và thử lại. Một field additive thông thường có thể dùng constructor default mà không tăng version. Khi thêm attribute vào model trước đây dùng tên type làm key, framework nhận lại key tên type cũ. Các key explicit vẫn cần giữ tên đã phát hành.

Field JSON chưa nhận diện được giữ khi model ghi lại. Với collection object, dùng ID duy nhất như `id`, `Id`, `ID`, `QuestId` hoặc composite `*Id`/`*Key` để field bổ sung đi theo đúng phần tử khi reorder. Mảng không có ID chỉ được ghép theo vị trí khi số phần tử giữ nguyên. Đổi cấu trúc hoặc ý nghĩa collection cần migration rõ ràng. Xóa phần tử qua model được coi là thao tác dữ liệu có chủ đích.

Các entry của feature chưa bật vẫn nằm trong save và đi qua lần đọc/ghi. ID/hash legacy từng bị collision hoặc hash từ runtime khác cần mapping từ bản đã phát hành bằng `GetSaveObject<T>(key, legacyHash)`.

## Firebase

Progress tiếp tục dùng collection `Players` và giữ các field hiện hữu như `coinBalance`, `powerUps`, `maxReachedLevelIndex`. Các model mới nằm dưới `frameworkSave.models`, mỗi entry có `key`, `version`, `json`. Map key được encode để không phụ thuộc ký tự trong save ID.

Upload dùng `CloudWriteMode.Merge`, được adapter ánh xạ sang `SetOptions.MergeAll`, trong transaction đang claim/write player. Transaction đọc cloud version trước khi ghi và từ chối downgrade envelope/model. Unknown cloud model không nằm trong payload update sẽ được giữ trên server. Payload model rỗng được bỏ khỏi update để tránh thay map đang có trên Firestore. [Quy tắc merge map của Firebase](https://firebase.google.com/docs/firestore/manage-data/add-data). Download đưa các model qua cùng migration pipeline với local. Unknown model được giữ nguyên trong local save, nhưng chỉ model đang được client nhận diện và bật CloudSync mới được upload lại. Chọn Local/Cloud cũng so sánh payload mở rộng.

Currency, Skins, Daily Reward, Quest và Lives đã bật đồng bộ model. Profile và IAP/entitlement tiếp tục dùng adapter chuyên trách hiện có. Các API kiểm tra và transformation payload được test độc lập với Firebase SDK. Game dùng CloudDocumentSnapshot/CloudFieldValue qua [SDK adapter](firebase-integration.md), giữ nguyên schema và đường dẫn dữ liệu. Phiên kiểm tra hiện tại không có Firebase SDK/define FIREBASE nên cần xác nhận transaction và rule bằng emulator hoặc sandbox khi tích hợp SDK.

Với field mới quan trọng và nhiều bản game cùng ghi cloud, tăng version model kèm migration. Cùng version thì bản cũ vẫn có thể gửi chuỗi JSON thiếu field mới. `MergeAll` áp dụng ở cấp field Firestore, nên toàn bộ chuỗi model trong lần ghi có thể được thay thế. Xem ví dụ thêm field và ca kiểm tra rollout ở [sổ tay Save](modules/data.md#thêm-field-hoặc-migration).

## Ghi bền vững và phục hồi

- `Save()` chụp snapshot trên main thread rồi xếp IO vào queue khi wrapper hỗ trợ. `SaveChecked()` chờ IO và throw nếu ghi thất bại.
- Writer ghi `.pending`, flush rồi replace. Primary hợp lệ trước đó trở thành `.bak`. Khi phục hồi từ backup, lần sửa primary giữ nguyên backup tốt.
- File chưa có tạo state mới. Primary lỗi dùng backup. Nếu cả hai lỗi, ghi bị khóa và hai file được giữ nguyên. `Serializer.IsWriteBlocked()` hoặc `SaveController.IsWriteBlocked` cho biết trạng thái này.
- Envelope mới hơn client bị từ chối, bao gồm trường hợp có backup cũ. Reset/xóa save phải là thao tác explicit.
- Pause/quit flush đồng bộ. `Shutdown()` chờ save rồi dừng receiver/coroutine. Mutation và Flush chạy main thread.
- Custom wrapper phải báo lỗi ghi cho caller và hoàn tất IO trước khi trả về. WebGL bridge hiện có chưa cung cấp acknowledgement ghi bền vững.

## Kiểm thử

`SaveModuleTests`, `SaveMigrationTests` và `IAPModuleTests` kiểm tra fixture save cũ, migration nhiều bước, giữ field/model chưa nhận diện, constructor defaults, backup/repair, version downgrade, cloud merge policy, ordered writes, dirty state và IAP retry. File native mới dành cho nâng cấp tiến tới, các build cũ chỉ có BinaryFormatter reader cần được xét riêng trong chính sách downgrade của game.

Custom Reward cần Validate trước khi mutation và ApplyReward phải hoàn tất một bước hoặc giữ nguyên state khi lỗi. CurrencyReward dùng batch đã kiểm tra trước, IAP trì hoãn currency observer đến sau checkpoint ghi bền vững.

# Dữ liệu, kinh tế và thưởng

[Sổ tay module](README.md)

## Save

`SaveController` là API cho game. `GlobalSave` quản lý model theo key. `SavedDataContainer` giữ JSON của từng model, `SaveSchema` chuyển version, `Serializer` và wrapper đọc/ghi file. Phần bootstrap ở `Initializer/Runtime/Modules/SaveInitModule.cs`. Preset và công cụ build WebGL ở `Integration/Save`. [Runtime](../../Assets/NebulaSoft%20Core/Modules/Save/Runtime) · [SaveInitModule](../../Assets/NebulaSoft%20Core/Modules/Initializer/Runtime/Modules/SaveInitModule.cs) · [Integration](../../Assets/NebulaSoft%20Core/Integration/Save)

1. Đặt **Save Controller** trước các module đọc dữ liệu. Đặt `cleanSaveStart` là false cho flow giữ progress. `autoSaveDelay = 0` tắt autosave định kỳ.
2. Định nghĩa model `[Serializable]` và `ISaveObject` trong feature sở hữu dữ liệu. Dùng field public hoặc `[SerializeField]`. Dùng `Flush()` để copy runtime cache vào field serializable khi có cache riêng.
3. Đọc model sau khi Save đã init. Sau khi đổi state, gọi `MarkAsSaveIsRequired()`. `Save()` lên lịch ghi khi dirty. `SaveChecked()` ghi đồng bộ và báo lỗi cho caller, phù hợp với điểm xác nhận giao dịch cần biết IO thành công.

Ví dụ model trong game và phần gọi từ controller sau startup.

```csharp
using System;
using NebulaSoft;

[Serializable, SaveModel("game.inventory", 0, CloudSync = true)]
public sealed class InventorySave : ISaveObject
{
    public int stars;
    public int dailyTickets = 3;

    public void Flush() { }
}
```

```csharp
var inventory = SaveController.GetSaveObject<InventorySave>();
inventory.stars++;
SaveController.MarkAsSaveIsRequired();
```

### Thêm field hoặc migration

| Thay đổi | Cách triển khai |
| --- | --- |
| Thêm field mặc định như `dailyTickets = 3` | Giữ key và version hiện tại cho nâng cấp local đơn giản. Model được tạo trước khi JSON cũ áp vào, nên field thiếu dùng giá trị khởi tạo. |
| Đổi tên, kiểu hoặc ý nghĩa của field | Tăng `modelVersion` của model. Đăng ký từng bước liên tiếp trước lần đọc đầu tiên. |
| Thêm model | Đặt một `SaveModel` ID duy nhất và gọi `GetSaveObject<T>()` sau Save init. Bật `CloudSync` nếu model thuộc progress cần đồng bộ. |
| Thêm field cloud quan trọng khi nhiều bản game cùng ghi | Dùng version model mới và migration để chặn bản cũ ghi đè sau khi cloud nhận version mới. |

Ví dụ nâng `InventorySave` từ version 0 lên 1. Đổi số version trong attribute thành `1`, rồi đăng ký bước dưới đây một lần ở startup trước khi module nào gọi `GetSaveObject<InventorySave>()`. Nếu JSON đã chứa field thì migration giữ giá trị đó.

```csharp
SaveSchema.RegisterMigration<InventorySave>(0, json =>
{
    if (json["dailyTickets"] == null)
        json["dailyTickets"] = 3;
});
```

Giữ key đã phát hành của model dùng overload `GetSaveObject<T>("key")`. Field chưa được client nhận diện được ghép lại khi ghi. Dùng ID ổn định, duy nhất cho phần tử collection cần giữ field lạ qua reorder. Với mảng không có ID, merge theo vị trí chỉ áp dụng khi độ dài giữ nguyên. Model version cao hơn client hoặc thiếu bước migration làm lần đọc model thất bại để tránh ghi đè state chưa hiểu. [SaveSchema](../../Assets/NebulaSoft%20Core/Modules/Save/Runtime/SaveSchema.cs) · [Save chuyên sâu](../save-module.md)

### Restore, backup và cloud

Native save ghi `.pending`, flush rồi thay file chính, giữ `.bak` từ lần ghi hợp lệ trước. Save cũ định dạng binary được đọc qua compatibility reader. Save hỏng cả primary và backup bị khóa ghi. WebGL dùng wrapper riêng, với giới hạn acknowledgement được mô tả trong tài liệu Save.

Cloud export lấy model đã được khởi tạo và bật `CloudSync`. Firebase lưu model dưới `frameworkSave.models` với key, version và chuỗi JSON. Local restore từ cloud cũ dùng default khi tạo model mới. Với model đang sống, restore có thể giữ field thiếu từ snapshot JSON đã flush, nên phải kiểm tra riêng trường hợp còn mutation trong bộ nhớ. Payload cùng version nhưng thiếu field mới vẫn có thể ghi đè chuỗi model trên cloud. So sánh hiện tại cũng có thể báo conflict khi hai payload chỉ khác field mặc định. [GlobalSave](../../Assets/NebulaSoft%20Core/Modules/Save/Runtime/GlobalSave.cs) · [SaveCloudData](../../Assets/NebulaSoft%20Core/Modules/Save/Runtime/SaveCloudData.cs) · [Firebase progress](services.md#firebase-progress)

**Kiểm tra.** Dùng `SaveModuleTests` và `SaveMigrationTests`. Với model mới, thử save cũ thiếu field, bản ghi chứa field lạ, save hỏng cần backup và migration thiếu bước. Với cloud, thử thứ tự upload bản cũ/bản mới bằng môi trường test.

## RemoteConfig

`RemoteConfigInitModule` tải JSON trong `Prepare` và cài bundled defaults. `RemoteConfigController` parse snapshot và đọc theo key. Model config kế thừa `RemoteConfigData` và có `Validate()` để từ chối giá trị ngoài phạm vi. [Runtime RemoteConfig](../../Assets/NebulaSoft%20Core/Modules/RemoteConfig/Runtime)

1. Thêm **Remote Config** trong registry. Gắn bundled TextAsset, ví dụ `Remote Config Defaults.json`. Endpoint là URL HTTPS trực tiếp nếu game dùng fetch từ xa.
2. Viết model config trong thư mục game, đặt `Key` ổn định và validate dữ liệu đủ dùng. Đọc trong `CreateComponent` hoặc lúc tạo session sau startup. Với `TryGetConfig<T>`, khi key remote thiếu hoặc deserialize/validate thất bại, controller thử bundled key.
3. Giữ fallback ở consumer nếu cả hai nguồn đều thiếu. Có thể lấy snapshot khi bắt đầu ván để tuning trong ván ổn định.

```csharp
using NebulaSoft;

[System.Serializable]
[UnityEngine.Scripting.Preserve]
public sealed class BoardTuning : RemoteConfigData
{
    public override string Key => "board";
    public int moveLimit = 30;

    public override bool Validate() => moveLimit > 0;
}
```

Thêm `"board": { "moveLimit": 30 }` vào JSON object và đọc `RemoteConfigController.TryGetConfig<BoardTuning>("board")?.moveLimit ?? 30`. Validator cần kiểm tra giá trị có ý nghĩa, vì field thiếu trong JSON có thể nhận mặc định của model. [Bundled config mẫu](../../Assets/Project%20Files/Data/Remote%20Config%20Defaults.json)

**Kiểm tra.** Chạy offline, payload sai và override thiếu một key. `RemoteConfigModuleTests` bao phủ parser/fallback; test thêm validator của game.

Mỗi lần Prepare chọn nguồn mới sẽ xóa override của phiên trước. Shutdown xóa active và bundled snapshot. Fallback áp dụng theo key, còn field thiếu trong một block hợp lệ dùng default của model/consumer. Config currency được kiểm tra ID duy nhất, số nguyên không âm và shape của từng entry. [Tài liệu Remote Config đầy đủ](../remote-config-module.md)

## Currency

`CurrencyDatabase` định nghĩa số dư ban đầu, ID và icon. `CurrencyController` sở hữu số dư runtime và thông báo thay đổi. `CurrencyAmount` mang cặp currency/amount cho giá và reward. [Runtime Currency](../../Assets/NebulaSoft%20Core/Modules/Currency/Runtime)

1. Thêm entry trong `Currency Database.asset`. Với currency mới, đặt `id` ổn định như `Gem` và số dư ban đầu không âm. Entry legacy có `id` trống dùng enum và save key đã có.
2. Gắn database vào **Currencies** sau Save. Khi tạo shop hoặc HUD mới, lấy currency theo ID và bind `Currency.OnCurrencyChanged`; tháo listener khi đóng view.
3. Dùng `TrySpend` cho quyết định mua. Với overload string, ID chưa đăng ký, giá âm hoặc số dư thiếu trả false. `Add` từ chối giá trị âm và phép cộng overflow. Gắn phần thưởng sau khi game đã xác nhận kết quả.

```csharp
if (CurrencyController.TrySpend("Gem", 5, "extra_moves"))
{
    // Controller game cấp lượt đi theo luật của ván.
}

CurrencyController.Add("Gem", 10, "level_win");
```

Phần trình bày legacy còn dùng `CurrencyType`, gồm FloatingCloud và reward của Quest/Daily Reward. Với Gem/Energy, kiểm tra đường đọc icon, animation và reward picker trước khi dùng toàn bộ UI mẫu. Tránh đổi ID currency đã có dữ liệu hoặc chuyển entry legacy sang ID mới nếu chưa có mapping save. [Database mẫu](../../Assets/Project%20Files/Data/Currency%20Database.asset) · [FloatingCloud](../../Assets/NebulaSoft%20Core/Modules/UI/Runtime/FloatingCloud.cs)

**Kiểm tra.** Thêm currency thử qua asset, kiểm tra số dư sau reload, giá âm, thiếu tiền và overflow. Kiểm tra `CoreModuleUpgradeTests` cho batch currency.

## Reward

`RewardsSet` là asset chứa `[SerializeReference] List<Reward>`. `Reward.Validate` kiểm tra trước khi đổi state. `ApplyReward` thực thi một bước. `RewardView` và `IRewardPreview` hiển thị thưởng, `RewardsHolder` gắn reward set với UI. [Runtime Reward](../../Assets/NebulaSoft%20Core/Modules/Reward/Runtime) · [Prefab Reward](../../Assets/NebulaSoft%20Core/Modules/Reward/Prefabs)

1. Tạo **Data → Rewards → Rewards Set**, thêm reward phù hợp và gắn vào offer, claim view hoặc catalog IAP.
2. Dùng `CurrencyReward` cho batch currency. Nếu thêm loại mới, viết subclass `[Serializable]`, kiểm tra toàn bộ đầu vào trong `Validate`, hoàn thành một bước trong `ApplyReward` và khai báo view tương ứng nếu dùng reward picker.
3. Controller game xác nhận điều kiện claim rồi gọi grant. Chỉ mở preview hoặc thông báo sau khi bước cần lưu đã thành công. Với purchase, giao dịch đi qua `IAPManager.TryFulfillPurchase` để checkpoint và chống lặp.

```csharp
var reward = new CurrencyReward(new[]
{
    new CurrencyAmount("Coins", 50),
    new CurrencyAmount("Gem", 2)
}, "stage_reward");
reward.ApplyReward();
```

`RewardsSet.ApplyReward()` validate từng reward trước khi chạy các bước. Khi một bước runtime ném lỗi, các bước trước có thể đã áp dụng. Dùng cơ chế checkpoint hoặc thiết kế từng bước có thể retry an toàn nếu feature cần claim bền vững. `TimerRewardsHolder` hiện ghi cooldown trước khi apply, nên kiểm tra lỗi reward trước khi dùng cho phần thưởng quan trọng. [RewardsSet](../../Assets/NebulaSoft%20Core/Modules/Reward/Runtime/RewardsSet.cs) · [TimerRewardsHolder](../../Assets/NebulaSoft%20Core/Modules/Reward/Runtime/TimerRewardsHolder.cs)

**Kiểm tra.** Kiểm tra thiếu config, lỗi bước giữa, claim lặp và reload. Nếu reward thuộc IAP, test với cùng transaction ID.

## Skins

`AbstractSkinData` chứa ID, preview và trạng thái unlock. `GenericSkinDatabase<T>` quản lý một nhóm dữ liệu skin. `SkinsHandler` chứa các database mà `SkinController` quản lý. Game tự áp sprite, material hoặc prefab theo skin được chọn. [Runtime Skins](../../Assets/NebulaSoft%20Core/Modules/Skins/Runtime)

1. Tạo kiểu data của game kế thừa `AbstractSkinData` và database cụ thể kế thừa `GenericSkinDatabase<T>`. Ví dụ dưới đây đặt trong `BoardSkinDatabase.cs`. Board skin có thể thêm sprite hoặc material.
2. Tạo asset database, nhập ID duy nhất, preview và field art. Gắn asset vào `SkinsHandler` của `SkinController`, gọi `Init()` sau khi Save sẵn sàng.
3. Gọi `UnlockSkin(skinId, select: true)` khi cấp thưởng hoặc `SelectSkin(skinId)` cho skin đã mở khóa. Đọc `GetSelectedSkin<BoardSkinDatabase>()` trong presenter của game.

```csharp
using NebulaSoft;
using UnityEngine;

[System.Serializable]
public sealed class BoardSkinData : AbstractSkinData
{
    [SerializeField] private Sprite background;
    public Sprite Background => background;
}

[CreateAssetMenu(menuName = "Game/Skins/Board Database")]
public sealed class BoardSkinDatabase : GenericSkinDatabase<BoardSkinData> { }
```

Giữ ID skin và mapping nhóm database khi nâng cấp. Selected skin được ghi bằng string ID, còn hash cũ được dùng để migrate khi đọc dữ liệu cũ. Unlock và select đánh dấu dirty. [SkinController](../../Assets/NebulaSoft%20Core/Modules/Skins/Runtime/SkinController.cs)

**Kiểm tra.** Unlock và chọn skin khi save đang sạch, lưu rồi reload. Kiểm tra thêm save chọn skin bằng hash và khi reorder các skin trong một database.

# Công cụ và kiểu dùng chung

[Sổ tay module](README.md)

## Inspector

`Runtime` chứa attributes để khai báo cách hiển thị field. `Editor` chứa `CustomInspector`, drawer, theme và công cụ UniqueID. Hai phần có asmdef riêng. [Source Inspector](../../Assets/NebulaSoft%20Core/Modules/Inspector)

1. Gắn attribute như `Group`, `ReadOnly`, `ShowIf` hoặc `UniqueID` vào field serializable của model/config theo mục đích Inspector.
2. Với Inspector riêng, đặt file trong `Editor` và kế thừa `CustomInspector` nếu muốn dùng renderer của framework. Gọi `base.OnInspectorGUI()` trước các nút hành động tùy chỉnh nếu vẫn cần field mặc định.
3. Drawer mới sửa giá trị qua `SerializedProperty` để hỗ trợ undo và prefab override. Đặt validate runtime cần thiết trong service/module.

```csharp
[Group("Board")]
[SerializeField] private int moveLimit = 30;
```

**Kiểm tra.** Mở asset và prefab, thử undo/redo, override và reload. Với ID dùng cho save, kiểm tra hành vi khi duplicate asset rồi cấp ID mới cho bản sao. [CustomInspector](../../Assets/NebulaSoft%20Core/Modules/Inspector/Editor/CustomInspector.cs)

## CachedComponents

`ICachedComponent<T>` có `Cache(T)` và `Apply(T)`. Các class sẵn có lưu dữ liệu Transform, RectTransform và Collider. Drawer hỗ trợ lấy/apply cấu hình từ component được liên kết. [Source CachedComponents](../../Assets/NebulaSoft%20Core/Modules/CachedComponents)

1. Dùng `CachedTransform` hoặc kiểu tương ứng trên owner cần reset component. Cache từ instance nguồn ở thời điểm xác định.
2. Apply khi reset instance hoặc spawn lại. `CachedTransform` lưu world position/rotation và local scale, nên chọn lúc cache phù hợp với parent của object.
3. Khi cần loại component mới, tạo class serializable cài `ICachedComponent<T>`, chỉ lưu các field thật sự cần reset.

```csharp
var snapshot = new CachedTransform();
snapshot.Cache(sourceTransform);
snapshot.Apply(targetTransform);
```

**Kiểm tra.** Cache, đổi component, apply và kiểm tra đúng state. Thử với prefab pool có parent khác.

## Collections

`WeightedList<T>` chứa các `WeightedItem<T>` với weight. `PrefabsWeightedList` là phiên bản cho GameObject. `GetRandomItem` chọn theo tổng weight qua Unity Random. [Runtime Collections](../../Assets/NebulaSoft%20Core/Modules/Collections/Runtime)

1. Dùng list cho chọn prefab/effect hoặc biến thể theo trọng số. Kiểm tra item khác null và weight dương khi cấu hình.
2. Thêm item rồi gọi `GetRandomItem()`. List rỗng trả null kèm log. Nếu tổng weight bằng 0, implementation trả phần tử đầu tiên.
3. Nếu board cần tái hiện cùng seed theo ván, giữ RNG xác định ở gameplay và truyền kết quả lựa chọn sang view.

```csharp
var choices = new WeightedList<GameObject>();
choices.Add(new WeightedItem<GameObject>(3f, commonPrefab));
choices.Add(new WeightedItem<GameObject>(1f, rarePrefab));
var chosenPrefab = choices.GetRandomItem();
```

**Kiểm tra.** Kiểm tra một item, nhiều weight, list rỗng và cấu hình toàn weight 0.

## Types

`DuoFloat`, `DuoInt`, `DuoColor` và các kiểu Duo mô tả cặp giá trị. `ToggleType<T>` và các class cụ thể lưu cờ bật cùng một override. Drawer nằm trong `Types/Editor`. [Source Types](../../Assets/NebulaSoft%20Core/Modules/Types)

1. Dùng Duo cho cặp min/max hoặc hai giá trị liên quan trong config. Với range, validate min/max theo ý nghĩa của feature.
2. Dùng `IntToggle`, `FloatToggle` hoặc kiểu toggle phù hợp cho override tùy chọn. `Handle(fallback)` trả override nếu enabled, nếu không trả fallback.

```csharp
var pitchRange = new DuoFloat(0.9f, 1.1f);
float pitch = pitchRange.Random();

var moveLimitOverride = new IntToggle(true, 25);
int moves = moveLimitOverride.Handle(30);
```

**Kiểm tra.** Kiểm tra range đảo và cả hai trạng thái toggle. Khi đổi field đã serialize, đối chiếu dữ liệu cũ trong asset/save.

## Utilities

`Core.UE` có helper runtime như `SceneUtils`, `TimeUtils`, `CoreExtensions` và `Bezier`. `Core.UE.Editor` chứa `EditorUtils`, `EditorExtensions` và `TMPUtils`. [Runtime Utilities](../../Assets/NebulaSoft%20Core/Utilities/Runtime) · [Editor Utilities](../../Assets/NebulaSoft%20Core/Utilities/Editor)

1. Dùng helper hiện có ở phần phù hợp. Ví dụ controller kiểm tra `SceneUtils.DoesSceneExist(sceneName)` trước khi điều hướng.
2. Đặt code tìm asset, tạo inspector hay thao tác `AssetDatabase` trong Editor. Với module có asmdef, thêm reference vào assembly thực sự cần dùng.
3. Thêm helper vào core khi nhiều module dùng cùng contract. Giữ rule của board hoặc title trong lớp game.

**Kiểm tra.** Kiểm tra helper theo cả luồng gọi trong Editor và API runtime cần có trong player. Với helper đổi thời gian, test mốc 0, biên ngày và locale của title.

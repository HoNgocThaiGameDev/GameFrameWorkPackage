# Giao diện và feedback

[Sổ tay module](README.md)

## UI

`UIController` quản lý page theo type trong scene. `UIPage` sở hữu Canvas, GraphicRaycaster và các `IUIPageElement`. `CachedPages` cấu hình prefab tải khi cần. Popup triển khai `IPopupWindow`, thêm `IPausePopup` nếu cần pause gameplay theo setting của controller. [Runtime UI](../../Assets/NebulaSoft%20Core/Modules/UI/Runtime)

1. Gắn `UIController`, Canvas và CanvasScaler vào root UI. Đặt page làm con trực tiếp hoặc khai báo prefab trong CachedPages. Lưu scene/prefab để editor hook cập nhật các element đã đăng ký.
2. Scene controller gọi `uiController.Init()` rồi `uiController.InitPages()` trước khi hiển thị page đầu. Page đọc state trong `Init` hoặc khi mở, tùy vòng đời dữ liệu.
3. Trong `PlayShowAnimation`, báo `UIController.OnPageOpened(this)` khi page mở. Trong `PlayHideAnimation`, gọi `UIController.OnPageClosed(this)` khi animation đóng kết thúc. Controller tắt canvas, xử lý popup và chạy callback đóng theo page.
4. Từ game dùng `UIController.ShowPage<ShopPage>()` và `UIController.HidePage<ShopPage>(afterClosed)`. View cập nhật từ service event và tháo listener khi rời vòng đời sử dụng.

Tạo `ShopPage.cs` với chuyển trạng thái tức thời. Có thể thêm `IPausePopup` vào danh sách interface khi làm popup pause.

```csharp
using NebulaSoft;

public sealed class ShopPage : UIPage, IPopupWindow
{
    public bool IsOpened { get; private set; }

    public override void Init() { }

    public override void PlayShowAnimation()
    {
        IsOpened = true;
        UIController.OnPageOpened(this);
    }

    public override void PlayHideAnimation()
    {
        IsOpened = false;
        UIController.OnPageClosed(this);
    }
}
```

Khi thêm animation cho popup pause, dùng tween chạy theo thời gian unscaled để callback đóng chạy khi `timeScale = 0`. Dùng safe area và banner layout trên root thích hợp. HUD, shop và popup riêng của title đặt trong `Game/UI` hoặc `Game/Features`. [UIController](../../Assets/NebulaSoft%20Core/Modules/UI/Runtime/UIController.cs) · [Ví dụ UIComplete](../../Assets/Project%20Files/Game/UI/Scripts/UIComplete.cs)

**Kiểm tra.** Mở/đóng hai popup, đóng bằng callback lồng nhau, đổi scene khi popup đang đóng và kiểm tra pause trở về giá trị trước popup. Dùng `FrameworkSceneSmokeTests` để rà script thiếu và `CoreModuleUpgradeTests` cho callback đóng.

## Tween

`Tween` cập nhật các `TweensHolder` theo Update, FixedUpdate và LateUpdate. `TweenCase` giữ trạng thái một animation và callback. `Ease` cung cấp easing. `TweenInitModule` ở `Integration/Tween` tạo component trên Initializer. [Runtime Tween](../../Assets/NebulaSoft%20Core/Modules/Tween/Runtime) · [TweenInitModule](../../Assets/NebulaSoft%20Core/Integration/Tween/Runtime/TweenInitModule.cs)

1. Đặt Tween trước module dùng animation hoặc `Tween.InvokeCoroutine`, như Lives.
2. Gọi API tween khi owner đã sẵn sàng. Giữ `TweenCase` ở component sở hữu để hủy animation cũ khi rebind hoặc đóng page.
3. Dùng `OnComplete` để tiếp tục flow sau animation. Dùng `KillActive()` khi bỏ animation lúc unload. Chọn unscaled cho UI cần chạy khi game pause.

```csharp
fadeTween.KillActive();
fadeTween = Tween.DoFloat(0f, 1f, 0.25f,
    value => canvasGroup.alpha = value, unscaledTime: true);
```

`fadeTween` là field `TweenCase` và `canvasGroup` là CanvasGroup của view. Hủy tween khi view bị destroy hoặc không còn sở hữu target.

**Kiểm tra.** Dùng `TweenLogicTests` cho complete/kill và callback. Thử rebind hai lần và pause giữa animation.

## Pool

`Pool` giữ danh sách instance của prefab. `PoolManager` đăng ký pool theo tên đầy đủ và cấp container mặc định nếu không có container riêng. Runtime và editor tooling có asmdef riêng. [Pool](../../Assets/NebulaSoft%20Core/Modules/Pool/Runtime/Pool.cs) · [PoolManager](../../Assets/NebulaSoft%20Core/Modules/Pool/Runtime/PoolManager.cs)

1. Tạo một pool cho mỗi loại prefab cần tái sử dụng, bằng field serializable hoặc `new Pool(prefab, "game.tile")`. Đặt tên duy nhất, prewarm bằng `CreatePoolObjects(count)` nếu cần.
2. `GetPooledObject()` trả object đã active. Kiểm tra null nếu pool giới hạn dung lượng. Reset state của instance như transform, data, tween và listener theo contract spawn của game.
3. Đặt instance inactive khi trả về. Khi owner kết thúc, dùng `PoolManager.DestroyPool(pool)` nếu pool thuộc owner đó. Chọn container scene cho vòng đời theo scene hoặc container mặc định khi cần tái sử dụng qua scene.

```csharp
var tile = tilePool.GetPooledObject();
if (tile != null)
    tile.transform.SetPositionAndRotation(worldPosition, Quaternion.identity);

// Tại điểm despawn
if (tile != null)
    tile.SetActive(false);
```

Vì `OnEnable` có thể chạy ngay lúc lấy object, đưa logic cần dữ liệu spawn mới vào bước setup rõ ràng sau khi lấy object.

**Kiểm tra.** Spawn tới giới hạn, trả lại rồi lấy tiếp. Destroy instance từ ngoài hoặc unload scene chứa container và xác nhận slot hỏng được dọn. [CoreModuleUpgradeTests](../../Assets/Tests/Editor/CoreModuleUpgradeTests.cs)

## Audio

`AudioInitModule` gắn `AudioClips`, cấu hình số source và số voice tối đa. `AudioController` quản lý pool SFX và mức volume lưu trong Save. `MusicSource` dùng cho nhạc scene hoặc nguồn nhạc global. [Runtime Audio](../../Assets/NebulaSoft%20Core/Modules/Audio/Runtime)

1. Đặt Audio sau Save, gắn `Audio Clips.asset` và chọn pool/voice cap theo thiết bị mục tiêu.
2. Gọi `PlaySound` với clip đã gắn trong Inspector. `minDelay` giới hạn tần suất phát theo clip trong pool của controller. Dùng overload có vị trí cho SFX 3D.
3. Bind slider Settings vào `AudioController.SetVolume` với `NebulaSoft.AudioType.Sound` hoặc `NebulaSoft.AudioType.Music`. Dùng `GetVolume` để render giá trị lúc mở.
4. Cấu hình `MusicSource` cho nhạc từng scene, gọi `Activate()` theo flow scene. Nguồn global của Initializer được init/activate sau các InitModule.

```csharp
AudioController.PlaySound(clip, volumePercentage: 0.8f, minDelay: 0.08f);
AudioController.SetVolume(NebulaSoft.AudioType.Sound, 0.5f);
```

**Kiểm tra.** Phát nhiều effect cùng clip, xác nhận throttle và cap. Đổi volume khi clip đang phát và reload save. Kiểm tra listener sau đổi camera/scene.

## Haptic

`HapticInitModule` khởi tạo `Haptic` sau Save. Runtime chọn wrapper nền tảng và dùng `HapticData` hoặc `HapticPattern` để mô tả feedback. [Runtime Haptic](../../Assets/NebulaSoft%20Core/Modules/Haptic/Runtime)

1. Thêm **Haptic** vào registry và kiểm tra wrapper/define cho target.
2. Gọi feedback tại điểm controller đã xác nhận thao tác, ví dụ kết quả ghép hoặc kết thúc level. Chọn priority theo ý nghĩa thay vì rung ở mọi frame.
3. Dùng `Haptic.IsActive` cho thiết lập bật/tắt trong game. Nếu thêm pattern, đặt ID duy nhất và đăng ký bằng `Haptic.RegisterPattern` sau init.

```csharp
#if MODULE_HAPTIC
Haptic.Play(Haptic.HAPTIC_MEDIUM, HapticPriority.Outcome);
#endif
```

**Kiểm tra.** Thử bật/tắt, nhiều feedback liên tiếp và resume trên thiết bị. Scene ví dụ cho thấy cách gọi API. [Haptic example](../../Assets/NebulaSoft%20Core/Examples/Example%2003%20-%20Haptic)

## FloatingText

`FloatingTextController` đăng ký các `FloatingTextCase` theo tên. Mỗi case gắn prefab có `FloatingTextBaseBehavior` và pool riêng. Game quyết định text, vị trí và thời điểm phát. [Runtime FloatingText](../../Assets/NebulaSoft%20Core/Modules/FloatingText/Runtime)

1. Gắn controller vào scene. Thêm case `score` và kéo prefab có behavior phù hợp.
2. Gọi `floatingTextController.Init()` từ scene controller trước lần spawn đầu.
3. Gọi `FloatingTextController.SpawnFloatingText("score", "+10", worldPosition)` khi gameplay cộng điểm. Dùng tên case và tên pool prefab duy nhất.
4. Nếu tạo behavior mới, xử lý animation và reset state khi object được tái sử dụng. `FloatingTextController.Unload()` đưa object đang dùng về pool khi reset presentation của scene.

**Kiểm tra.** Spawn nhiều lần ở các vị trí khác nhau và qua hai lượt chơi. Kiểm tra text cũ, tween còn chạy và trường hợp case/prefab thiếu.

## Particles

`ParticlesController` quản lý danh mục `Particle` và các effect instance dùng pool. `ParticleCase` cho phép gán vị trí, target và theo dõi việc effect dừng. `ParticleBehaviour` là điểm mở rộng cho logic trình bày khi effect activate/deactivate. [Runtime Particles](../../Assets/NebulaSoft%20Core/Modules/Particles/Runtime)

1. Gắn `ParticlesController` vào scene, thêm các particle prefab và tên như `board_clear`, rồi gọi `Init()` từ controller.
2. Gọi effect từ event gameplay sau khi rule đã xử lý. Có thể đăng ký effect bổ sung bằng `RegisterParticle(string, GameObject)` khi controller đã sẵn sàng.
3. Dùng `ParticleCase.SetPosition` cho điểm phát hoặc `SetTarget` khi effect đi theo object. Giữ phần grant/score ngoài callback animation.

```csharp
var effect = ParticlesController.PlayParticle("board_clear");
effect?.SetPosition(worldPosition);
```

**Kiểm tra.** Phát lặp, kiểm tra effect tự dừng và quay lại pool. Thử unload scene giữa effect và effect có delay hoặc follow target.

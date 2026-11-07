# Feature tùy chọn và lớp game

[Sổ tay module](README.md)

Lives, Quest và Daily Reward giữ logic dùng chung trong `NebulaSoft Core/Features`. Database của title nằm ở `Project Files/Data`. Game tự nối presenter và sự kiện gameplay trong `Game/Features`. Để tắt feature, bỏ module khỏi registry và cập nhật các view hoặc adapter vẫn gọi feature đó.

## Lives

`LivesSystemInitModule` nhận `LivesData`, cần Save, Currency và Tween trước nó. `LivesSystem` quản lý số life, lock của ván, refill, infinite mode và thời điểm hồi. `LivesSave` lưu state, `LivesStatus` cung cấp snapshot cho UI, `Clock` có thể thay khi test. [Runtime Lives](../../Assets/NebulaSoft%20Core/Features/Lives/Runtime) · [Lives Data](../../Assets/Project%20Files/Data/Lives%20Data.asset)

1. Tạo hoặc gắn `LivesData` vào **Lives System**. Đặt số life tối đa, thời gian hồi và prefab preview. Nếu dùng remote config, đối chiếu key `lives`.
2. Controller kiểm tra đủ life trước khi vào ván. Chỉ gọi `LockLife()` sau khi bắt đầu ván thật. Gọi `UnlockLife(decrease: true)` khi kết thúc theo rule trừ life, hoặc `false` nếu được giữ life. Xử lý revive trước khi quyết định trừ.
3. HUD đọc `LivesSystem.Status` và subscribe `StatusChanged`. Shop kiểm tra giá rồi cấp refill/infinite mode bằng API của service. Controller phải tách bước mua khỏi animation preview.

```csharp
// Sau khi đã kiểm tra quyền bắt đầu ván
LivesSystem.LockLife();

// Khi ván đã kết thúc và rule yêu cầu trừ một life
LivesSystem.UnlockLife(decrease: true);
```

**Kiểm tra.** Thử hết life, thắng/thua, thoát khi đang lock, vào lại sau thời gian hồi và infinite mode hết hạn. Dùng clock cố định để kiểm tra biên thời gian. [View Lives của game](../../Assets/Project%20Files/Game/Features/Lives)

## Quest

`QuestDatabase` giữ definition và milestone. `QuestService` quyết định kỳ UTC, rotation, progress và claim. `QuestProgressSave` lưu theo quest ID và kỳ. `QuestGameProgressAdapter` của game mẫu nối sự kiện tiêu Coins và xem rewarded ad vào goal. [Runtime Quest](../../Assets/NebulaSoft%20Core/Features/Quest/Runtime) · [Game adapter](../../Assets/Project%20Files/Game/Features/Quest/Scripts/QuestGameProgressAdapter.cs)

1. Gắn `Quest Database.asset` vào module **Quest** sau Save và Currency. Thêm definition với ID ổn định, category, goal, target, reward, rotation slot và weight. Event quest đổi period key khi bắt đầu đợt mới.
2. Gửi tiến độ từ điểm gameplay đã hoàn tất, ví dụ level win hoặc purchase thành công. Với goal riêng, game có thể báo qua quest ID hoặc thêm mapping trong adapter.
3. View dùng `GetDefinitions`, `GetProgress` và `GetMilestoneState` để bind. Nút claim gọi `TryClaim` hoặc `TryClaimMilestone`, cập nhật trạng thái khi trả true.

```csharp
// Gọi một lần sau khi đã chốt thắng ván.
QuestService.ReportProgress(QuestGoalType.CompleteLevels);

// Trong callback của nút Claim, questId lấy từ item được bind.
bool claimed = QuestService.TryClaim(questId);
```

**Giới hạn hiện tại.** PowerUp và RandomPowerUp chưa có grant adapter. Luồng claim currency cấp thưởng trước khi đánh dấu claimed. Một callback currency gọi lại claim cùng quest có thể cộng thưởng hai lần. Khi triển khai title, giữ claim ở một điểm vào, tránh gọi claim từ currency observer và xử lý khóa theo quest/kỳ trước khi mở rộng luồng claim. [QuestReward](../../Assets/NebulaSoft%20Core/Features/Quest/Runtime/QuestReward.cs) · [QuestService](../../Assets/NebulaSoft%20Core/Features/Quest/Runtime/QuestService.cs)

**Kiểm tra.** Dùng `QuestRotationTests` cho rotation/prefab. Thử đổi ngày/tuần UTC, event chạy lại, reload khi đã claim, milestone và callback claim lồng nhau. [Database mẫu](../../Assets/Project%20Files/Data/Quest/Quest%20Database.asset)

## DailyReward

`DailyRewardDatabase` chứa lịch 7 ngày. `DailyRewardService` quyết định ngày UTC, điều kiện claim và bước grant. `DailyRewardSave` lưu ngày claim, index lịch và checkpoint đang cấp. Service có overload nhận `IDailyRewardScheduleProvider`, `IDailyRewardClock` và `IDailyRewardStateStore`. [Runtime DailyReward](../../Assets/NebulaSoft%20Core/Features/DailyReward/Runtime)

1. Gắn database vào **Daily Reward** sau Save và Currency. Cấu hình đủ bảy ngày và reward hợp lệ cho từng ngày. Dùng schedule version để xác định lịch khi nâng cấp.
2. UI đọc `GetState()` để biết ngày hiện tại và `CanClaimToday`. Đọc `GetDay(index)` để bind reward preview.
3. Nút claim gọi `TryClaimToday()`. Hiện popup nhận thưởng sau khi API trả true. Với test lịch, inject clock và store tạm để chủ động tiến ngày và mô phỏng lỗi ghi.

```csharp
var state = DailyRewardService.GetState();
if (state.CanClaimToday && DailyRewardService.TryClaimToday())
{
    // View tải lại trạng thái lịch và hiện kết quả.
}
```

Database mẫu có PowerUp ở ngày 2, 4, 6 và 7. `DailyRewardGrant.IsAvailable()` hiện trả false cho PowerUp, nên bốn ngày này bị chặn. Với title dùng PowerUp, cần triển khai điểm cấp thưởng của game trước khi dùng lịch đó. Với lịch thử nghiệm chỉ có currency, dùng database riêng để kiểm tra đủ chu kỳ. [Lịch mẫu](../../Assets/Project%20Files/Data/Daily%20Reward/Daily%20Reward%20Database.asset) · [DailyRewardModels](../../Assets/NebulaSoft%20Core/Features/DailyReward/Runtime/DailyRewardModels.cs)

**Kiểm tra.** Claim hai lần trong ngày, đổi ngày UTC, lùi clock, bỏ một ngày và lỗi giữa nhiều reward. Kiểm tra cả bảy ngày của asset title. Harness ModuleAudit có ca claim trùng và reentry.

## LegacyPowerUps

`PUType` giữ các giá trị enum đã serialize của template. Nó được tham chiếu bởi data Quest/Daily Reward và lớp PowerUps của game. [PUType](../../Assets/NebulaSoft%20Core/Features/LegacyPowerUps/Runtime/PUType.cs)

1. Giữ số enum cũ khi đọc asset/save đã phát hành.
2. Tạo inventory, rule sử dụng và grant adapter trong `Game/Features/PowerUps` của title. Game mẫu có `PUSave` và UI shell để làm điểm tích hợp.
3. Khi chuyển sang ID string cho booster, định nghĩa mapping từ enum cũ sang ID mới và migration cho dữ liệu đã lưu. Giữ cách hiển thị icon và tên trong asset của game.

**Kiểm tra.** Mở asset reward cũ, đối chiếu đúng booster và số lượng. Thử grant, consume, lưu và reload trên bản sao save cũ. [PowerUps của game](../../Assets/Project%20Files/Game/Features/PowerUps)

## Lớp game mẫu

`GameInitModule` khởi tạo `GameData` và gắn adapter Quest. `MenuController` quản lý sảnh. `GameController` quản lý vòng đời ván, session và popup kết quả. Khi thêm board, để rule và input trong gameplay mới, rồi nối các điểm begin/win/fail/revive vào controller. Mẫu hiện cung cấp shell để gắn board và dữ liệu level. [GameInitModule](../../Assets/Project%20Files/Game/Scripts/GameInitModule.cs) · [GameController](../../Assets/Project%20Files/Game/Scripts/Controllers/GameController.cs) · [Ví dụ gắn board](../framework-developer-guide.md#27-ví-dụ-đầy-đủ-board-nối-gamecontroller)

| Feature của title | Cách nối khi làm game mới |
| --- | --- |
| [CoinSafe](../../Assets/Project%20Files/Game/Features/CoinSafe) | Dùng `CoinSafeProgress` cho state của feature, bind popup và text. Nối checkpoint cloud qua `CoinSafeCloudSync` nếu có tài khoản. |
| [DailyReward](../../Assets/Project%20Files/Game/Features/DailyReward) | Bind calendar và claim popup vào `DailyRewardService`. Dùng database của title. |
| [DevPanel](../../Assets/Project%20Files/Game/Features/DevPanel) | Gắn settings và command QA của game qua `DevPanelInitModule`. Đặt command reset/skip trong flow phát triển. |
| [FeatureAnnouncement](../../Assets/Project%20Files/Game/Features/FeatureAnnouncement) | Cấu hình nội dung bằng `FeatureAnnouncementPopupData`, mở popup ở mốc mà game quyết định. |
| [Leaderboard](../../Assets/Project%20Files/Game/Features/Leaderboard) | Bind panel vào `LocalLeaderboardService` để thử UI offline. Nếu có backend, adapter của title cung cấp dữ liệu và refresh cho view. |
| [Lives](../../Assets/Project%20Files/Game/Features/Lives) | Bind indicator, nút mua/refill và popup quit theo `LivesSystem`. Controller sở hữu việc trừ life. |
| [NoAdsOffer](../../Assets/Project%20Files/Game/Features/NoAdsOffer) | Gắn offer vào product catalog và cập nhật nút từ entitlement. Grant đi qua IAP. |
| [PowerUps](../../Assets/Project%20Files/Game/Features/PowerUps) | Gắn icon/nút vào inventory và rule booster của game. Lưu lượng còn lại qua model của title. |
| [Profile](../../Assets/Project%20Files/Game/Features/Profile) | Bind avatar, frame và dialog vào profile, dùng Firebase adapter khi bật đăng nhập/sync. |
| [Quest](../../Assets/Project%20Files/Game/Features/Quest) | Bind panel/element vào `QuestService`, báo tiến độ qua adapter hoặc controller gameplay. |
| [Settings](../../Assets/Project%20Files/Game/Features/Settings) | Gắn các button/slider vào Audio, Haptic và action thoát của controller. |
| [Store](../../Assets/Project%20Files/Game/Features/Store) | Đặt offer prefab theo tab/nhóm UI, gắn product key, reward và price label từ catalog. |
| [Tutorial](../../Assets/Project%20Files/Game/Features/Tutorial) | Viết flow tutorial của board qua `ITutorial`/`BaseTutorial`, lưu progress qua model tutorial và báo event sau thao tác hoàn tất. |

Adapter Ads và Firebase nằm ở `Game/Integration`. Mỗi title sở hữu các quyết định như hiện offer ở đâu, grant quyền tiếp tục thế nào, và xử lý kết quả đăng nhập ra sao. [Game integration](../../Assets/Project%20Files/Game/Integration)

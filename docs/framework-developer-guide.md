# GameFrameWorkPackage

## Hướng dẫn dùng framework, scene và module

Tài liệu cho developer và technical designer. Bản framework gốc dùng InitModule, không dùng DI container. Đối chiếu README, tài liệu Ads/IAP/Save và source trong dự án ngày 11/10/2026.

Unity 6000.6.8f1 | Framework 1.4.0 | Init > Menu > Game

Ads Kit 2.3.0 | Google Mobile Ads 11.0.0 | EDM4U 1.2.187

Đọc chương 1-4 trước khi tích hợp. Mỗi chương module trình bày trách nhiệm, cấu hình, điểm khởi tạo, cách nối controller, ví dụ và giới hạn. Code trong tài liệu là mẫu tích hợp, chưa được cài vào Assets. Các kiểu có tiền tố Guide là kiểu mẫu để phân biệt với API có thật.

[Sổ tay triển khai module](modules/README.md) cung cấp mục tra cứu theo từng module, kèm đường dẫn source, cách cấu hình và các ca cần kiểm tra.

## 1. Cách dùng framework từ đầu

Framework được sử dụng theo ba scene **Init -> Menu -> Game**. Init chuẩn bị dịch vụ chung; Menu hiển thị sảnh và nhận lựa chọn của người chơi; Game chứa HUD, popup kết quả và gameplay của game mới. Bắt đầu bằng cách chạy được luồng này, sau đó mới nối từng module vào controller.

```uml
scene_lifecycle
```

### Cách đọc UML trong tài liệu

Các sơ đồ theo ký pháp UML, trình bày kiểu Astah. Nguồn chỉnh sửa `.puml` và bản vector `.svg` ở `docs/diagrams/uml/`; không phải file `.asta` native. Mỗi caption nêu rõ sơ đồ theo source hay thiết kế đề xuất.

| Ký pháp | Ý nghĩa |
| --- | --- |
| Class box, field / method compartments | Type và member; không phải GameObject hierarchy |
| Nét liền có mũi tên | Association có reference; hướng tới type được tham chiếu |
| Nét đứt có mũi tên thường | Dependency: gọi API / sử dụng type, không khẳng định ownership |
| Nét liền + tam giác rỗng | Generalization; tam giác hướng về base class |
| Nét đứt + tam giác rỗng | Realization; concrete class implements interface |
| Hình thoi đen ở đầu owner | Composition / ownership lifetime đã được xác nhận |
| Lifeline, activation, return nét đứt | Sequence gọi qua thời gian; ô alt/opt/loop/ref biểu diễn fragment |
| Diamond trong activity | Quyết định theo guard, có nhánh đi riêng |

### Ba scene có trách nhiệm gì?

| Scene / build index | Thành phần chính | Kết quả cần đạt |
| --- | --- | --- |
| Init / 0 | Game Loading, Loading Graphics, Initializer, Project Init Settings, SDKs | Save và service đã sẵn sàng; tải Menu; đóng loading |
| Menu / 1 | Main Camera, UI Main Canvas, Scripts Holder, Bottom Navigation Canvas | Hiện UIMainMenu; mở popup; Play chuyển sang Game |
| Game / 2 | Main Camera, UI Main Canvas với GameController, UIGame và popup | Hiện HUD; game mới tạo board, chạy rule, báo win/fail |

Scene nằm trong `Assets/Project Files/Game/Scenes/`. Giữ tên Init, Menu, Game khớp với `GameConsts`; nếu đổi tên phải đổi nơi gọi và danh sách scene build. Hierarchy trong bảng được lấy từ scene đã lưu, kiểm tra qua Unity MCP ngày 11/10/2026. UML class/sequence đối chiếu source; các sơ đồ đề xuất đều được đánh dấu riêng.

### Bước 1: mở và chạy template

1. Mở thư mục GameFrameWorkPackage bằng Unity 6000.6.8f1. Chờ import và compile xong; giải quyết lỗi Console trước khi Play.
2. Trong Build Profiles, kiểm tra Scene List bật Init, Menu, Game theo index 0, 1, 2. GameLoading mặc định lấy scene kế tiếp Init theo build index.
3. Mở Init.unity rồi Play. Kỳ vọng loading xuất hiện, sau đó UIMainMenu. Không chạy riêng Menu/Game để kiểm tra toàn bộ framework: service của Init chưa được tạo nếu không có luồng khởi động hỗ trợ.
4. Trong Menu, thử Settings, Store, Daily Reward, Profile, các tab sảnh; nhấn PLAY để vào Game. Scene Game hiện là shell UI để gắn gameplay, chưa có board puzzle hoàn chỉnh.
5. Stop rồi Play lại từ Init. Kiểm tra không xuất hiện bản Initializer hoặc EventSystem thứ hai, save đọc lại được và popup không giữ trạng thái pause từ phiên trước.

Chỉ bước khởi chạy nằm ở Init. Khi retry/next hoặc quay về sảnh, chuyển Menu/Game trực tiếp; không quay lại Init để khởi tạo toàn bộ SDK và save một lần nữa.

<!-- pagebreak -->

### Bước 2: cấu hình dữ liệu và service cho game mới

1. Mở `Assets/Project Files/Data/Project Init Settings.asset`. Đây là danh sách module thực sự được Initializer gọi. Kiểm tra asset cấu hình của từng module; giữ Save trước các module đọc player state.
2. Chuẩn bị GameData, currency database, audio settings/library, lives, quest và reward catalog theo game. Dùng asset config cho cân bằng; dùng SaveController cho dữ liệu người chơi.
3. Xác định feature nào game cần. Nếu bỏ feature, xử lý cả module, button/popup và listener phụ thuộc; không chỉ xóa file settings đang được scene tham chiếu.
4. Với Ads hiện tại, chọn Android target và test IDs. Ads Kit đã cài; Test Mode bật, chỉ placement banner bật. Fullscreen/rewarded cần được cấu hình và bật trước khi tích hợp flow thật.
5. Kiểm tra Player Settings: identifier của game mới, orientation, build target và scripting defines phù hợp. Đây là bước chuẩn bị sản phẩm, không cần viết lại bootstrap.

### Bước 3: đổi giao diện, rồi gắn gameplay

1. Giữ hierarchy và component binding của Init; thay background/logo/loading art qua LoadingGraphics và các Image liên quan.
2. Ở Menu, thay art của UIMainMenu, nội dung sảnh và các popup. Giữ tham chiếu MenuController -> UIController; giữ binding StaticMapPanel -> Play Button.
3. Ở Game, giữ GameController và UIController cùng trên UI Main Canvas. Thêm controller gameplay riêng và Board Root; hai thành phần này là phần game mới cần tạo, không có sẵn trong scene template.
4. Controller gameplay đọc level database của game, gọi ActiveSession khi bắt đầu lượt, tạo board rồi mới ActivateGame. Input chỉ được xử lý khi lượt chơi đang hoạt động và không bị khóa bởi popup/animation.
5. Nối thắng, thua, retry, next, revive vào game flow. Next phải chọn và lưu level kế tiếp trước khi tải lại Game; revive phải phục hồi board/timer, ngoài việc đóng popup.
6. Nối feedback bằng AudioController, Haptic, Tween/Pool; nối economy/save/ads qua API framework. Các chương module phía sau có ví dụ C# và contract cụ thể.

```uml
mvc_components
```

Template cung cấp loading, service, save, UI và economy/monetization. Game mới tự thêm board, input, rule, level database và policy next/retry/revive; tutorial và booster cần nối mechanic của game.

## 2. Scene Init: loading và khởi tạo ứng dụng

Mở `Assets/Project Files/Game/Scenes/Init.unity`. Scene có ba GameObject root: **Initializer**, **Loading Graphics**, **Game Loading**. Chúng có vòng đời khác nhau: Initializer được giữ suốt ứng dụng; hai root loading chỉ sống qua giai đoạn tải đầu tiên rồi bị hủy.

### Quan hệ class và cấu hình Init

```uml
init_structure
```

Class diagram trên vẽ reference giữa các component và asset; bảng dưới mô tả hierarchy GameObject riêng. ATT/UMP/Monetization/AdsGameBridge là component trên SDKs, không phải GameObject con.

<!-- pagebreak -->

### Hierarchy GameObject

| GameObject / quan hệ Transform | Component hoặc nội dung |
| --- | --- |
| Initializer - root | Initializer; DontDestroyOnLoad |
| Initializer/SDKs - child | SDKInitializer, ATTTaskBehavior, UMPTaskBehavior, MonetizationSDKBehavior, AdsGameBridge |
| Initializer/Music - child | AudioSource, MusicSource |
| Initializer/Event System - child | EventSystem, InputSystemUIInputModule |
| Initializer/Core System Messages Canvas - child | Canvas, CanvasScaler, SystemMessage |
| Loading Graphics - root | LoadingGraphics; child Loading Camera và Canvas |
| Loading Graphics/Canvas - child | Background, Logo, Top/Bottom Image, Loading Text, Fillbar, Retry Button |
| Game Loading - root | GameLoading; hai serialized reference tới Initializer / LoadingGraphics |

Camera loading và Canvas thuộc Loading Graphics nên được giữ tạm khi Menu đã tải, đủ thời gian fade màn loading. EventSystem ở Initializer phục vụ cả Menu và Game.

**Không đặt board gameplay trong Init.** Scene này chỉ compose config/service/SDK và loading. Một board tạo lại theo mỗi lượt nên thuộc Game; art sảnh và navigation nên thuộc Menu.

<!-- pagebreak -->

### Inspector: kéo tham chiếu nào vào đâu?

| Component / field | Giá trị trong scene hiện tại | Mục đích |
| --- | --- | --- |
| Initializer.initSettings | Project Files/Data/Project Init Settings.asset | Danh sách và config module |
| Initializer.sdkInitializer | Initializer/SDKs | Chạy SDK behaviors và loading tasks |
| Initializer.systemMessage | Core System Messages Canvas | Thông báo, loading panel dùng chung |
| Initializer.globalMusicSource | Initializer/Music | Nguồn nhạc toàn ứng dụng |
| Initializer.eventSystem | Initializer/Event System | Nhận input UI xuyên scene |
| GameLoading.initializer | Initializer | Gọi Init, InitModules, InitSDKs |
| GameLoading.loadingGraphics | Loading Graphics | Cập nhật progress, lỗi và fade |
| GameLoading.useManualControl | false | Hiện tự kết thúc loading |
| GameLoading.checkNetworkConnection | false | Scene hiện không chặn boot bằng network probe |

Trong LoadingGraphics, nối Loading Camera vào `loadingCamera`; Canvas vào `canvasScaler` và `canvasGroup`; Background vào `backgroundImage`; Fillbar/Fillbar Mask vào `loadingImage`; Loading Text và Percentage Text vào hai field TMP; Fillbar vào `loadingbarObject`; Retry Button vào `retryButton`. Các tham chiếu này đang được nối trong scene.

### Cấu hình Project Init Settings

Registry mẫu chạy theo thứ tự **Remote Config → Save → Analytics → Tween → Audio → Currencies → Haptic → Dev Panel → Game Settings → Lives → Screen Settings → Quest → Daily Reward → IAP → Ads**. Thứ tự thực tế do asset quyết định.

Save chuẩn bị dữ liệu trước Currency/Lives/Quest/Daily Reward. Tween cần sẵn sàng trước animation loading/UI. Audio settings được chuẩn bị trước khi global MusicSource khởi tạo. IAP/Ads module chuẩn bị service/config; SDK monetization nối tiếp sau bước consent.

### Đổi màn loading cho game mới

1. Giữ ba root và các component điều phối. Thay sprite Background, Logo, Top Image, Bottom Image theo art mới; kiểm tra cả màn cao và tablet.
2. Background phủ toàn Canvas; chỉ nội dung cần tránh notch mới nằm trong safe content. Loading hiện không có Safe Area root riêng; nếu bổ sung, không gom Background vào đó.
3. Với progress, giữ `loadingImage` là Image có kiểu fill phù hợp và giữ Mask đúng như art. LoadingGraphics cập nhật `fillAmount`, phần trăm và thông điệp.
4. Giữ Retry Button ẩn lúc bình thường. Nếu bật network check, mất mạng sẽ ẩn fillbar, hiện Retry; button đã gọi GameLoading.RetryConnection.
5. Bật manual control chỉ khi có công việc chuẩn bị thêm. Owner công việc đó phải gọi `GameLoading.MarkAsReadyToHide()` khi hoàn tất, nếu không loading sẽ chờ tiếp.

<!-- pagebreak -->

### Luồng khởi tạo từ Play đến Menu

```uml
init_sequence
```

Hai giây được tính ở giai đoạn bắt đầu tải scene tiếp theo, sau module/task; không phải tổng thời gian bootstrap cố định. Sau fade, LoadingGraphics gọi EnableBanner. MenuController cũng yêu cầu banner; service Ads quản lý trạng thái thực tế.

<!-- pagebreak -->

### Sequence chi tiết: SDK behavior, task và consent

```uml
sdk_sequence
```

SDKBehavior / ISDKTaskBehavior là các component trên SDKs. MonetizationSDKBehavior override OnUserConsentReceived để khởi tạo Monetization, Ads và IAP. Editor chạy callback ngay; device chờ các task hoàn tất.

<!-- pagebreak -->

### Vòng đời và dấu hiệu kiểm tra

| Sau khi Menu xuất hiện | Kỳ vọng |
| --- | --- |
| Initializer / Music / SDKs / Event System | Còn trong DontDestroyOnLoad |
| Game Loading | Bị hủy sau khi báo kết thúc loading |
| Loading Graphics | Bị hủy sau fade; camera loading không còn |
| UI của Menu | Do MenuController khởi tạo; không phải UI loading |

Nếu boot dừng trước Menu: kiểm tra null reference, task chưa IsFinished, Scene List và manual control. Nếu không có nhạc: kiểm tra Audio module, Audio settings và MusicSource. Nếu không click được UI: kiểm tra EventSystem/input module và lớp loading còn chặn raycast. Không thêm Initializer thứ hai vào Menu để chữa lỗi chưa boot.

**Nguồn:** Initializer.cs, GameLoading.cs, LoadingGraphics.cs, SDKInitializer.cs trong `NebulaSoft Core/Modules/Initializer/Runtime/`; Init.unity và Project Init Settings.asset.

## 3. Scene Menu: sảnh, navigation và vào game

Mở `Assets/Project Files/Game/Scenes/Menu.unity`. Scene này dùng các service đã tạo ở Init và dựng giao diện sảnh. Nó chứa UIController riêng của Menu; khi tải Game bằng single scene, các GameObject Menu bị hủy, còn Initializer vẫn ở lại.

### Quan hệ controller và UIPage của Menu

```uml
menu_structure
```

<!-- pagebreak -->

### Hierarchy GameObject

| GameObject trong Menu | Component / nội dung chính |
| --- | --- |
| Main Camera; Light/Directional Light | Camera, URP camera data; ánh sáng scene |
| UI Main Canvas | Canvas, CanvasScaler, GraphicRaycaster, UIController, ProgressPopupToggle |
| UI Main Canvas/UI Main Menu | UIMainMenu; Background, Levels Map/Play Button, Safe Zone |
| Các UIPage con trực tiếp | No Ads, Menu Settings, Add Lives, Store, Rewards Confirmation, Profile |
| Scripts Holder | MenuController, ParticlesController, FloatingTextController, SkinController, PoolSceneHolder, TutorialController |
| Bottom Navigation Canvas/LeaderboardBottomNavigation | Canvas riêng; LeaderboardBottomNavigationController, BottomNavigationBannerSpacer |
| IngameDebugConsole | Root hiện inactive; debug UI |


UI Main Canvas còn chứa Tutorial Overlay, UI leaderboard, NoConnectionPopup, ProgressPopUp, Panel_quest và DailyRewardPopup. Chúng được giải thích ở trang kế tiếp vì không dùng chung một cách đăng ký UIPage.

Scripts Holder còn có ParticlesController, FloatingTextController, SkinController, PoolSceneHolder và TutorialController. Chúng phục vụ presentation/tài nguyên theo scene; MenuController giữ tham chiếu đến UI Main Canvas. Không có Initializer thứ hai trong Menu.

<!-- pagebreak -->

### Thành phần và binding cần giữ khi sửa sảnh

| Thành phần / field | Tham chiếu thực tế | Cách dùng |
| --- | --- | --- |
| MenuController.uiController | UI Main Canvas / UIController | Awake gọi Init rồi InitPages |
| UIMainMenu.safeAreaRectTransform | Safe Zone | Đăng ký vùng nội dung tránh notch |
| UIMainMenu.coinsPanel | Currency Panel Simple | Init và mở Store khi nhấn dấu cộng |
| UIMainMenu.staticMapPanel | Levels Map | Nối button PLAY với MenuController |
| StaticMapPanel.playButton | Levels Map/Play Button | Init đăng ký OnPlayButtonClicked |
| UIMainMenu.noAdsButton | No Ads Button: rect + button | Mở UINoAdsOffer; trạng thái theo No Ads entitlement |
| UIMainMenu.dailyRewardButton | DailyRewardButton | Mở DailyRewardPopup thủ công |
| UIMainMenu.storeButton | Hai reference đang null | Sảnh hiện có đường vào Store từ navigation/currency |
| UIController.cachedPages.pages | Mảng rỗng | Các UIPage hiện được đặt trực tiếp trong scene |
| UIController.usePausePopups | true | IPausePopup tham gia cơ chế pause |

`UIController.Init()` chỉ quét **con trực tiếp** có UIPage của UI Main Canvas, cache Canvas/raycaster và lập map theo concrete type. `InitPages()` chuẩn bị safe area, currency cloud, rồi PreparePage/Init từng page. Không đặt UIPage mới sâu dưới một thư mục GameObject nếu mong nó tự được quét.

### UIPage và các panel được điều khiển riêng

| GameObject | Component | Owner mở/đóng |
| --- | --- | --- |
| UI Main Menu và các popup UIPage ở sơ đồ | UIMainMenu, UISettings, UIStore... | UIController.ShowPage/HidePage theo type |
| UI leaderboard | LeaderboardPanelView | Navigation và leaderboard view flow |
| Panel_quest | QuestPanelView | Navigation và quest view flow |
| DailyRewardPopup | DailyRewardPopupView | UIMainMenu / view.Show và claim service |
| ProgressPopUp | CoinSafePopupView | ProgressPopupToggle / view flow |
| NoConnectionPopup | NetworkCheckPopup | Navigation/network presentation |
| Tutorial Overlay | TutorialCanvasController | TutorialController của Scripts Holder |

Các panel MonoBehaviour không tự trở thành UIPage chỉ vì nằm trong Canvas. Navigation đã lắng nghe nhiều event popup để ẩn/hiện phù hợp. Khi thêm panel mới, nối event visibility hoặc đưa nó vào contract UIPage nếu phù hợp với thiết kế.

Nhiều root UIPage được lưu `activeSelf = true` trong scene; PreparePage tắt Canvas của chúng. Trạng thái GameObject active không có nghĩa là popup đang hiển thị. Giữ Canvas và GraphicRaycaster của UIPage, vì class yêu cầu hai component này.

<!-- pagebreak -->

### Luồng Menu: từ Awake đến nhấn PLAY

```uml
menu_sequence
```

Daily Reward auto-show hiện tắt; nhấn nút mới mở. PLAY chỉ chuyển scene, chưa áp dụng life lock hoặc chọn level database của game mới.

### Hướng dẫn custom sảnh theo thứ tự

1. Đổi sprite Background và art sảnh trước. Giữ Background ngoài Safe Zone; Safe Zone chỉ chứa nội dung cần tránh notch.
2. Giữ reference trong UIMainMenu và StaticMapPanel. Nếu thay Play Button, kéo Button/TMP mới vào field; không thêm OnClick trùng với listener mà Init đã đăng ký.
3. Thay cấu trúc sảnh/map theo game. StaticMapPanel hiện là shell PLAY, không phải level selection engine. Nếu có chọn level, lưu lựa chọn qua model/session rồi mới gọi LoadGame.
4. Với popup mới, tạo class UIPage cụ thể, đặt trực tiếp dưới UI Main Canvas hoặc khai báo prefab trong CachedPages. Nối view với service của feature; không viết một save/audio controller khác trong popup.
5. Với navigation, giữ Bottom Navigation Canvas riêng và BottomNavigationBannerSpacer. Navigation hạ xuống khi không có banner thực; chỉ chừa chiều cao khi banner đang hiển thị. Kiểm tra cùng safe area trên điện thoại.
6. Nếu game không dùng Quest/Profile/Store, gỡ đường mở, listener và view của feature tương ứng sau khi xác định dependency; tránh để nút trỏ vào page đã xóa.

### Kiểm tra trước khi chuyển sang Game

Menu xuất hiện từ Init, tiền/lives đọc từ save và popup không hiện chồng ngoài ý muốn. Mở/đóng Settings phải khôi phục pause state. PLAY phải vào đúng Game trong Scene List. Thử cả banner hiển thị, banner ẩn và No Ads entitlement để kiểm tra navigation; không đánh giá layout chỉ bằng khoảng trống cố định trong Editor.

Page “not found”: kiểm tra concrete type, con trực tiếp/CachedPages. PLAY lỗi: kiểm tra StaticMapPanel và InitPages. Thao tác chạy hai lần: kiểm tra listener trùng giữa Inspector và code.

**Nguồn:** Menu.unity; MenuController, UIMainMenu, StaticMapPanel, navigation/spacer và UIController.

## 4. Scene Game: HUD, popup và kết nối gameplay

Mở `Assets/Project Files/Game/Scenes/Game.unity`. Scene hiện chứa camera và UI shell; **game mới cần bổ sung board, input và luật chơi**. GameController hiện điều phối trạng thái lượt và scene transition. Nó không tự tạo level, vận hành board hoặc tăng level khi nhấn Next.

### Quan hệ controller và UIPage của Game

```uml
game_structure
```

<!-- pagebreak -->

### Hierarchy GameObject

| GameObject trong Game | Component / nội dung chính |
| --- | --- |
| Main Camera | Camera, CameraController, UniversalAdditionalCameraData |
| UI Main Canvas | Canvas, CanvasScaler, GraphicRaycaster, UIController, GameController |
| UI Main Canvas/UI Game | UIGame; Safe Area, Level/Timer/Power Up HUD, Message Box, Flying Objects, Dev Panel, Tutorial Overlay |
| UI Complete; UI Game Over | UIComplete: Next / Extra Reward; UIGameOver: Revive / Fail panel |
| UI Game Settings; UI No Ads Pop Up; UI Add Lives Panel | UISettings; UINoAdsOffer; UIAddLivesPanel |
| UI Quit Pop Up; UI Feature Announcement Popup | UILevelQuitPopUp; FeatureAnnouncementPopup |
| UI Store; UI Rewards Confirmation Popup | UIStore; UIRewardsConfirmation |
| UI PU Purchase Panel | Placeholder inactive, chưa có script điều phối |


Không có Board Root trong hierarchy này. Khi tạo game, thêm Board Root và controller/game flow riêng dưới một root gameplay để quản lý item, input và level content. Đây là cấu trúc đề xuất để mở rộng, không phải thành phần đã có sẵn. Camera/light/background gameplay được thiết lập theo board 2D/3D của game; không suy ra engine từ HUD hiện tại.

UI Main Canvas phải giữ GameController cùng UIController: Awake của GameController dùng `GetComponent<UIController>()`. Di chuyển riêng GameController sang root khác mà không đổi binding sẽ làm UI không được khởi tạo.

<!-- pagebreak -->

### Inspector và các API nối vào game flow

| Thành phần / field | Giá trị hoặc API có thật | Trách nhiệm |
| --- | --- | --- |
| UIGame.safeAreaRectTransform | UI Game/Safe Area | Đăng ký vùng HUD tránh notch |
| UIGame.gameplayTimer | Timer Visualiser | View timer; không tự tick timer model |
| UIGame.messageBox | Message Box + Text TMP | Hiển thị thông điệp HUD |
| UIGame.powerUpsUIController | Power Up Panel | Thanh booster UI; mechanic cần game tự nối |
| UIComplete.nextLevelButton | Next Level Button | Hiện gọi OnCompleteRewardRecieved |
| UIComplete.extraRewardButton | Extra Reward Button | Hiện cũng gọi cùng callback reload Game |
| UIController.cachedPages.pages | Mảng rỗng | UIPage hiện đặt trực tiếp trong scene |
| GameController.ActivateGame() | Bật cờ IsGameActivated | Gọi khi board/input đã sẵn sàng |
| GameController.GameComplete() | Hoàn tất session + report Quest | Gọi một lần khi rule xác nhận win |
| GameController.ShowCompleteUI() | ShowPage<UIComplete> | Hiện popup sau animation kết quả |

### Điểm compose MVC đề xuất cho game mới

```uml
game_mvc
```

1. Tạo dữ liệu level thuộc game và database ánh xạ index -> level. `LevelData` của framework đang rỗng; chưa có level database tự sinh nội dung cho Game.
2. Tạo controller/game flow của game. Lấy service sau khi Init đã chạy; truyền dependency cho model/presenter khi cần kiểm tra độc lập.
3. Chọn level, bind session, dựng board và input; gọi ActivateGame khi sẵn sàng. Chặn input trước đó và khi kết thúc lượt.
4. Gắn GameplayTimer nếu game cần thời gian. Timer có một owner tick; TimerVisualiser chỉ biểu diễn state. Power-up cũng cần inventory và command/rule của game; đổi icon không tạo mechanic.
5. Nối outcome vào GameController; chọn một owner cho completion/reward để tránh ghi nhận hai lần.

<!-- pagebreak -->

### Luồng một lượt và popup kết quả

```uml
game_sequence
```

<!-- pagebreak -->

### Activity diagram: xử lý outcome khi game đã tích hợp

```uml
game_outcome_activity
```

Đây là flow đề xuất của game mới. Các nhánh diamond thể hiện quyết định; action tăng level, grant reward và phục hồi board/timer không được coi là đã có trong shell.

<!-- pagebreak -->

### Hành vi shell hiện tại và phần cần nối

| Action | Source hiện làm gì? | Game mới phải bổ sung |
| --- | --- | --- |
| Win | GameComplete đặt finished, tắt activated, OnLevelCompleted và Quest report | Rule win, reward một owner, animation và ShowCompleteUI |
| Next Level | UIComplete callback tải lại Game | Tăng/chọn level, lưu state trước reload; không tự xảy ra |
| Extra Reward | Cùng callback tải lại Game | Rewarded placement, kiểm tra kết quả và grant đúng một lần |
| Retry | Replay tải lại Game | Chính sách giữ level, reset board/timer/input |
| Revive | Reset finished/activated, ẩn UIGameOver | Phục hồi board, thêm thời gian và resume timer/input |
| Về Menu | LoadMenu tải Menu | Cleanup dependency/event/tween/pool thuộc lượt |

GameComplete đã ghi OnLevelCompleted; không gọi thêm GameController.OnLevelCompleted cho cùng lượt. `Revive(int seconds)` hiện nhận seconds nhưng chưa dùng để thay đổi timer/board. UI Game Over có flow lấy life ở final failure và quảng cáo qua facade; Play chưa có life lock tương ứng. Game mới cần một owner cho life policy và completion reward.

Trong cấu hình hiện tại, rewarded và interstitial placement đang tắt. Không mô tả nút revive/extra reward là flow quảng cáo production hoàn chỉnh chỉ vì popup có icon ads. Cần nối IAdsService/placement và outcome board thật, theo chương Ads.

<!-- pagebreak -->

### Từng bước thêm gameplay mà vẫn giữ core dùng chung

Chương 26-27 có ví dụ C# cho level, timer, input và board adapter nối shell. Đây là mẫu tích hợp, chưa được cài vào scene template.

1. Thêm root gameplay trong Game và các script thuộc game: level database, model/rule, controller và board view. Giữ implementation này trong Project Files/Game, tách khỏi NebulaSoft Core.
2. Nối controller của game với UIGame sau khi UIController đã InitPages. Không phụ thuộc thứ tự Start giữa hai MonoBehaviour nếu chưa có entry point/ordering rõ ràng; dùng một game entry điều phối bước chuẩn bị.
3. Đăng ký input khi board đã dựng xong. Khi popup pause mở, kết thúc lượt hoặc animation cần khóa thao tác, dùng gate tập trung; không để mỗi item tự quyết định state toàn game.
4. Tại win, cập nhật model/session và reward đúng một lần. Hiện UI Complete sau hiệu ứng. Bind Next qua một flow của game để chọn level tiếp rồi reload; loại listener reload cũ nếu thay bằng listener mới.
5. Tại fail, chọn cho revive hay final fail. Nếu revive thành công, phục hồi state trước khi cho input hoạt động; nếu không thành công, đi tiếp fail flow. Retry giữ level hiện tại theo policy sản phẩm.
6. Popup riêng của game nên kế thừa UIPage, có concrete type riêng và nằm trực tiếp dưới UI Main Canvas hoặc trong CachedPages. Bind field trong Inspector; thực hiện OnPageOpened/Closed theo contract để canvas/pause/event đúng.
7. Khi rời Game, unsubscribe event service, kill tween còn hoạt động, trả object về pool và giải phóng reference của board. Không shutdown Save, Audio hoặc Ads vì một scene/view bị hủy.

### Checklist chạy hoàn chỉnh ba scene

| Ca kiểm tra | Kết quả cần thấy |
| --- | --- |
| Init -> Menu -> Game | Một Initializer/EventSystem hoạt động; HUD đúng và board được tạo |
| Win -> Next | Completion chỉ ghi một lần; Game mới tải level kế tiếp đã chọn |
| Fail -> Retry | Đúng level cũ; board/timer/input trở về trạng thái đầu |
| Fail -> Revive | Board và thời gian phục hồi; popup đóng; tiếp tục chơi được |
| Game -> Menu -> Game | UIController mỗi scene được bind lại; không giữ reference view đã hủy |
| Settings/popup trong lượt | Input khóa/pause đúng; đóng xong tiếp tục; không mắc Time.timeScale = 0 |
| Banner có/không có | HUD/booster không che nhau; background vẫn phủ màn hình |
| Đóng và mở ứng dụng | Save đọc lại progress/currency; không phụ thuộc state chỉ trong scene |

Nếu Game chỉ có HUD, kiểm tra đã thêm board entry chưa. Nếu Next lặp lại level, kiểm tra bước đổi index trước reload. Nếu revive chỉ đóng popup, kiểm tra board/timer adapter. Nếu popup không hiện, kiểm tra UIPage direct child/type, binding Canvas/raycaster và calls InitPages/OnPageOpened.

**Nguồn:** GameController.cs, UIGame.cs, UIComplete.cs, UIGameOver.cs, ActiveSession.cs, UIController.cs và Game.unity. Từ chương 5 trở đi, tài liệu đi sâu vào cấu trúc, module và các ví dụ API để thực hiện những bước trên.

## 5. Cấu trúc thư mục và trách nhiệm

```text
Assets/
  NebulaSoft Core/
    Foundation/Runtime/      Contract nền tảng, clock, callback
    Modules/                 Dịch vụ dùng chung, tách Runtime và Editor
    Features/                Logic Quest, DailyReward, Lives có thể tái sử dụng
    Integration/             Composition và công cụ tích hợp nhiều assembly
    Utilities/Runtime|Editor/ Helper dùng chung
    Shared/                  Prefab, animation, hình ảnh của framework
    Editor/                  Công cụ chung của framework
    Examples/                Ví dụ của core
  Project Files/
    Data/                    Database, settings, catalog của game
    Game/
      Scenes/                Init, Menu, Game
      Scripts/               Bootstrap, controller, session của game
      Features/              Script, prefab, art theo feature của game
      UI/                    UI, font, sprite và popup chung của game
      Integration/           Adapter Firebase và Ads riêng của game
      Prefabs/               Bootstrap và prefab còn lại của game
  Addon/ThirdParty/UIEffect-5.9.6/
  Plugins/VoxelBusters/       Ads Kit và CoreLibrary
  GoogleMobileAds/            Google Mobile Ads SDK
  ExternalDependencyManager/  Resolver native dependency
  Resources/                 Ads Kit settings và banner options
  Tests/Editor/              Test của framework và feature
Packages/                    Manifest và lock dependency
ProjectSettings/             Unity, build, platform, define
docs/                        Hướng dẫn và source tài liệu
tools/Documentation/         Tool dựng PDF, UML và tài liệu Word cũ
output/pdf/                  PDF xuất cho đội phát triển
```

UI mẫu trước đây trong Addon/UI đã chuyển về Game. Quest, DailyReward, Lives, CoinSafe, Profile và Leaderboard gom script/prefab/art trong Game/Features theo feature. UI dùng chung giữa nhiều feature của game nằm trong Game/UI. Logic retention dùng chung vẫn ở NebulaSoft Core/Features; database cấu hình của game tiếp tục ở Project Files/Data. Xem bản đồ di chuyển và quy tắc thêm module trong [cấu trúc dự án](project-structure.md).

| Thay đổi | Nơi đặt |
| --- | --- |
| Rule ghép, sort, merge, đường đi của item | Project Files/Game/Scripts/Gameplay của game mới |
| HUD và popup chung của game | Project Files/Game/UI |
| View, adapter tiến độ và prefab của một feature | Project Files/Game/Features/<Feature> |
| Một service tổng quát tái sử dụng được | NebulaSoft Core/Modules/<Feature> |
| Asset cân bằng, level, audio clip | Project Files/Data và art folder của game |
| Adapter SDK mới hoặc migration API | Provider/adapter ở biên tích hợp |
| Test của feature | Assets/Tests/Editor/<Feature> |

Giữ file `.meta` khi di chuyển asset để giữ GUID. Namespace và save key đã phát hành cần được quản lý như dữ liệu tương thích; đổi folder không có nghĩa là được đổi tên kiểu lưu dữ liệu. Các đường dẫn trong hướng dẫn tính từ root dự án.

## 6. MVC: controller, UI và save tương tác ra sao

Kiến trúc cần được đọc theo trách nhiệm và luồng dữ liệu. Framework hiện dùng controller scene, controller/service của feature, UIPage có logic binding, và dịch vụ persistence dùng static API. Đây là cách phân lớp theo hướng MVC; các page hiện chưa phải passive view hoàn toàn. Không thể xem mọi lớp có chữ Controller là cùng một tầng, hoặc dùng sơ đồ kế thừa UIPage để thay cho kiến trúc MVC.

### Vai trò của các lớp đang có

| Thành phần | Trách nhiệm thực tế | Quan hệ với MVC |
| --- | --- | --- |
| MenuController | Init hệ thống UI của Menu, hiện UIMainMenu, nhận PLAY và tải Game | Controller của scene/sảnh |
| GameController | Init HUD, quản lý cờ active/finished, báo win/fail và yêu cầu popup kết quả | Controller của ván; board/rule do game mới bổ sung |
| UIController | Đăng ký page, InitPages, ShowPage/HidePage, theo dõi popup và pause | Điều phối presentation; không quản lý tiến trình level |
| UIPage và các UI component | Bind button, đọc dữ liệu hiển thị, animate, thông báo page mở/đóng | View cùng logic binding/presenter nhẹ đang nằm trong page |
| ActiveSession | Lấy và giữ LevelSave; cung cấp level hiện tại; cập nhật kết quả ván | Facade của session/model, không phải UI controller |
| CurrencyController / WinStreakProgress | Lấy model save, đổi state của feature, mark dirty, phát change event | Controller/service của feature |
| LevelSave / Currency.Save / WinStreakSave | Giữ tiến trình, số tiền và streak trong RAM; implement ISaveObject | Model state có thể persist |
| SaveController / GlobalSave / wrapper | Load, restore/cache model, Flush/snapshot, điều phối ghi và lifecycle save | Persistence service, không xử lý luật chơi hay render UI |

GameData, LevelData và database là config/content để đọc; khác với player state trong các SaveObject. InitModule tạo/khởi tạo service ở Init, không xử lý intent của người chơi. Model state tồn tại trong save container; page và controller scene được tạo lại khi chuyển scene.

<!-- pagebreak -->

### Quan hệ controller, hệ thống UI và persistence

```uml
mvc_controllers
```

1. MenuController giữ serialized reference tới UIController. GameController tìm UIController trên cùng GameObject. Cả hai gọi Init/InitPages trong Awake rồi yêu cầu page qua API UIController.
2. UIController giữ các UIPage trong pages/pagesLink. Nó gọi Init và animation/canvas của page. PLAY được StaticMapPanel.Init bind thẳng tới MenuController.OnPlayButtonClicked; NEXT được UIComplete.Init bind tới GameController.OnCompleteRewardRecieved. Button không cần đi vòng qua UIController để chạy logic game.
3. GameController gọi ActiveSession.Current.OnLevelCompleted. ActiveSession giữ live LevelSave lấy qua SaveController.GetSaveObject. SaveController giữ GlobalSave; GlobalSave quản lý SavedDataContainer và ISaveObject. Core save không biết kiểu LevelSave cụ thể và không tham chiếu UI.

Sơ đồ này chọn các lớp đại diện để chỉ ra dependency/reference thật; không biểu diễn thứ tự thời gian. Các luồng khởi tạo, thay đổi model và kết quả ván được tách ở những sequence dưới đây. Kế thừa UIPage và interface popup được mô tả riêng ở chương 10.

<!-- pagebreak -->

### Luồng đọc: Init load save, feature lấy model, UI bind dữ liệu

```uml
mvc_restore_sequence
```

SaveInitModule gọi SaveController.Init trước Currencies. SaveController load GlobalSave từ wrapper vào RAM. GetSaveObject<T> tìm container theo key/hash: khôi phục object khi cần hoặc tạo object mặc định nếu chưa có. Những lần lấy lại cùng key/type trả cùng live model đã cache; đây không phải một lần đọc file mới.

CurrencyController.Init lấy Currency.Save và bind vào Currency bằng SetSave. Khi Menu tạo UI, CurrencyUIPanelSimple.Init lấy Currency đã khởi tạo, đọc icon/số tiền, redraw và subscribe OnCurrencyChanged nếu updateOnChange bật. Init của panel có guard nên Awake và UIMainMenu.Init có thể cùng gọi mà không init lại.

ActiveSession.Current cũng là consumer của save: lần đầu tạo session, constructor lấy LevelSave qua GetSaveObject<LevelSave>(). MenuController/GameController không load file riêng; StaticMapPanel hiện chỉ bind PLAY và đặt chữ PLAY/LEVEL, chưa bind nhãn level từ ActiveSession.

<!-- pagebreak -->

### Luồng ghi và hiển thị: đổi tiền là một ví dụ đầy đủ

```uml
mvc_currency_sequence
```

CurrencyController.Add đổi Currency.Amount, mà property này ghi trực tiếp vào Currency.Save.Amount. Sau đó controller gọi MarkAsSaveIsRequired rồi InvokeChangeEvent. Panel nhận OnCurrencyAmountChanged, đọc state mới qua Currency và cập nhật text. CurrencyController không cần giữ reference tới panel; event subscriber nằm ở phía UI.

**Đổi state, refresh UI và ghi file là ba việc khác nhau.** MarkAsSaveIsRequired chỉ đánh dấu dirty; model vẫn đang ở RAM. UI cập nhật ngay từ RAM, trước khi có một lần Save. SaveController không gọi Redraw/ShowPage và OnSaveLoaded không phải event thông báo mọi lần model thay đổi.

Việc persist xảy ra khi gọi Save hoặc từ callback pause/quit; autosave định kỳ chỉ được tạo nếu autoSaveDelay > 0. Project Init Settings hiện có autoSaveDelay = 0, nên không có autosave định kỳ đang chạy. Chương 9 mô tả Flush/snapshot và writer; Save mặc định có thể trả về trước khi worker ghi xong.

<!-- pagebreak -->

### Luồng kết quả ván: session/save và popup có điểm nối riêng

```uml
mvc_win_sequence
```

GameComplete chặn completion lặp, cập nhật cờ ván và gọi ActiveSession.OnLevelCompleted, rồi báo quest. ActiveSession sửa LevelSave và gọi WinStreakProgress.RegisterWin; service streak cập nhật WinStreakSave, mark dirty và phát Changed cho subscriber như WinStreakText. Dirty thuộc GlobalSave nên lần persist sẽ lấy snapshot của cả các SaveObject đã thay đổi.

Game mới gọi ShowCompleteUI sau animation kết quả. UIController hiện UIComplete; NEXT đã được page bind tới OnCompleteRewardRecieved. Callback hiện chỉ reload Game: không tự tăng level, không tự cộng reward và không tự gọi Save. Vì vậy không được vẽ win -> popup -> next level -> persist như một chuỗi tự động đã có trong template.

<!-- pagebreak -->

### Nối gameplay mới theo MVC vào các điểm đang có

| Bước của game mới | Nơi xử lý | Kết nối với framework |
| --- | --- | --- |
| Vào Game, xác định content và tiến trình | Game flow/feature controller | Đọc ActiveSession hoặc model qua GetSaveObject sau Init; lấy layout từ level database của title |
| Người chơi tap/drag item | View/input phát intent; controller xử lý rule | Controller đổi board model; view render kết quả. UIController chỉ điều phối page/popup |
| Xác nhận thắng | Game flow sau khi board giải xong | Gọi GameController.GameComplete đúng một lần; sau animation gọi ShowCompleteUI |
| Nhận reward / chọn NEXT | Controller của policy tiến trình | Xác định reward/level kế; dùng CurrencyController cho tiền; SetLevelIndex và mark dirty rõ ràng cho tiến trình |
| Hiển thị state mới | View bind model hoặc controller cung cấp view data | Subscribe change event nếu feature có; nếu chưa có event, controller gọi refresh có chủ đích |
| Lưu checkpoint | Feature/game flow quyết định thời điểm | MarkAsSaveIsRequired sau mutation; Save/SaveChecked khi cần persist, dùng pipeline SaveController |

Các bước reward/tăng level trong bảng là việc cần tích hợp ở game mới, không phải tính năng NEXT sẵn có. ActiveSession.SetLevelIndex/GetLevelIndex/OnLevelStarted hiện sửa model mà không tự mark dirty hoặc phát event level. Controller tích hợp phải đánh dấu dirty và cập nhật view cho những mutation đó; không suy ra rằng gọi setter là đã ghi file.

Một feature nên giữ một nguồn state chính: lấy model save rồi thao tác qua API của feature. Page gửi intent và hiển thị; controller chịu luật và policy; UIController chịu routing/canvas; SaveController chịu persistence. Với API hiện có, coin panel đọc và subscribe Currency trực tiếp, còn UIProfilePopup vẫn có code sửa save. Khi tách presenter cho game mới, chuyển phần policy ra controller từng feature, tránh tuyên bố toàn bộ source hiện tại đã tách MVC hoàn toàn.

### Source dùng để kiểm chứng phần này

Controller: `Game/Scripts/Controllers/MenuController.cs`, `GameController.cs`. UI: `Game/UI/Scripts/StaticMapPanel.cs`, `UIComplete.cs`, `UIMainMenu.cs`; core `Modules/UI/Runtime/UIController.cs`. State/session: `Game/Scripts/Level System/ActiveSession.cs`, `LevelSave.cs`, `WinStreakProgress.cs`. Currency: `Modules/Currency/Runtime/CurrencyController.cs`, `Currency.cs`, `UI/CurrencyUIPanelSimple.cs`. Persistence: `Modules/Save/Runtime/SaveController.cs`, `GlobalSave.cs`, `SavedDataContainer.cs`. Cấu hình: `Assets/Project Files/Data/Project Init Settings.asset`.

## 7. Scene, vòng đời và thứ tự module

```uml
modules_sequence
```

`Initializer.Init()` bind SystemMessage, Overlay và EventSystem, khởi tạo AnalyticsModules, rồi giữ Initializer bằng DontDestroyOnLoad. `InitModulesCoroutine()` đợi các bước `InitModule.Prepare()` trước khi chạy `InitModules()`, static modules và global MusicSource. `SDKInitializer` khởi tạo SDKBehavior và các ISDKTaskBehavior. Khi mọi task hoàn tất và không có trạng thái Failed/TimedOut, nó gọi `OnUserConsentReceived`. Xem cách xử lý timeout và task tùy chọn ở [Initializer](modules/startup.md#initializer).

`GameLoading.checkNetworkConnection` có thể chặn loading khi mất kết nối. Với game offline, tắt yêu cầu này theo policy sản phẩm; không mô tả luồng hiện tại là luôn bỏ qua network. Manual loading cần gọi `GameLoading.MarkAsReadyToHide()` sau khi sẵn sàng.

### Danh sách đã đăng ký trong Project Init Settings

| Thứ tự | Module | Công việc chính |
| --- | --- | --- |
| 1 | Remote Config | Chuẩn bị config remote, cache và bundled defaults |
| 2 | Save Controller | Load save trước các service phụ thuộc |
| 3 | Analytics | Cấu hình analytics service và provider |
| 4 | Tween | Tạo runtime tween trên Initializer |
| 5 | Audio Controller | Khởi tạo library và pool AudioSource |
| 6 | Currencies | Nạp định nghĩa và balance từ save |
| 7 | Haptic | Chọn wrapper platform |
| 8 | Dev Panel | Link settings dev panel |
| 9 | Game Settings | GameData.Init và adapter Quest |
| 10 | Lives System | Khởi tạo life state/time |
| 11 | Screen Settings | Frame rate và sleep timeout |
| 12 | Quest | Nạp database và progress |
| 13 | Daily Reward | Schedule, clock, state store |
| 14 | Monetization/IAP | Chuẩn bị catalog IAP |
| 15 | Monetization/Ads | Chuẩn bị AdsModuleSettings |

ProjectInitSettings chạy đúng thứ tự serialized trong Inspector. RegisterModule.Order hỗ trợ tạo danh sách ở editor, không phải bộ giải dependency runtime. Đặt module sau mọi service mà nó cần. IAP/Ads đăng ký config ở bước module, kết nối SDK theo luồng monetization sau consent.

UIController hiện thuộc controller scene: Menu gọi Init/InitPages trong Awake, Game cũng gọi trong Awake. Persistent Initializer không đồng nghĩa tất cả page UI đều persistent. Không áp dụng cấu trúc DontDestroyOnLoad của một game khác vào template mà chưa kiểm tra ownership.

## 8. Thêm module mới đúng phạm vi

Ví dụ một feature tiến trình riêng: config nằm trong asset, module đăng ký service, service lấy save qua SaveController. Mẫu này không thay ActiveSession có sẵn; dùng khi game mới cần state bổ sung.

```csharp
using System;
using NebulaSoft;
using UnityEngine;

namespace FrameworkGuide.Samples
{
    [CreateAssetMenu(menuName = "Game/Progress Config")]
    public sealed class GuideProgressConfig : ScriptableObject
    {
        public int LevelCount = 50;
    }

    [Serializable]
    public sealed class GuideProgressSave : ISaveObject
    {
        public int NextLevelIndex;
        public void Flush() { }
    }

    [RegisterModule("Game/Progress", core: false)]
    public sealed class GuideProgressInitModule : InitModule
    {
        [SerializeField] GuideProgressConfig config;
        public override string ModuleName => "Game Progress";
        public override void CreateComponent()
        {
            GuideProgressService.Init(config);
        }
    }

    [StaticUnload]
    public static class GuideProgressService
    {
        public static GuideProgressSave State { get; private set; }
        public static void Init(GuideProgressConfig config)
        {
            if (config == null || config.LevelCount <= 0)
                throw new InvalidOperationException("Missing config");
            State = SaveController.GetSaveObject<GuideProgressSave>(
                "game.progress.v1");
            State.NextLevelIndex = Mathf.Clamp(
                State.NextLevelIndex, 0, config.LevelCount - 1);
        }
        private static void UnloadStatic() { State = null; }
    }
}
```

Tạo config asset, thêm Game/Progress trong Project Init Settings và đặt sau Save. Controller chỉ đọc service sau bootstrap. Clamp trong mẫu chuẩn hóa dữ liệu runtime; nếu muốn lưu lại giá trị chuẩn hóa thì phải mark dirty. Không đăng ký cùng service một lần ở module và một lần nữa ở Awake.

Mẫu module cần quyết định rõ lifetime, reset static, save identity, validation và thứ tự khởi tạo. Dùng `core: true` khi mọi game đều bắt buộc cần feature đó; gameplay của từng title thường để `core: false`.

## 9. Save: model, dirty state và controller

Nguồn: `Modules/Save/Runtime/SaveController.cs`, `ISaveObject.cs`, `GlobalSave.cs`. SaveInitModule khởi tạo trước service đọc dữ liệu. Feature sở hữu model; SaveController sở hữu load/serialize/ghi và autosave.

```uml
save_sequence
```

```csharp
using NebulaSoft;

namespace FrameworkGuide.Samples
{
    public sealed class GuideProgressController
    {
        private readonly GuideProgressSave save;
        public GuideProgressController()
        {
            save = SaveController.GetSaveObject<GuideProgressSave>(
                "game.progress.v1");
        }
        public void SetNextLevel(int index)
        {
            save.NextLevelIndex = index;
            SaveController.MarkAsSaveIsRequired();
            SaveController.Save();
        }
    }
}
```

<!-- pagebreak -->

### Persist và tương thích dữ liệu

| API | Dùng khi | Ý nghĩa hoàn tất |
| --- | --- | --- |
| GetSaveObject<T>(key) | Lấy state feature bằng key ổn định | Trả object sống do GlobalSave quản lý |
| MarkAsSaveIsRequired() | Sau mutation cần persist | Đánh dấu revision dirty |
| Save() | Tiến trình thông thường | Snapshot xong, IO có thể còn trong queue |
| Save(useThreads: false) | Cần chờ ghi thông thường | Chờ IO; lỗi được log, state còn dirty |
| SaveChecked() | Luồng cần biết chắc lần ghi có thành công | Chờ IO và throw nếu ghi thất bại |
| SaveCustom / PresetsSave | Save/preset riêng | Có barrier với queued write |

Model dùng public field hoặc SerializeField phù hợp JsonUtility; interface bắt buộc Flush(). Nếu model chỉ chứa field đã serialize thì Flush có thể rỗng. Nếu có cache runtime/dictionary được chuyển sang dữ liệu serialize, Flush phải đồng bộ trước snapshot. Không tự ghi PlayerPrefs hay tạo save file thứ hai cho cùng state.

Mutation và Flush chạy main thread. Pause/quit flush đồng bộ; Init idempotent, không load lại đè state ở mỗi scene. Giữ key, enum value, serialized type và schema tương thích sau release. Native ghi JSON có version và đọc được BinaryFormatter legacy. Mỗi model có version/migration độc lập; Firebase đồng bộ model đã bật CloudSync. Xem [Save, version và migration](save-module.md) để thêm dữ liệu và cập nhật schema.

## 10. UI: page, popup và controller

Nguồn: `Modules/UI/Runtime/UIController.cs`, `UIPage.cs`, `IPopupWindow.cs`, `IPausePopup.cs`. Page cần Canvas và GraphicRaycaster; root UIController cần danh sách page hoặc cached prefab được đăng ký đúng.

Sơ đồ sau chỉ mô tả hệ thống type của UI. Quan hệ controller với model/save và các luồng MVC nằm ở chương 6.

```uml
ui_classes
```

<!-- pagebreak -->

### Binding page và callback

Controller scene gọi Init rồi InitPages một lần. Controller feature gọi ShowPage/HidePage; page phụ trách binding và animation. ShowPage tự EnableCanvas; page báo OnPageOpened khi show hoàn tất. HidePage tắt raycast trước; page báo OnPageClosed khi hide hoàn tất để controller tắt canvas và phát callback.

```csharp
using NebulaSoft;
using UnityEngine;
using UnityEngine.UI;

namespace FrameworkGuide.Samples
{
    public sealed class GuideHelpPopup : UIPage, IPopupWindow
    {
        [SerializeField] Button closeButton;
        private bool initialized;
        public bool IsOpened => IsPageDisplayed;
        public override void Init()
        {
            if (initialized) return;
            initialized = true;
            closeButton.onClick.AddListener(Close);
        }
        public void Close() => UIController.HidePage<GuideHelpPopup>();
        public override void PlayShowAnimation()
            => UIController.OnPageOpened(this);
        public override void PlayHideAnimation()
            => UIController.OnPageClosed(this);
        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
        }
    }

    public sealed class GuideHelpController : MonoBehaviour
    {
        public void OpenHelp() => UIController.ShowPage<GuideHelpPopup>();
    }
}
```

Tạo prefab GuideHelpPopup, gán closeButton, đặt page trong UIController.Pages hoặc CachedPages. Bind nút Help tới GuideHelpController.OpenHelp. Mẫu dùng transition tức thời; thêm animation rồi chỉ báo opened/closed ở cuối animation. Không gọi OnPageClosed thêm lần nữa trong OnDestroy.

Để popup pause gameplay, implement thêm IPausePopup và bật usePausePopups ở UIController. UIController quản lý danh sách pause popup và Time.timeScale. Animation của popup pause cần unscaled time. Nếu rule/input dùng clock riêng, game controller phải tự pause rule đó khi popup mở; không chỉ dựa vào Time.timeScale.

## 11. Safe area, background và banner layout

Tên API thực tế là `NotchSaveArea` dù file tên `NotchSafeArea.cs`. Nó chuyển Screen.safeArea thành anchor cho RectTransform đã đăng ký. Background full screen đặt ngoài vùng safe; chỉ HUD, button và nội dung tương tác nằm trong safe panel.

```text
Canvas
  FullScreenBackground       stretch toàn màn hình
  SafeContent                NotchSaveArea quản lý anchors
    TopHUD
    MainContent
    BottomNavigation         BottomNavigationBannerSpacer
  PopupLayer                 theo layout của từng page
```

```csharp
using NebulaSoft;
using UnityEngine;

namespace FrameworkGuide.Samples
{
    public sealed class GuideSafePanel : MonoBehaviour
    {
        [SerializeField] RectTransform safePanel;
        private void Start()
        {
            NotchSaveArea.RegisterRectTransform(safePanel);
        }
    }
}
```

Không đăng ký lặp lại cùng panel qua mỗi OnEnable. Page hiện hữu có thể đã đăng ký safe area; kiểm tra trước khi thêm mẫu. Không xóa Canvas trong khi GraphicRaycaster hoặc UIPage còn phụ thuộc vào nó.

BottomNavigationBannerSpacer và PowerUpBannerSpacer thuộc layer game UI. Chúng nghe BannerLayoutChanged và đổi pixel sang tọa độ parent/canvas. Không banner thì navigation về vị trí nền; khi SDK báo banner thật có kích thước thì mới nâng lên. Không hardcode 320x50 hoặc cộng safe-area inset hai lần.

Ads Kit adapter hiện đọc ad-view proxy public, hỗ trợ bottom banner với inset 0. Khi SDK không báo kích thước dương, không giữ một khoảng trống tự đoán. Adaptive banner trong Editor có thể không cung cấp metric; kiểm tra geometry trên thiết bị trước release.

## 12. Audio: SFX, music và settings

AudioInitModule nhận `Project Files/Data/Audio Clips.asset`, AudioSource pool và default 3D settings. AudioController khởi tạo sau Save; volume dùng state của framework. Global music được Initializer khởi tạo sau các module.

```csharp
using NebulaSoft;
using UnityEngine;

namespace FrameworkGuide.Samples
{
    public sealed class GuideAudioFeedback : MonoBehaviour
    {
        [SerializeField] AudioClip selectClip;
        [SerializeField] AudioClip matchClip;
        public void OnItemSelected()
            => AudioController.PlaySound(selectClip);
        public void OnMatch(Vector3 worldPosition)
            => AudioController.PlaySound(matchClip, worldPosition);
        public void SetSoundEnabled(bool enabled)
            => AudioController.SetVolume(NebulaSoft.AudioType.Sound,
                enabled ? 1f : 0f);
        public void SetMusicEnabled(bool enabled)
            => AudioController.SetVolume(NebulaSoft.AudioType.Music,
                enabled ? 1f : 0f);
    }
}
```

Game controller gọi feedback sau khi action được rule chấp nhận. Clip gán bằng Inspector hoặc asset library, không tạo manager âm thanh song song. Overload vị trí dành cho 3D audio; UI và board 2D thường dùng overload không vị trí. `minDelay` giúp hạn chế âm thanh lặp quá dày khi cần. Dùng NebulaSoft.AudioType để tránh trùng tên với UnityEngine.AudioType.

### Nhạc theo scene

Gắn MusicSource và AudioSource, gán clip, để loop do MusicSource.Init thiết lập. Với nhạc scene riêng, chọn auto activate hoặc gọi Init/Activate một lần khi scene sẵn sàng. Không tự gọi Init lần nữa khi đã bật auto activate. MusicSource nhận VolumeChanged và bỏ subscription khi destroy. Nếu chỉ cần nhạc xuyên scene, dùng globalMusicSource có sẵn trên Initializer.

MusicSource.Activate chuyển nguồn nhạc active, Fade dùng Tween. Controller không nên tự SetVolume trên AudioSource ngoài service vì sẽ lệch settings và volume multiplier. UI button dùng UIAudioFeedback hiện hữu; không phát thêm click từ nhiều listener cho cùng nút.

## 13. Haptic: feedback theo intent

HapticInitModule khởi tạo wrapper platform. Controller chọn intent như select/match/fail; Haptic xử lý setting, priority và native implementation. Haptic không quyết định item nào được chọn.

```csharp
using NebulaSoft;

namespace FrameworkGuide.Samples
{
    public sealed class GuideHapticFeedback
    {
        public void Selected() => Haptic.Play(Haptic.HAPTIC_LIGHT);
        public void Matched() => Haptic.Play(Haptic.HAPTIC_MEDIUM);
        public void Failed() => Haptic.Play(Haptic.HAPTIC_HARD);
        public void SetEnabled(bool enabled) => Haptic.IsActive = enabled;
    }
}
```

Có thể tạo/register HapticPattern và dùng overload priority khi game có nhiều feedback cạnh tranh. Không gọi API Android/iOS trực tiếp từ item hoặc UI. UIHapticFeedback hiện hữu phụ trách button; tránh rung hai lần do vừa page feedback vừa controller feedback.

Editor chỉ kiểm tra gọi API và state; cảm giác rung phải đối chiếu thiết bị. Độ mạnh, thời gian và hỗ trợ pattern khác nhau theo platform.

## 14. Tween và Pool: lifetime của presentation

TweenInitModule tạo runtime Tween trên Initializer. TweenCase là handle để kill/complete animation. Controller giữ handle thuộc object của mình, không để callback chạy sau khi view bị hủy. Popup pause dùng unscaledTime.

```csharp
using NebulaSoft;
using UnityEngine;

namespace FrameworkGuide.Samples
{
    public sealed class GuideFadeView : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        private TweenCase fade;
        public void FadeIn()
        {
            fade.KillActive();
            fade = Tween.DoFloat(group.alpha, 1f, 0.2f,
                value => group.alpha = value, unscaledTime: true);
        }
        private void OnDisable() => fade.KillActive();
    }
}
```

Pool dùng lại GameObject dựa vào active state. GetPooledObject tự Init nếu cần và activate object; trả về null khi pool bị cap và hết chỗ. Trả object bằng SetActive(false), không Destroy từng item trong pool.

```csharp
using NebulaSoft;
using UnityEngine;

namespace FrameworkGuide.Samples
{
    public sealed class GuideMatchVfx : MonoBehaviour
    {
        [SerializeField] GameObject prefab;
        private Pool pool;
        private void Start()
        {
            pool = new Pool(prefab, "Match VFX", transform);
            pool.CreatePoolObjects(8);
        }
        public void PlayAt(Vector3 position)
        {
            GameObject item = pool.GetPooledObject();
            if (item == null) return;
            item.transform.position = position;
            item.GetComponent<ParticleSystem>().Play();
        }
        private void OnDestroy() { pool?.Clear(); }
    }
}
```

VFX prefab phải có ParticleSystem, tắt Play On Awake và chọn Stop Action = Disable để trở lại pool khi hết effect. Nếu prefab có nhiều particle con hoặc animation, viết component return riêng sau khi toàn bộ effect xong. Khi reuse, reset transform, color, trail, animation và listener. Pool này thuộc scene và được Clear khi owner destroy; không giữ reference scene trong persistent pool.

## 15. Currency và Reward: economy chung

CurrencyInitModule nhận `Currency Database.asset`; CurrencyController nạp balance qua SaveController. CurrencyType hiện có Coins = 0; không đổi thứ tự enum đã phát hành. Service owns balance; UI nhận callback và chỉ hiển thị.

```csharp
using NebulaSoft;
using TMPro;
using UnityEngine;

namespace FrameworkGuide.Samples
{
    public sealed class GuideCoinHud : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        private void OnEnable()
        {
            CurrencyController.SubscribeGlobalCallback(OnChanged);
            Refresh();
        }
        private void OnDisable()
            => CurrencyController.UnsubscribeGlobalCallback(OnChanged);
        private void OnChanged(Currency currency, int difference)
            => Refresh();
        private void Refresh()
            => label.text = CurrencyController.Get(
                CurrencyType.Coins).ToString();
        public bool TrySpend(int price)
        {
            if (price <= 0 || !CurrencyController.HasAmount(
                CurrencyType.Coins, price)) return false;
            CurrencyController.Substract(CurrencyType.Coins,
                price, "booster_purchase");
            return true;
        }
    }
}
```

Tên API trong source là `Substract` (giữ cách viết hiện tại). Kiểm tra affordability và trừ trên main thread; không trừ tiền trong mỗi frame hoặc mỗi lần UI refresh. Các API currency cập nhật save state theo framework; chọn checkpoint Save khi policy game cần persist.

<!-- pagebreak -->

### Reward module

RewardsSet chứa danh sách Reward đã đăng ký, hỗ trợ ApplyReward, RestoreReward và preview. Reward View/RewardsHolder hiển thị; controller/feature quyết định thời điểm grant. IAPManager grant reward từ catalog nên callback UI không grant lại.

```csharp
using NebulaSoft;
using UnityEngine;

namespace FrameworkGuide.Samples
{
    public sealed class GuideClaimController : MonoBehaviour
    {
        [SerializeField] RewardsSet reward;
        private bool claimed;
        public void ClaimOnceThisSession()
        {
            if (claimed) return;
            claimed = true;
            reward.ApplyReward();
            SaveController.MarkAsSaveIsRequired();
            SaveController.Save();
        }
    }
}
```

Mẫu chỉ chống double tap trong một instance, không chống claim lại sau reload. Claim vĩnh viễn phải lưu claim identity và policy của feature. Custom reward phải mutate đồng bộ và đáng tin cậy; một RewardsSet không tự rollback các reward trước đó nếu reward sau thất bại. Không xem reward preview như bằng chứng đã grant.

## 16. Ads: contract, cấu hình và loose coupling

Ads là module riêng tại `NebulaSoft Core/Modules/Ads`. Contracts và Runtime là assembly tách biệt không tham chiếu Unity hoặc SDK. Configuration tạo định nghĩa thuần từ ScriptableObject. AdsModule sở hữu lifetime; AdsService sở hữu request/policy; provider dịch SDK. AdsSaveBridge nối No Ads với save cũ, AdsGameBridge nối UI/analytics của game.

```uml
ads_classes
```

<!-- pagebreak -->

### Class diagram: ownership và save boundary của Ads

```uml
ads_lifetime
```

AdsModule tạo và Dispose AdsService nên dùng composition. AdsSaveBridge giữ reference IAdsService / AdSave và gọi SaveController; runtime AdsService không truy cập save trực tiếp.

| Asset/kiểu | Vai trò |
| --- | --- |
| AdsModuleSettings | Provider, catalog, default banner/interstitial/rewarded |
| AdsCatalog + AdPlacement | ID ổn định, format, enabled, preload, cooldown |
| AdsKitProvider.asset | Mapping ID framework sang vendor placement |
| AdsKitAdMobConsent.asset | Factory consent: Editor simulation/native UMP |
| Resources/AdsKitSettings.asset | Vendor app/unit IDs, network, Test Mode |
| GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset | App ID native Android/iOS |
| IAdsService | Gameplay API; không expose vendor type |

Hiện có menu_banner bật, level_transition và rewarded tắt. Muốn dùng rewarded/interstitial phải bật placement tương ứng trong asset. SDK Auto Init và Auto Load tắt để module điều khiển. Test IDs của Google đã gán Android/iOS; Force Test Mode đang bật. Provider được chọn là Ads Kit; có thể đổi sang Dummy bằng asset.

### Presenter thuần cho continue

```csharp
using System.Threading;
using System.Threading.Tasks;
using NebulaSoft.Ads;

namespace FrameworkGuide.Samples
{
    public sealed class GuideContinuePresenter
    {
        private readonly IAdsService ads;
        private readonly string placementId;
        public GuideContinuePresenter(IAdsService ads, string id)
        {
            this.ads = ads;
            placementId = id;
        }
        public async Task<bool> TryContinueAsync(CancellationToken token)
        {
            AdResult result = await ads.ShowAsync(placementId, token);
            return result.RewardGranted && !token.IsCancellationRequested;
        }
    }
}
```

Presenter không gọi Ads Kit, không tự Init SDK và không tự tạo AdsModule. Composition root truyền AdsManager.Service và ID từ AdPlacement. Core có overload extension nhận placement asset; mẫu dùng string để presenter không cần biết Unity asset.

```csharp
using System;
using System.Threading;
using NebulaSoft.Ads;
using UnityEngine;

namespace FrameworkGuide.Samples
{
    public sealed class GuideContinueButton : MonoBehaviour
    {
        [SerializeField] AdPlacement placement;
        private readonly CancellationTokenSource lifetime =
            new CancellationTokenSource();
        private GuideContinuePresenter presenter;
        private Action revive;
        private bool pending;
        public void Bind(IAdsService ads, Action reviveAction)
        {
            presenter = new GuideContinuePresenter(ads, placement.Id);
            revive = reviveAction;
        }
        public async void OnClicked()
        {
            if (presenter == null || pending) return;
            pending = true;
            try
            {
                if (await presenter.TryContinueAsync(lifetime.Token))
                    revive?.Invoke();
            }
            catch (Exception error) { Debug.LogException(error); }
            finally { pending = false; }
        }
        private void OnDestroy()
        {
            lifetime.Cancel();
            lifetime.Dispose();
        }
    }
}
```

Gán rewarded placement vào button controller và Unity Button.OnClick tới OnClicked. Game controller gọi Bind sau bootstrap; chỉ action revive được inject mới thay gameplay. Đóng ad mà không nhận reward sẽ không revive. Async void chỉ nằm ở biên Unity event; presenter trả Task để test được.

### Kết quả, No Ads và update SDK

ShowAsync trả explicit result: Busy, NotReady, Disabled, Cancelled, Failed hoặc Disposed. Chỉ RewardGranted cho phép grant; không suy ra reward từ ad-close. Mỗi request có ID, callback cũ không được grant cho request mới. Fullscreen có lock chung; cancel caller không thể đóng native ad, lock được giữ tới khi native đóng.

No Ads chặn banner/interstitial nhưng giữ rewarded. Dùng facade entitlement hiện hữu/NoAdsReward hoặc AdsSaveBridge để persist; `SetForcedAdsDisabled` chỉ là service policy, tự gọi nó không thay save entitlement. Subscribe BannerChanged/StatusChanged đúng lifetime và unsubscribe khi view destroy.

Khi Ads Kit update: giữ gameplay và save identity, cập nhật vendor nguyên bản, sửa dịch API trong Providers/AdsKit nếu cần, validate mapping/consent rồi test lại. Modern adapter dùng ReadyToShow/Loading/Showing. Flag NEBULASOFT_ADSKIT_LEGACY_STATES chỉ dành API cũ Ready/Waiting; không bật trên SDK 2.3.0.

## 17. IAP: catalog, prefab và controller

IAPInitModule chuẩn bị standalone `Project Files/Data/IAP Settings.asset`; monetization SDK behavior kết nối store sau consent. Với module đã đăng ký, setting module là nguồn chính, kể cả khi disabled. Luồng legacy giữ tương thích cho dự án chưa có module.

### Thêm sản phẩm bằng Inspector

1. Add product trong IAP Settings, đặt key ổn định như coins_2500.
2. Gán store ID Android/iOS đúng, chọn Consumable/NonConsumable theo ý nghĩa.
3. Gán RewardsSet. Preview price phục vụ editor; giá bán thật lấy từ store.
4. Dùng prefab IAPRewardsHolder + IAPButton, chọn product từ catalog dropdown.
5. Gán purchase button, price text, reward views và loading refs. Bộ component tự bind purchase; không thêm OnClick mua lần hai.

```csharp
using NebulaSoft;
using TMPro;
using UnityEngine;

namespace FrameworkGuide.Samples
{
    public sealed class GuideStoreController : MonoBehaviour
    {
        [SerializeField] string productKey = "coins_2500";
        [SerializeField] TMP_Text price;
        private void OnEnable()
        {
            IAPManager.StateChanged += Refresh;
            IAPManager.ProductPurchaseCompleted += OnPurchased;
            Refresh();
        }
        private void OnDisable()
        {
            IAPManager.StateChanged -= Refresh;
            IAPManager.ProductPurchaseCompleted -= OnPurchased;
        }
        private void Refresh()
            => price.text = IAPManager.GetProductLocalPriceString(
                productKey);
        public void Buy() => IAPManager.BuyProduct(productKey);
        public void Restore() => IAPManager.RestorePurchases();
        private void OnPurchased(string key)
        {
            if (key == productKey) Refresh();
            // Reward da duoc IAPManager grant; khong grant lai.
        }
    }
}
```

<!-- pagebreak -->

### Biên mua hàng và khôi phục

Mẫu dành custom view không dùng IAPButton tự bind. ProductPurchaseFailed nhận key và PurchaseFailureReason để UI hiện lỗi; StateChanged cập nhật busy/ready. Manager xử lý giao dịch dù offer đã bị destroy. Button không sở hữu transaction.

`GetIAPItemByKey` tìm catalog key; `GetIAPItem(string)` tìm store ID. Hai identity khác nhau. Key đã phát hành không được rename tùy ý. Legacy enum giữ các save cũ; product mới dùng key. Manager deduplicate transaction ID và persist trước khi acknowledge; retry sau save failure không grant lại giao dịch đã mutate.

Không có Unity Purchasing thì dùng DummyIAPWrapper. SDK IAP vẫn optional; cài Ads Kit không cài Unity IAP. Use Fake Store là mô phỏng, không kết nối store thật. Product/account/receipt và sandbox purchase cần cấu hình/test riêng. Chi tiết migration ở docs/iap-module.md.

## 18. Analytics: report event và adapter

Initializer gọi AnalyticsModules.Init. AnalyticsController phát event typed; provider analytics được nối qua module/adapter. Core event enum hiện tập trung vào currency, IAP và Ads; không có sẵn event LevelStart cho mọi game.

```csharp
using NebulaSoft;
using UnityEngine;

namespace FrameworkGuide.Samples
{
    public sealed class GuideAnalyticsObserver : MonoBehaviour
    {
        private void OnEnable()
            => AnalyticsController.EventFired += OnEvent;
        private void OnDisable()
            => AnalyticsController.EventFired -= OnEvent;
        private void OnEvent(AnalyticsEventType type,
            IAnalyticsEventData data)
            => Debug.Log("Analytics event: " + type);
    }
}
```

Gắn observer mẫu để xem dispatch local; nó không gửi backend. CurrencyController.Add/Substract đã có analytics source/sink parameter; AdsGameBridge phát RV/interstitial event cho compatibility flow. Không report cùng một intent ở nhiều lớp.

Event gameplay mới nên có schema riêng trong game adapter/presenter: ID level, lần chơi, outcome và duration. Adapter dịch sang SDK analytics; board không import Firebase/SDK. Không thêm enum/event dùng riêng một title vào core mà chưa quyết định API version chung.

## 19. Skins: database, chọn và áp dụng visual

SkinController là component scene với SkinsHandler chứa AbstractSkinDatabase providers. Gọi Init sau Save và trước UI skin. Controller quản lý unlock/select và save; game view tự chuyển sprite/model/material theo ISkinData.

```csharp
using NebulaSoft;
using UnityEngine;

namespace FrameworkGuide.Samples
{
    public sealed class GuideSkinMenu : MonoBehaviour
    {
        [SerializeField] SkinController skins;
        private void OnEnable() => SkinController.SkinSelected += Changed;
        private void OnDisable() => SkinController.SkinSelected -= Changed;
        public void Select(string id)
        {
            if (skins.IsSkinUnlocked(id)) skins.SelectSkin(id);
        }
        private void Changed(ISkinData data)
        {
            // View cua game ap dung sprite/model tu data.
            Debug.Log("Selected skin changed");
        }
    }
}
```

Gán SkinController đã được scene owner Init, database và view preview phù hợp. UnlockSkin(id, select) chỉ nên được gọi bởi reward/purchase policy. ID skin phải ổn định sau release. Core không tự biết một skin phải thay material hay sprite nào trong board của game mới.

## 20. Lives: life lock và game flow

LivesSystemInitModule nhận Lives Data.asset, đọc LivesSave qua SaveController và phục hồi theo thời gian. UI dùng StatusChanged/LivesStatus; game flow quyết định thời điểm lock và tiêu hao life.

```csharp
using NebulaSoft;
using UnityEngine;

namespace FrameworkGuide.Samples
{
    public sealed class GuidePlayWithLives : MonoBehaviour
    {
        public void Play()
        {
            if (LivesSystem.IsLocked) return;
            if (!LivesSystem.InfiniteMode && LivesSystem.Lives <= 0)
                return; // Mo popup them lives cua game tai day.
            LivesSystem.LockLife();
            MenuController.LoadGame();
        }
        public void FinalFailure()
            => LivesSystem.UnlockLife(decrease: true);
        public void Win()
            => LivesSystem.UnlockLife(decrease: false);
    }
}
```

Mẫu là policy lựa chọn, không phải behavior hiện có của MenuController.OnPlayButtonClicked: source hiện chỉ load scene Game. Khi áp dụng, bind nút Play tới mẫu thay vì thêm listener cạnh listener load cũ. Chỉ gọi FinalFailure khi thực sự kết thúc, không gọi ngay lúc hiển thị revive popup. Không tiêu hao thêm life khi ad đóng mà chưa quyết định outcome.

Luồng quit phải chọn decrease hay release rõ ràng. InfiniteMode và max-life reward dùng service; không sửa trực tiếp LivesStatus từ label. Khóa life và save checkpoint thuộc game flow, tránh hai controller cùng lock/unlock.

## 21. Quest: report từ controller và claim từ UI

QuestInitModule nhận Quest Database.asset sau Save. QuestService quản lý period, definition, progress và claim. Các goal hiện có: CompleteLevels, SpendCoins, UsePowerUp, PurchasePowerUp, OnlineMinutes và WatchRewardedAds.

```csharp
using NebulaSoft;

namespace FrameworkGuide.Samples
{
    public sealed class GuideQuestHooks
    {
        public void BoosterActuallyUsed()
            => QuestService.ReportProgress(QuestGoalType.UsePowerUp);
        public void CoinsActuallySpent(int amount)
        {
            if (amount > 0) QuestService.ReportProgress(
                QuestGoalType.SpendCoins, amount);
        }
        public bool Claim(string questId)
            => QuestService.TryClaim(questId);
    }
}
```

Report khi action đã thành công. Click booster nhưng board từ chối không được tăng quest. GameController.GameComplete đã report CompleteLevels; không report lần hai ở board bridge. Kiểm tra callback purchase/ad của game để chọn một owner report, không đếm song song ở UI và service.

UI subscribe DataChanged/QuestChanged, đọc GetProgress/GetDefinitions và gọi TryClaim. Không tự grant QuestReward từ view khi TryClaim đã grant. RefreshPeriods quản lý reset period; local quest clock không phải xác minh server. Quest reward phụ thuộc feature khả dụng; thanh power-up UI-only không làm cho reward booster tự hoạt động.

## 22. Daily Reward: schedule, UTC và claim

DailyRewardInitModule nhận Daily Reward Database.asset. Service có IDailyRewardScheduleProvider, IDailyRewardClock và IDailyRewardStateStore, thuận tiện thay schedule/clock/storage và unit test. Default local state dùng SaveController.

```csharp
using NebulaSoft;
using UnityEngine;
using UnityEngine.UI;

namespace FrameworkGuide.Samples
{
    public sealed class GuideDailyClaim : MonoBehaviour
    {
        [SerializeField] Button claimButton;
        private void OnEnable()
        {
            DailyRewardService.StateChanged += Refresh;
            Refresh();
        }
        private void OnDisable()
            => DailyRewardService.StateChanged -= Refresh;
        private void Refresh()
            => claimButton.interactable =
                DailyRewardService.GetState().CanClaimToday;
        public void Claim()
        {
            DailyRewardService.TryClaimToday();
            Refresh();
        }
    }
}
```

Bind nút Claim một lần tới Claim; service quyết định eligibility và grant, view chỉ render. Mỗi ngày UTC claim tối đa một lần. Bỏ hơn một ngày reset sequence về đầu; clock lùi trước ngày claim cuối không được claim tiếp. Schedule cần reward khả dụng; thiếu feature grant sẽ trả false.

Không dùng DateTime.Now trong popup để tính một policy khác service. Nếu cần thời gian server, cung cấp clock adapter ở composition root và quy định fallback. ResetForTesting dành QA/dev, không gắn vào flow claim production.

## 23. Tutorial và feature presentation

TutorialController.Init nhận TutorialCanvasController. BaseTutorial đăng ký khi OnEnable và bỏ đăng ký khi OnDisable. ITutorial định nghĩa Init, StartTutorial, FinishTutorial, Unload cùng active/finished state. GetTutorial có thể tự Init.

Lưu ý source: ActivateTutorial chỉ bảo đảm Init và xử lý skip; nó không gọi StartTutorial. Controller muốn bắt đầu tutorial phải kiểm tra finished rồi gọi StartTutorial theo policy của game.

```csharp
using NebulaSoft;
using UnityEngine;

namespace FrameworkGuide.Samples
{
    public sealed class GuideFirstTapTutorial : BaseTutorial
    {
        [SerializeField] GameObject focusOverlay;
        private SimpleBoolSave save;
        private bool active;
        public override bool IsActive => active;
        public override bool IsFinished => save != null && save.Value;
        public override int Progress => IsFinished ? 1 : 0;
        public override void Init()
        {
            if (isInitialised) return;
            save = SaveController.GetSaveObject<SimpleBoolSave>(
                "tutorial.first_tap.v1");
            isInitialised = true;
        }
        public override void StartTutorial()
        {
            Init();
            if (IsFinished) return;
            active = true;
            focusOverlay.SetActive(true);
        }
        public override void FinishTutorial()
        {
            Init();
            save.Value = true;
            SaveController.MarkAsSaveIsRequired();
            Unload();
        }
        public override void Unload()
        {
            active = false;
            if (focusOverlay != null) focusOverlay.SetActive(false);
        }
    }
}
```

Game controller gọi tutorial.StartTutorial khi board sẵn sàng và FinishTutorial khi nhận đúng action được rule chấp nhận. Overlay phải làm rõ target, dim phần còn lại và chặn input đúng vùng theo game; framework không tự biết item nào được highlight. Mẫu chỉ bật/tắt focusOverlay, không triển khai shader cutout hoặc chọn item.

## 24. Remote Config, profile và leaderboard

Remote Config là module tùy chọn trong **Window → NebulaSoft Core → Project Init Settings**. Template đã gán `Remote Config Defaults.json` vào **Local Config**. Có thể chỉnh profile puzzle trong file này để kiểm tra ngay ở lần khởi động kế tiếp. Nếu asset khởi tạo khác chưa có mục này, dùng **Add Module → Remote Config**.

1. Đặt một JSON object ở URL HTTPS trực tiếp, chẳng hạn trên CDN. Mỗi key là một giá trị hoặc object. **Copy Example JSON** tạo payload mẫu từ các lớp `RemoteConfigData`. Chỉ giữ các key muốn áp dụng.
2. Điền **Endpoint**, chọn **Timeout Seconds** và gán **Local Config** nếu muốn đóng gói một file JSON dự phòng. Khi để Endpoint trống, module đọc Local Config nếu đã gán.
3. Chạy scene **Init**. Bootstrap đợi các bước `Prepare()` trước khi chạy `CreateComponent()`, nên Currency, Ads, Game Settings và Lives đọc cùng một snapshot ngay lúc khởi tạo. Nếu muốn vào game khi mất mạng, giữ tùy chọn **Check Network Connection** của `GameLoading` tắt như trong scene mẫu.

Module ưu tiên snapshot HTTPS hợp lệ, rồi đến cache hợp lệ gần nhất của cùng URL, rồi Local Config. Nếu không có snapshot, mỗi tính năng dùng mặc định của nó. Mỗi lần tải thành công thay cả snapshot. Bỏ một key khỏi JSON sẽ trả tính năng về mặc định, còn field thiếu trong một object dùng giá trị của lớp dữ liệu tương ứng. Cache lưu trên thiết bị và được cập nhật sau khi nhận payload hợp lệ. Module tải một lần lúc khởi động và giới hạn payload ở 256 KiB.

```csharp
using NebulaSoft;

namespace FrameworkGuide.Samples
{
    public static class GuideRemoteDefaults
    {
        public static int GetInterstitialMinLevel(int localDefault)
        {
            var data = RemoteConfigController.TryGetConfig<
                AdsRemoteConfigData>("ads");
            return data?.minLevel ?? localDefault;
        }
    }
}
```

Dùng `TryGetValue<T>(key, out value)` cho feature flag hoặc giá trị đơn. Với dữ liệu có cấu trúc, override `RemoteConfigData.Validate()` để bảo vệ ràng buộc của tính năng. Một key thiếu, sai kiểu hoặc không qua validation trả về trạng thái thiếu override. `GameData` áp override vào bản sao runtime của asset. Currency và giới hạn Lives đã lưu tiếp tục dùng giá trị trong save của người chơi hiện tại.

Tích hợp nguồn riêng như Firebase có thể tải trong `InitModule.Prepare()` rồi gọi `RemoteConfigController.Init(json)` trước khi các consumer khởi tạo. JSON từ xa được đọc trên client, nên chỉ đặt thông số công khai trong payload.

### Profile puzzle casual

Nhóm `puzzle` chỉ điều khiển luồng chơi chung của template. Mỗi game tiếp tục quản lý luật bàn, số lượt, hint và dữ liệu level trong module gameplay của mình.

| Key trong `puzzle` | Profile mẫu | Hành vi |
| --- | --- | --- |
| `rewardedReviveEnabled` | `true` | Cho phép hiện lời mời nếu luật level cho hồi sinh và quảng cáo thưởng đã sẵn sàng |
| `reviveOfferSeconds` | `8` | Số giây thực để chọn hồi sinh. Chấp nhận từ 1 đến 30 |
| `maxRewardedRevivesPerAttempt` | `1` | Giới hạn lượt hồi sinh thưởng đã thành công trong mỗi lần chơi. Chấp nhận từ 0 đến 5, với 0 là không giới hạn |
| `interstitialOnFinalFailure` | `false` | Bỏ qua quảng cáo chen ngang ở màn thua để người chơi có thể thử lại ngay |

Field vắng mặt trong `puzzle` dùng mặc định trong **Game Data**. Nhóm `revive` tiếp tục chứa giá và số giây được cộng. Khi hồi sinh thành công, `GameController.Revived` chuyển số giây đó cho gameplay. Level có đếm giờ có thể đăng ký sự kiện để cộng thời gian. Level tính theo lượt có thể dùng cùng sự kiện để áp dụng luật hồi sinh của mình.

### Profile và leaderboard

LocalLeaderboardService hiện cung cấp dữ liệu offline demo, có PreloadLeaderboardsAsync, GetTopPlayersAsync và RecordCurrentLevelCompletionAsync. MenuController preload bảng local. Kết quả local không phải ranking online được xác thực.

```csharp
using System.Threading.Tasks;
using NebulaSoft;

namespace FrameworkGuide.Samples
{
    public static class GuideLeaderboardQuery
    {
        public static async Task<int> GetVisibleRowCount()
        {
            var entries = await LocalLeaderboardService.GetTopPlayersAsync(10);
            return entries.Count;
        }
    }
}
```

View nhận kết quả rồi render khi lifetime còn hợp lệ; adapter backend có thể dùng cùng contract view. PlayerProfileSave/FirebaseProfileHandler và progress snapshot giữ các bit phân biệt field thiếu với giá trị 0. Không mặc định ghi đè save local từ cloud snapshot chưa biết đầy đủ.

Firebase/Facebook code được guard FIREBASE/FACEBOOK; tên handler không chứng minh SDK/backend đang hoạt động. Nếu bật tích hợp, cài package tương ứng, nối loading task, auth/profile/cloud policy và cấu hình ứng dụng riêng. Đừng import SDK vào board rule.

## 25. Screen Settings, Dev Panel, Defines và Inspector

| Hệ thống | Cấu hình/cách dùng | Controller cần làm |
| --- | --- | --- |
| Screen Settings | InitModule đặt frame rate, battery mode, sleep timeout | Không set frame rate cạnh tranh trong từng scene |
| Dev Panel | DevPanelInitModule link DevPanelSettings, tooling test state | Expose action QA qua adapter game, tách reset QA khỏi flow thật |
| Defines | DefineAttribute và editor tooling phát hiện dependency | Guard code SDK; không dùng symbol thay runtime readiness |
| Inspector | Drawer, attribute, module/editor UI và prefab hooks | Dùng để authoring; runtime không tham chiếu UnityEditor |

Ví dụ metadata của dependency: `[Define("MODULE_X", assemblyType: "Vendor.Type")]` giúp tooling phát hiện package/type. Đó là metadata compile, không tạo instance hay dependency container. Khi package bị xóa cần tắt symbol tương ứng và kiểm tra compilation; không xóa toàn bộ editor tooling nằm trong core vì một công cụ ngoài core đã bị loại bỏ.

Các symbol Ads hiện tại: MODULE_ADSKIT, MODULE_ADMOB và ENABLE_VOXELBUSTERS_ADS_KIT. MODULE_IAP/UNITY_IAP_NEW chỉ bật theo adapter Unity Purchasing tương ứng. FIREBASE/FACEBOOK theo dependency thực sự có. Kiểm tra từng target vì define của Android, iOS và Standalone có thể khác.

Runtime/controller không cần gọi Inspector module. Editor script đặt trong Editor folder hoặc editor-only asmdef. Asset UIEffect vendor giữ nguyên; editor custom authoring của game không đặt trong vendor folder.

## 26. Level, timer, input và power-up của game mới

Template giữ ActiveSession và LevelSave cho tiến trình. GameController.GameComplete đánh dấu hoàn tất, win streak và quest; ShowCompleteUI là bước riêng. GameController.GameOver điều phối shell/UI; Revive chỉ đổi cờ shell và đóng popup, không tự khôi phục board hoặc timer của game.

| Contract hiện tại | Behavior thực tế |
| --- | --- |
| LevelData | Asset rỗng; game mới phải thêm schema hoặc subclass |
| ActiveSession.Current | Lazy load LevelSave từ SaveController |
| OnLevelStarted(data) | Link data và đặt FirstStart false |
| GetLevelIndex(index) | Ghi real/last index theo index truyền vào |
| SetLevelIndex(index) | Đổi display/max reached; không tự chọn asset |
| GameComplete() | Guard complete một lần, ghi session/streak, report quest |
| OnCompleteRewardRecieved() | Load lại Game scene; không tự tăng index |
| PUUIController | Tạo bốn UI preview; chưa có mechanic booster |

Không gọi OnLevelCompleted lần nữa sau GameComplete vì sẽ lặp mutation session/streak. Title cần level database thật và policy resolve index, random/replay/end content. Không xem MaxReachedLevelIndex là ID asset.

<!-- pagebreak -->

### Timer do game controller cập nhật

```csharp
using NebulaSoft;
using UnityEngine;

namespace FrameworkGuide.Samples
{
    public sealed class GuideTimedLevel : MonoBehaviour
    {
        [SerializeField] float seconds = 120f;
        private readonly GameplayTimer timer = new GameplayTimer();
        private void Start()
        {
            timer.SetMaxTime(seconds);
            timer.OnTimerFinished += Finished;
            timer.Start();
        }
        private void Update() => timer.Update();
        private void Finished() => GameController.GameOver(true);
        private void OnDestroy() => timer.OnTimerFinished -= Finished;
        public void Revive(int extraSeconds)
        {
            timer.SetMaxTime(extraSeconds);
            timer.Start();
            GameController.Revive(extraSeconds);
        }
    }
}
```

GameplayTimer là class thuần, không có MonoBehaviour Update tự động. SetMaxTime trước Start; Start đặt lại current về max. Mẫu revive dành timer đã hết giờ và bắt đầu một khoảng extraSeconds mới; không dùng Resume khi chưa có pause reason, vì Resume chỉ xử lý reason đang tồn tại. Pause/Resume dùng reason có count, mỗi pause cần resume cân xứng. Khi tích hợp board, chỉ một owner tick timer và cập nhật UI từ OnTimeSpanChanged.

Input và booster phải có contract game: CanApply, Apply, cost/stock và event outcome. UI phát intent; controller validate rồi mutate board/save, phát feedback và report quest. Undo/shuffle/extra-slot/tool mechanic không được suy ra từ icon UI. Nếu grant booster từ IAP/Daily/Quest, cần inventory và reward adapter thật trước khi enable reward đó.

## 27. Ví dụ đầy đủ: board nối GameController

Mẫu này đặt ở layer game mới, không sửa module core. GuideBoardPort là adapter abstract; đội gameplay triển khai concrete board. GuideLevelData là schema mẫu kế thừa LevelData. Controller chọn level, start board, kết thúc, persist, next level và revive.

```csharp
using System;
using NebulaSoft;
using UnityEngine;

namespace FrameworkGuide.Samples
{
    [CreateAssetMenu(menuName = "Game/Level")]
    public sealed class GuideLevelData : LevelData
    {
        public GameObject BoardPrefab;
        public int WinCoins = 20;
    }

    public abstract class GuideBoardPort : MonoBehaviour
    {
        public abstract event Action Won;
        public abstract event Action Lost;
        public abstract void Begin(GuideLevelData data);
        public abstract void StopInput();
        public abstract void ReviveBoard(int extraSeconds);
    }

    public sealed class GuideGameFlow : MonoBehaviour
    {
        [SerializeField] GuideLevelData[] levels;
        [SerializeField] GuideBoardPort board;
        [SerializeField] GuideContinueButton continueButton;
        private int index;
        private bool resolved;
        private void Start()
        {
            if (levels == null || levels.Length == 0)
                throw new InvalidOperationException("No levels");
            index = Mathf.Clamp(ActiveSession.Current.DisplayLevelIndex,
                0, levels.Length - 1);
            ActiveSession.Current.GetLevelIndex(index);
            ActiveSession.Current.OnLevelStarted(levels[index]);
            board.Won += Win;
            board.Lost += Fail;
            if (continueButton != null)
                continueButton.Bind(AdsManager.Service, Revive);
            GameController.ActivateGame();
            board.Begin(levels[index]);
        }
        private void Win()
        {
            if (resolved) return;
            resolved = true;
            board.StopInput();
            GameController.GameComplete();
            CurrencyController.Add(CurrencyType.Coins,
                levels[index].WinCoins, "level_win");
            SaveController.MarkAsSaveIsRequired();
            SaveController.Save();
            GameController.ShowCompleteUI();
        }
        private void Fail()
        {
            if (resolved) return;
            resolved = true;
            board.StopInput();
            GameController.GameOver(allowRevive: true);
        }
        private void Revive()
        {
            if (!resolved) return;
            resolved = false;
            int seconds = GameData.Data.ReviveExtraSeconds;
            board.ReviveBoard(seconds);
            GameController.Revive(seconds);
        }
        public void NextLevel()
        {
            if (index + 1 >= levels.Length)
            {
                GameController.LoadMenu();
                return;
            }
            ActiveSession.Current.SetLevelIndex(index + 1);
            SaveController.MarkAsSaveIsRequired();
            SaveController.Save();
            GameController.Replay();
        }
        private void OnDestroy()
        {
            if (board == null) return;
            board.Won -= Win;
            board.Lost -= Fail;
        }
    }
}
```

### Gắn trong Unity

1. Tạo GuideLevelData cho từng level, gán board prefab và reward.
2. Triển khai GuideBoardPort trong gameplay: xây board, accept input, phát Won/Lost, pause/stop và revive đúng mechanic.
3. Gắn GuideGameFlow vào Game scene, gán levels, concrete board và continue button. GameController/UIController hiện hữu vẫn phụ trách shell và page.
4. Bật ADP_Rewarded, gán nó vào GuideContinueButton, bind Unity Button.OnClick tới OnClicked.
5. Với UIComplete, đổi binding nút Next sang GuideGameFlow.NextLevel thay callback mẫu GameController.OnCompleteRewardRecieved; không giữ cả hai listener.
6. Nếu dùng UI GameOver sẵn có, nối action revive vào cùng board flow; không để nút chỉ gọi GameController.Revive mà bỏ qua board/timer.

Mẫu grant coins tại Win một lần trong scene; khi dùng completion reward UI khác, chọn một owner grant để tránh double reward. Chưa có durable completion/claim ID và policy replay farm reward; game production cần bổ sung theo thiết kế. Lives lock/unlock của chương 20 cũng là policy riêng, không tự xảy ra trong mẫu này.

Mẫu phục vụ luồng tích hợp, không triển khai board engine hoặc 50 level. Khi content hết, Next trả Menu. Với level lớn, thay array serialized bằng database có ID ổn định; giữ controller và port để engine có thể thay mà không kéo SDK vào gameplay.

## 28. Ma trận kết nối và lifetime

| Feature | Entry sau Init | Owner controller/view | Cleanup / chú ý |
| --- | --- | --- | --- |
| Save | SaveController | Service/model của feature | Dirty sau mutation; không Init lại mỗi scene |
| UI | UIController | Scene owner + UIPage | OnPageClosed đúng một lần; tránh restore trong destroy |
| Audio/Music | AudioController/MusicSource | Feedback hoặc scene music | Bỏ event volume; không duplicate global music |
| Haptic | Haptic | Feedback intent | Không gọi wrapper native từ item |
| Tween | Tween/TweenCase | View/animation owner | Kill handle khi disable/destroy |
| Pool | Pool | Scene VFX/object owner | Reset khi reuse; deactivate khi return |
| Currency | CurrencyController | Economy controller + HUD | Unsubscribe balance callback |
| Reward | RewardsSet/Reward | Feature grant owner | Một owner; claim identity nếu qua reload |
| Ads | AdsManager.Service / IAdsService | Presenter + AdsGameBridge | Cancel wait; không dispose service từ page |
| IAP | IAPManager | Offer/view + manager | Unsubscribe UI; transaction vẫn do manager sở hữu |
| Analytics | AnalyticsController | Adapter/observer | Unsubscribe, tránh double report |
| Skins | SkinController | Skin scene owner/view | Unsubscribe; apply visual do game |
| Lives | LivesSystem | Play/session flow | Lock/unlock theo policy một owner |
| Quest | QuestService | Gameplay hooks + quest view | Report outcome thật, claim qua service |
| Daily Reward | DailyRewardService | Claim presenter/view | StateChanged unsubscribe; UTC policy chung |
| Tutorial | TutorialController/ITutorial | Game controller + focus view | Init/Start tách; Unload overlay |
| Remote/Cloud | Typed config/handler adapter | Init task + feature service | Validate schema/lifetime, giữ local fallback |
| Level/Timer | ActiveSession/GameplayTimer | Controller game mới | Explicit next index, tick một owner |

Không tạo một singleton mới cho từng UI feature khi service đã có facade. Game root quyết định composition, scene controller giữ instance presenter/session; service toàn ứng dụng do InitModule hoặc Initializer sở hữu. Một page destroy không được shutdown AdsModule, SaveController hoặc global AudioController.

## 29. Kiểm tra, nâng version và tài liệu nguồn

### Quy trình thay đổi feature

1. Chỉnh config và controller tại đúng ownership layer.
2. Kiểm tra Init > Menu > Game, popup show/hide, second boot và scene reload.
3. Chạy test tác động thật: Save/IAP/Ads và smoke test theo thay đổi.
4. Test thiết bị cho native consent, quảng cáo, reward/cancel/restore, audio/haptic và safe area.
5. Ghi changelog, migration nếu đổi schema/API, giữ SDK source nguyên bản.

Sau cài Ads Kit, nhóm test chọn lọc gồm AdsModuleTests, AdsDummyPlayModeTests, AdsKitPlayModeTests, IAPModuleTests, SaveModuleTests và FrameworkSceneSmokeTests: 77/77 pass trên Unity 6000.3.12f1, Android target. SDK smoke test khởi tạo, load ba format, show/hide banner và No Ads qua provider mô phỏng Editor. Test này không xác nhận ad delivery/consent/native geometry trên điện thoại.

Không diễn giải 77/77 là toàn bộ suite. Các audit trước ghi nhận bốn mismatch baseline QuestRotationTests về số slot daily/weekly, reward online và sprite nút Go; chúng không nằm trong nhóm 77 test cài Ads. Tài liệu được cập nhật không sửa test hoặc runtime để che baseline.

| Hiện tượng | Điểm kiểm tra |
| --- | --- |
| Controller chưa initialized | Có chạy Init trước scene Menu/Game, settings refs và thứ tự module? |
| UI page not found | Pages/CachedPages có đúng concrete type, InitPages đã gọi? |
| Popup đóng nhưng vẫn pause | OnPageClosed gọi đúng một lần, unscaled hide animation? |
| Background hụt safe area | Background đang là con SafeContent hoặc được đăng ký safe panel? |
| Ads Disabled/NotReady | Placement enabled, module ready, preload và mapping hợp lệ? |
| Ads simulation không load | Android/iOS target, test IDs, defines và package đã import? |
| IAP button mua hai lần | Có cả IAPButton auto binding và listener Buy tự thêm? |
| Save không persist | Mutation có mark dirty, Save là async, lỗi IO hoặc key đổi? |
| Next chơi lại level cũ | Shell chỉ reload Game; title đã cập nhật level index/database chưa? |
| Booster chỉ hiện icon | PUUIController là UI-only; inventory/board mechanic đã triển khai chưa? |

### Nguồn đối chiếu và cập nhật

README.md là overview. `docs/ads-module.md`, `docs/iap-module.md`, `docs/save-module.md` ghi contract và migration chi tiết; `docs/addon-cleanup.md` ghi cấu trúc Addon. Source runtime là căn cứ cho tên API và behavior. Hướng dẫn này sửa cách mô tả cũ theo source hiện tại, không tuyên bố feature chỉ có UI là engine hoàn chỉnh.

File source: `docs/framework-developer-guide.md`. Script dựng PDF: `tools/Documentation/build_framework_pdf.py`. Nguồn UML: `docs/diagrams/uml/`; render lại bằng `tools/Documentation/build_framework_uml.py`. Bản UML cập nhật xuất tại `output/pdf/GameFrameWorkPackage_Developer_Guide_UML.pdf`; README liên kết tới bản này. Các PDF trước đó là bản cũ. Các đoạn C# được kiểm tra biên dịch với assembly của Unity hiện tại; việc compile mẫu không thay thế triển khai concrete board và test gameplay.

Khi cập nhật SDK, sửa vendor/adapter/config tại biên tích hợp. Khi đổi service contract, ghi API version và migration. Giữ tài liệu và ví dụ sát source để junior nối feature qua service sẵn có.

# Addon cleanup

Đối chiếu cấu trúc với Flower Harvest Color Match 3D, hoàn tất ngày 2026-10-11.

Đây là báo cáo lịch sử. Từ ngày 2026-11-07, UI mẫu đã chuyển về Project Files/Game/Features và Project Files/Game/UI. Cấu trúc hiện tại được mô tả tại [project-structure.md](project-structure.md); evidence bên dưới giữ đường dẫn tại thời điểm audit.

```text
Assets/Addon/
├── ThirdParty/
│   └── UIEffect-5.9.6/
└── UI/
    ├── Fonts/
    ├── Scripts/
    ├── Sprites/
    ├── Textures/
    └── Prefabs/
        ├── CoinSafe/
        ├── DailyReward/
        ├── Leaderboard/
        ├── Popups/
        ├── Profile/
        ├── Quests/
        └── Shared/
```

Prefab và texture mẫu tiếp tục nằm trong Addon. Giữ các prefab phục vụ UI và framework; gom theo tính năng để có thể dùng làm mẫu cho game mới. Cấu trúc này không gắn với gameplay Flower Harvest.

## Thay đổi

- Chuyển 18 prefab và một font bằng Unity AssetDatabase; giữ nguyên GUID và nội dung.
- Giữ 19 prefab mẫu. LeaderboardBottomNavigation được giữ thêm so với bản Flower vì scene Menu của framework còn dùng nó.
- Xóa 163 asset Addon không có tham chiếu từ asset giữ lại, scene, prefab hoặc đường dẫn trong script; bỏ 44 thư mục rỗng.
- Giữ nguyên 661 file của UI-Effect, gồm mã vendor và metadata.
- Cập nhật đường dẫn prefab trong QuestPrefabBuilder, CoinSafeProgressTests và QuestRotationTests.

Xóa các công cụ ngoài core ở Assets/Editor: FrameworkVisualSanitizer, NoConnectionPopupPrefabBuilder, ProfilePopupPrefabBuilder, SaveProgressFoundDialogPrefabBuilder. PicturePuzzleFigmaEmbeddedBridge và PicturePuzzleFigmaWireframeExporter cùng hai bộ test phụ thuộc cũng đã được loại bỏ. Hai công cụ Figma và hai bộ test đã được bạn xóa trước. Đợt dọn này xóa bốn công cụ còn lại. Editor thuộc module framework vẫn được giữ.

Không thêm DI. Đối chiếu SHA-256 xác nhận 2692 file core, Project Files, Packages và ProjectSettings có trước đợt dọn vẫn giữ nguyên; ngoại lệ duy nhất là QuestPrefabBuilder được cập nhật đường dẫn như mô tả trên. Các thay đổi save/IAP bạn đang làm được giữ nguyên. Unity tự nâng phiên bản URP Renderer khi SaveAssets; đã khôi phục đúng byte trước đợt dọn sau khi xác minh hash, giữ bản tự nâng cấp trong Library/AddonCleanup.

## Kiểm tra

- Unity không còn lỗi biên dịch C# sau khi xóa công cụ và test phụ thuộc.
- 74 asset được giữ có nội dung và GUID không đổi.
- 19 prefab không có missing script.
- Không có tham chiếu serialized đến GUID vừa xóa.
- UI-Effect không thay đổi nội dung.
- 21 test hoàn tất; 4 lỗi Quest. Nhóm CoinSafe và FrameworkSceneSmokeTests không có test lỗi; luồng Init/Menu/Game không thiếu script.

Bốn lỗi Quest là khác biệt có sẵn giữa test và mẫu framework: Daily/Weekly có 6/5 nhóm nhưng test yêu cầu 4; phần thưởng quest online là Currency nhưng test yêu cầu RandomPowerUp; nút Go dùng WhiteSprite nhưng test yêu cầu Go_btn.png. Không sửa dữ liệu hoặc làm yếu assertion để che lỗi. Quest Database và QuestElement prefab khớp byte với Git HEAD; test chỉ đổi đường dẫn prefab. Xem baseline-quest-evidence.json và test-results.json để đối chiếu.

Chi tiết asset chuyển/xóa và kết quả kiểm tra nằm trong docs/addon/cleanup-audit.json, docs/addon/validation.json. Bản sao asset trước khi dọn ở Library/AddonCleanup/backup; thư mục Library là dữ liệu máy local, không phải bản lưu dài hạn trong Git.

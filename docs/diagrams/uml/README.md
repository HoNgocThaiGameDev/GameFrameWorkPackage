# UML cho framework

Sơ đồ dùng ký pháp UML theo phong cách Astah: class, sequence, activity,
state và component diagram. Nguồn chỉnh sửa là `.puml`, render vector là
`.svg`; đây không phải project `.asta` native. Runtime không dùng các file này.

`catalog.json` xác định loại sơ đồ, caption và phạm vi:

- `source`: quan hệ / lời gọi có thật trong source hiện tại.
- `proposed`: thiết kế tích hợp của game mới, chưa được cài vào template.

Hierarchy GameObject được ghi trong bảng riêng của hướng dẫn. Class diagram
không dùng parent-child Transform để biểu diễn kế thừa. Serialized reference
là association; GetComponent / lời gọi static là dependency. Mũi tên tam giác
rỗng hướng về base class / interface. Composition chỉ dùng khi có ownership
lifetime có thật, ví dụ AdsModule tạo và Dispose AdsService.

Sequence diagram có lifeline, activation, call, return và combined fragment
`alt`, `opt`, `loop`, `ref`. `ref` dẫn sang sơ đồ chi tiết, không có nghĩa là
một lời gọi SDK đồng bộ đã hoàn tất. SDK task/consent được tách thành UML-15.

Đã đối chiếu Initializer, GameLoading, SDKInitializer, ProjectInitSettings,
MenuController, GameController, UIController/UIPage và Ads/Save runtime.
Phần đề xuất đánh dấu rõ TitleGameFlow, board và level database cần bổ sung.

Chương 6 dùng UML-17 tới UML-20 để giải thích controller scene, UI routing,
session/model và persistence: restore/cache model, currency event cập nhật UI,
và win/session/popup/NEXT. UML-11 chỉ là hệ thống type của UI và nằm ở chương 10.
Không có quan hệ SaveController gọi UI refresh. Autosave định kỳ đang tắt
(`autoSaveDelay = 0`); callback NEXT hiện chỉ reload Game.

Render bằng PlantUML 1.2026.8 (Java 17 đang dùng tại máy này):

```powershell
python tools/Documentation/build_framework_uml.py --jar <path-to-plantuml.jar>
python tools/Documentation/build_framework_pdf.py
```

Muốn xuất thêm PNG để xem từng hình, thêm `--png`. Python cần `reportlab`,
`svglib` và `pypdf`; render/QA PDF sử dụng Poppler. Không gửi source lên server
PlantUML. JAR nằm trong cache tool, không thêm vào Assets/SDK hoặc Git repo.

Tham khảo ký pháp và CLI từ [PlantUML](https://plantuml.com/command-line).

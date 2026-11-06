# Module audit diagnostics

Các công cụ này tái hiện các ca trong báo cáo ngày 2026-11-07 và kiểm tra lại bản vá.

## Chạy lại

Tại thư mục gốc dự án, với .NET SDK 9 và Python 3.12:

```powershell
python tools/ModuleAudit/scan_project.py --output output/audit/2026-11-07/post-fix-inventory.json
dotnet run --project tools/ModuleAudit/ModuleAudit.csproj -- --output output/audit/2026-11-07/post-fix-reproductions.json
dotnet build tools/PlayerCompileProbe/PlayerCompileProbe.csproj --nologo -v minimal
```

`ModuleAudit` biên dịch trực tiếp các file được liệt kê trong project. Unity native objects, SaveController, SDK và một số service phụ dùng boundary stub trong bộ nhớ. Ca remote config chạy Newtonsoft.Json thật và kiểm tra đầu ra bằng System.Text.Json.

Kết quả trước vá có **3 đạt, 7 thất bại** và compile probe tái hiện **CS0246**. Sau vá, kết quả có **10 đạt, 0 thất bại** và compile probe hoàn tất. Hai file kết quả được lưu riêng để đối chiếu.

`--hash` in hash của `currency:0` trong process .NET hiện tại. Hash/cặp chuỗi va chạm có thể đổi mỗi lần chạy. Không suy diễn kết quả .NET 9 thành một phép đo trên Unity Mono/IL2CPP.

`scan_project.py` chỉ kiểm kê source, assembly, fingerprint SHA-256 và triage GUID. GUID không có trong `Assets` có thể thuộc UPM; khi thiếu `Library/PackageCache`, không được quy tất cả thành Missing Script. Scanner không thực hiện semantic analysis và không quyết định dự án build được hay không.

Kết quả này bổ sung cho Unity Test Runner. Build Android/iOS, sandbox IAP và consent trên thiết bị cần kiểm tra riêng.

# Báo cáo phân tích: Hóa đơn điện tử cấu hình động (PMSH_Web → OneS_PMSH_Web)

Phạm vi: chỉ PMS (chưa làm POS). Ngày lập: 19/11/2026.
Trạng thái: **chỉ đọc và phân tích, chưa sửa code hay DB nào.**

---

## 1. Tóm tắt nhanh

- PMSH_Web có một hệ thống hóa đơn điện tử **cấu hình động**: nhà cung cấp (Viettel, FAST, VNPT) được khai báo hoàn toàn bằng dữ liệu trong DB (provider, field cấu hình, API, mapping request/response, rule thành công/thất bại), không viết code riêng cho từng nhà.
- Cách PMSH_Web viết thì **không nên copy**: một controller 16.379 dòng, SQL thô trong controller, view 5.800 dòng, nhiều code chết.
- OneS đã có sẵn **phía PMS** (bảng `FolioVAT`, `FolioVATDetail`, `tblUploadInvoiceLog`, `tblPulishInvoiceLog`, SP `spVAT_SearchVAT_HDDT`…) nhưng **chưa có 15 bảng cấu hình** hóa đơn điện tử.
- Đề xuất: tạo project **`EInvoice`** theo đúng mẫu module của OneS, khoảng **14 file**, port nguyên logic engine, kiểm chứng bằng cách so sánh request Preview giữa bản cũ và bản mới.

---

## 2. Nguồn đã đọc

| Nguồn | Nội dung đã đọc |
|---|---|
| `D:\vanh\PMSH_Web` (code) | Toàn bộ model, interface và model runtime của engine, cấu trúc `EInvoiceController`, luồng PMS upload/publish, `HuongDan_CauHinhDong_HoaDonDienTu.md` |
| DB `oneSPMS` (PMSH_Web) | Cấu trúc 15 bảng, ràng buộc, chỉ mục, số dòng, danh sách Operation, Property, Rule, Field (chỉ `SELECT`) |
| DB `oneSPMSh_Tach_A1A2` (OneS) | Bảng/SP liên quan hóa đơn điện tử đã có sẵn (chỉ `SELECT`) |
| Code OneS | Cấu trúc module, DI, controller/service mẫu, `DataTableHelper`, `BaseModel` |

Chưa đọc từng dòng: 16.379 dòng controller và các file `Dynamic*Builder` (chỉ đọc cấu trúc và các hàm chính). Chi tiết bên trong hàm sẽ đọc lại khi port từng phần.

---

## 3. PMSH_Web đang làm gì

### 3.1 Luồng chạy

```
Property
  → chọn Provider mặc định
  → chọn Business Action (UPLOAD_INVOICE / PUBLISH_INVOICE / ISSUE_INVOICE …)
  → tìm Operation tương ứng
  → đọc giá trị cấu hình riêng của Property
  → lấy dữ liệu hóa đơn từ DB nguồn (FolioVAT trong DB của Property)
  → áp Mapping (dựng header/body/query/path)
  → đăng nhập nếu API yêu cầu (AUTH_LOGIN)
  → gọi API nhà cung cấp
  → đánh giá Response Rule (thành công / thất bại)
  → đọc Response Mapping (InvoiceNo, Key, KeySearch …)
  → ghi Execution Log
  → đồng bộ trạng thái về tblUploadInvoiceLog / tblPulishInvoiceLog của DB PMS
```

### 3.2 "Kết nối động" gồm hai lớp

1. **Nguồn dữ liệu theo Property:** `Property_Einvoice` lưu `Database_IP`, `Database_Name`, `Database_User`, `Database_Password`, tên SP tìm kiếm. Controller dựng connection string rồi chạy SP trên DB đó.
2. **Nhà cung cấp theo Property:** URL, tài khoản, mẫu số, ký hiệu… nằm trong `Property_EInvoiceProviderValue`. Mapping lấy ra bằng `SourceType = CONFIG`.

### 3.3 Các bảng (đều có trong DB PMSH_Web)

| Nhóm | Bảng | Số dòng |
|---|---|---:|
| Nhà cung cấp | `EInvoiceProvider` | 3 |
| | `EInvoiceProviderField` (field cấu hình, có điều kiện hiển thị) | 38 |
| | `EInvoiceProviderFieldOption` | 19 |
| API | `EInvoiceProviderOperation` | 42 |
| | `EInvoiceProviderMapping` | 481 |
| | `EInvoiceProviderResponseRule` | 10 |
| | `EInvoiceProviderBodyField` (khai báo document/collection) | 74 |
| Theo Property | `Property_Einvoice` | 8 |
| | `Property_EInvoiceProvider` | 7 |
| | `Property_EInvoiceProviderValue` | 70 |
| | `Property_InvoiceDetailParameter` (chỉ POS dùng) | 20 |
| | `Property_InvoiceDetailMapping` (chỉ POS dùng) | 12 |
| | `Property_Einvoice_Config` (không code nào đọc) | 10 |
| Nhật ký / quyền | `EInvoiceProviderExecutionLog` | 207 |
| | `PropertyPermission` | 192 |

Khóa ngoại: BodyField/Field/Operation → Provider; FieldOption → Field; Mapping → Operation; Operation tự tham chiếu (AuthenticationOperationID); Property_EInvoiceProvider → Property_Einvoice + Provider; Property_EInvoiceProviderValue → Property_EInvoiceProvider + Field.
Ràng buộc duy nhất đáng chú ý: Provider(Code), Field(ProviderID, FieldCode), Operation(ProviderID, OperationCode), Mapping(OperationID, MappingLocation, TargetPath), Property_EInvoiceProvider(PropertyID, ProviderID).

### 3.4 Dữ liệu cấu hình thực tế

**Provider và Operation (đang dùng cho PMS):**

| Provider | Operation chính | Business Action | Ghi chú |
|---|---|---|---|
| VIETTEL | `AUTH_LOGIN` → `CREATE_INVOICE` | `ISSUE_INVOICE` | Lập và phát hành 1 bước, dùng token đăng nhập; rule thành công: `errorCode IS_NULL` |
| FAST | `FAST_RESERVE_INVOICE` / `FAST_PUBLISH_INVOICE` | `UPLOAD_INVOICE` / `PUBLISH_INVOICE` | 2 bước; rule: `Success EQUALS 1`; còn nhiều operation phụ (kiểm tra trạng thái, PDF, điều chỉnh…) |
| VNPT | `VNPT_IMPORT_AND_PUBLISH_INV` | `ISSUE_INVOICE` | SOAP 1.1; rule: bắt đầu bằng `OK:` thành công, `ERR:` thất bại |

Một số operation đang `Inactive` (ví dụ `VNPT_IMPORT_INV_BY_PATTERN`, `VNPT_PUBLISH_INV_FKEY`, `CREATE_INVOICE_POS`…).

**Từ vựng Mapping thực tế:**
- Vị trí: `BODY`, `HEADER`, `QUERY`, `PATH`, `RESPONSE`.
- Loại nguồn: `COLLECTION`, `CONFIG`, `CONSTANT`, `CUSTOM`, `DOCUMENT`, `INVOICE`, `INVOICEDETAIL`, `RESPONSE`, `STATIC`, `SYSTEM`.
- Kiểu dữ liệu: `STRING`, `DECIMAL`, `INT`, `INTEGER`, `BOOLEAN`, `DATE`, `ARRAY`, `OBJECT`, `UNIX_MILLISECONDS`.
- **Điều kiện Mapping (Condition) chưa dùng ở dòng nào trong 481 dòng.**
- Body type: `JSON`, `FORM_URLENCODED`, `SOAP11`.

**Property (6 PMS, 2 POS):** AVANI, ARENA, TARENA, MERPERLE, ARENA_TEST (PMS) dùng SP `spVAT_SearchVAT_HDDT` trên DB riêng; AVANI→VNPT, ARENA_TEST→FAST, TARENA→VIETTEL, MERPERLE→VIETTEL…

### 3.5 Code (PMSH_Web)

- `WebApp/Controllers/EInvoiceController.cs`: **16.379 dòng**, 18 region: quản trị Property/Provider/Field/Operation/Mapping, quyền, PMS, POS, engine, mock/test.
- `WebApp/Services/InvoiceProviders/`: khoảng 40 file; lớn nhất `ProviderExecutionService` (1.680 dòng), `Runtime/DynamicProviderRequestBuilder` (1.915 dòng), `Runtime/DynamicProviderDocumentBuilder` (847 dòng), `ProviderRequestBodyBuilder` (777), `ProviderResponseMapper` (595).
- View: `ProviderManagement.cshtml` 5.813 dòng, `ElectronicInvoice.cshtml` 3.408, `PropertyEinvoice.cshtml` 2.805, `ElectronicInvoicePOS.cshtml` 1.611.
- Menu: E-INVOICE → User Management, Provider Management (chỉ nhóm admin `UserGroupID = 14`), Property E-Invoice, E-Invoice PMS, E-Invoice POS.

---

## 4. Điểm yếu của PMSH_Web (không nên copy)

1. Controller "thần": CRUD, engine, PMS, POS, test lẫn trong một file; SQL thô bằng `SqlConnection` ngay trong controller.
2. Lặp luồng PMS và POS (`UploadAndPublishInvoices` so với `UploadAndPublishInvoicesPOS`).
3. Khoảng 1.500 dòng endpoint `Test*` / `Mock*` nằm trong code chạy thật.
4. Code kiểu cũ còn sót: `EInvoiceApiService` (mọi thao tác gọi chung `/post`), `FastInvoiceProvider`, `ViettelInvoiceProvider`, `InvoiceProviderResolver`.
5. View nhồi JS inline hàng nghìn dòng.
6. Cờ `IsEncrypted` chỉ che giá trị khi trả ra client; trong DB `FieldValue` và `Database_Password` vẫn là chữ thường.
7. Quyền cứng `UserGroupID == 14`.

---

## 5. Nếp của OneS_PMSH_Web (rút từ code hiện có)

- Mỗi module là một project: `Controllers/`, `Services/Interfaces/I…Service.cs`, `Services/Implements/…Service.cs`, `Dto/`. Ví dụ Administration, Cashiering, Security.
- Thêm module: `ProjectReference` trong `WebApp.csproj`, `AssemblyPart` và `AddSingleton` trong `Program.cs`.
- Controller mỏng: nhận request, validate, gọi service, trả `Json`. Màn hình trả `PartialView`.
- Service gọi stored procedure qua `DataTableHelper.getTableData("spXxx", SqlParameter[])`. Kết nối qua `DBUtils`.
- Bảng cần CRUD chuẩn có thể dùng Model/BO/Facade trong BaseBusiness (`BaseModel` cho phép override `GetTableName()` khi tên bảng không theo quy tắc).
- View ở `WebApp/Views/<Module>/`, là modal nạp bằng `ModalManager.load(...)`, id có tiền tố, JS đặt trong module object. Menu ở `_Layout.cshtml`, ẩn theo quyền bằng `data-action`.
- Đã có `Encryptor` và khóa `Encryption` trong appsettings.

### Trạng thái hiện tại của DB OneS (`oneSPMSh_Tach_A1A2`)

| Có sẵn | Chưa có |
|---|---|
| `FolioVAT`, `FolioVATDetail`, `tblUploadInvoiceLog`, `tblPulishInvoiceLog`, `tblInvoiceChangeLog`, `PropertyPermission`, `PropertyType` | Toàn bộ bảng `EInvoice*` và `Property_*` (15 bảng) |
| SP `spVAT_SearchVAT_HDDT`, `spVAT_SearchVAT_Detail_HDDT` (tham số trùng khớp với lời gọi của PMSH_Web), `spVAT_SearchVAT_HDDT_FAST`, `spSearch_HDDT_FAST` | Dữ liệu cấu hình provider/operation/mapping |

---

## 6. Đề xuất cấu trúc: project `EInvoice` (khoảng 14 file)

```
EInvoice/
  EInvoice.csproj
  Controllers/EInvoiceController.cs               1 controller mỏng, chia #region như CashieringController
  Services/Interfaces/IEInvoiceService.cs         quản trị: Provider, Field, Option, Operation, Mapping, Property
  Services/Interfaces/IEInvoiceRuntimeService.cs  vận hành: tìm hóa đơn, preview, upload, publish
  Services/Implements/EInvoiceService.cs
  Services/Implements/EInvoiceRuntimeService.cs
  Dto/EInvoiceDto.cs                              gom model và request vào 1 file
  Runtime/ProviderRequestBuilder.cs               dựng request từ Mapping (gộp DynamicProviderRequestBuilder + DocumentBuilder)
  Runtime/ProviderExecutor.cs                     gọi HTTP, Response Rule, ghi log (gộp ExecutionService + HttpClient + ResponseMapper)
WebApp/Views/EInvoice/ProviderManagement.cshtml
WebApp/Views/EInvoice/PropertyEinvoice.cshtml
WebApp/Views/EInvoice/ElectronicInvoice.cshtml
Database/EInvoice_Setup.sql                       bảng + SP + dữ liệu cấu hình (chạy một lần trong SSMS)
```

Truy cập dữ liệu: service gọi **SP** qua `DataTableHelper`; toàn bộ SP nằm trong một file SQL, không rải SQL vào controller.

### Bỏ so với PMSH_Web (không ảnh hưởng PMS)

- `EInvoiceApiService`, `IInvoiceProvider`/`Fast`/`Viettel`/`Resolver`.
- Toàn bộ endpoint `Test*` / `Mock*`.
- Toàn bộ POS, `Property_InvoiceDetailParameter`, `Property_InvoiceDetailMapping`.
- `Property_Einvoice_Config` (không code nào đọc).
- **Cần kiểm tra trước khi quyết:** `HeaderBuilder`, `UrlBuilder`, `HttpRequestBuilder` có nằm trên đường gọi PMS không (sẽ truy vết từ `ExecuteAsync`).

### Thay đổi có chủ đích

- Quyền theo `data-action` của OneS thay vì `UserGroupID == 14` (nếu bạn đồng ý).
- Mã hóa thật `Database_Password` và các field Password bằng `Encryptor`.

---

## 7. Cách bảo đảm chạy chuẩn 100%

1. **Port logic, không viết lại.** Chỉ gộp file và bỏ code chết.
2. **Chuyển dữ liệu cấu hình** bằng script `INSERT` sinh từ `oneSPMS` sang DB OneS (cho các bảng `EInvoice*`), để 481 mapping giống bản đang chạy.
3. **So sánh Preview:** với cùng một `FolioVATID`, chạy Preview ở PMSH_Web và OneS rồi so sánh JSON/SOAP request từng ký tự. Chứng minh tương đương mà **không gửi gì lên Viettel/FAST/VNPT**.
4. **Chạy thật chỉ trên môi trường demo** của nhà cung cấp, từng hóa đơn một, kiểm tra Execution Log.
5. Checklist nghiệm thu theo `HuongDan_CauHinhDong_HoaDonDienTu.md` (mục 17).

---

## 8. Kế hoạch theo giai đoạn

| GĐ | Nội dung | Kết quả kiểm tra |
|---|---|---|
| 1 | Script bảng + SP; model; Provider Management (Provider, Field, Option) | Tạo/sửa provider và field trên web |
| 2 | Operation, Mapping, Response Rule | Dữ liệu 481 mapping hiển thị đúng |
| 3 | Property E-Invoice: kết nối DB động, giá trị cấu hình (mã hóa) | Test kết nối DB thành công |
| 4 | Engine: Preview và Execute cho PMS | Preview khớp PMSH_Web từng ký tự |
| 5 | Màn vận hành PMS (tìm, Upload, Publish, Issue, Upload & Publish), ghi log, đồng bộ trạng thái | Thử trên demo provider |

---

## 9. Rủi ro và điều chưa chắc

- Chưa đọc hết thân hàm của `Dynamic*Builder` và `ProviderExecutionService`; phát hiện logic ẩn khi port có thể làm tăng số file.
- Chưa kiểm tra SP nguồn `spVAT_SearchVAT_HDDT` của OneS trả đúng cột như PMSH_Web đang đọc (chỉ so được tham số).
- Cấu hình Viettel/FAST/VNPT đang chứa thông tin đăng nhập thật; khi chuyển dữ liệu cần đổi sang môi trường demo hoặc đảm bảo không vô tình gửi thật.
- Giá trị `FieldValue` hiện là chữ thường; mã hóa khi chuyển cần chốt cách làm để không mất khả năng đọc ra lúc gọi API.

---

## 10. Cần bạn chốt trước khi viết code

1. Tạo các bảng `EInvoice*` và `Property_*` ngay trong DB OneS (`oneSPMSh_Tach_A1A2`) được không? (DB này đã chứa `FolioVAT`, nên đồng thời là DB nguồn PMS.)
2. Property trong OneS: chỉ một dòng trỏ vào DB hiện tại, hay nhiều Property với `Database_IP` riêng như PMSH_Web?
3. Chuyển dữ liệu cấu hình cả 3 provider (VIETTEL, FAST, VNPT) hay chỉ provider bạn thực sự dùng?
4. Kiểu quyền: `data-action` theo tên quyền, hay vẫn nhóm admin cứng?

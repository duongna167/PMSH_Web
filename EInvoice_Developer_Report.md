# Báo cáo kỹ thuật: Hóa đơn điện tử cấu hình động

Ngày cập nhật: **19/11/2026**. Đối tượng: dev triển khai và bảo trì OneS_PMSH_Web.

Tài liệu được đối chiếu với mã nguồn và script hiện tại. Không dùng trạng thái đề xuất trong EInvoice_Analysis.md làm trạng thái triển khai. Trong lần lập báo cáo này chưa chạy build, script lên DB hoặc gửi API provider; các bước kiểm thử dưới đây là hướng dẫn thực hiện.

## 1. Phạm vi và kiến trúc

Module EInvoice hỗ trợ hóa đơn PMS: Upload, Publish, Issue và Upload & Publish. Provider, field cấu hình, operation/API, mapping request/response, document và response rule nằm trong DB. Có dữ liệu cấu hình VIETTEL, FAST, VNPT; khả năng sử dụng phụ thuộc operation active và cấu hình Property thực tế.

Có hai lớp động: kết nối DB nguồn theo Property và cấu hình API theo Provider. DB cấu hình dùng ConnectionStrings:DefaultConnection; DB PMS nguồn dùng thông tin lưu trong Property_Einvoice. Hai DB có thể trùng hoặc khác nhau.

| Thành phần | File | Trách nhiệm |
|---|---|---|
| Quản trị Provider | EInvoice/Controllers/EInvoiceProviderController.cs | Provider, Field, Option, Operation, Mapping, đọc Rule |
| Quản trị Property | EInvoice/Controllers/EInvoicePropertyController.cs | DB nguồn, giá trị cấu hình, Test Connection, Encrypt Secrets |
| Vận hành | EInvoice/Controllers/EInvoiceInvoiceController.cs | Tìm hóa đơn, Preview, ExecuteBatch, xem log |
| Service quản trị | EInvoice/Services/Implements/EInvoiceProviderService.cs | Validate, gọi SP quản trị |
| Service Property | EInvoice/Services/Implements/EInvoicePropertyService.cs | Lưu cấu hình, EInvoiceSecret mã hóa/giải mã |
| Service nghiệp vụ | EInvoice/Services/Implements/EInvoiceInvoiceService.cs | Tìm kiếm, trạng thái, batch tuần tự |
| Điều phối runtime | EInvoice/Services/Implements/EInvoiceRuntimeService.cs | Load config, xác thực, execute, đồng bộ PMS |
| Chuẩn hóa PMS | EInvoice/Runtime/PmsInvoiceContextBuilder.cs | Đọc header/detail và tạo context |
| Request/document | EInvoice/Runtime/ProviderRequestBuilder.cs | DynamicProviderRequestBuilder, DynamicProviderDocumentBuilder |
| HTTP/response/log | EInvoice/Runtime/ProviderExecutor.cs | ProviderExecutionService |
| Model | EInvoice/Dto/EInvoiceDto.cs; EInvoice/Runtime/ProviderRuntimeModels.cs | DTO và model runtime |
| View | WebApp/Views/EInvoice/ | ProviderManagement, PropertyEinvoice, InvoiceManagement, InvoicePreview |
| SQL | Database/EInvoice_Setup.sql | Schema, SP, quyền, dữ liệu cấu hình |

EInvoice target net9.0, tham chiếu BaseBusiness và Microsoft.AspNetCore.App. WebApp/WebApp.csproj tham chiếu EInvoice; WebApp/Program.cs nạp assembly controller bằng AssemblyPart và đăng ký singleton cho service/builder/executor. Không chạy EInvoice thành một ứng dụng riêng.

Menu nằm trong WebApp/Views/Shared/_Layout.cshtml, khu vực Cashiering. Electronic Invoice (PMS) là màn mới; E Invoice Management gọi Cashiering/EInvoiceManager là luồng cũ.

## 2. Bảng dữ liệu và DB phụ thuộc

Setup tạo 11 bảng trong DB cấu hình:

| Bảng | Nội dung |
|---|---|
| EInvoiceProvider | Nhà cung cấp |
| EInvoiceProviderField | Field cấu hình, control, required, encrypted, điều kiện hiển thị |
| EInvoiceProviderFieldOption | Lựa chọn của field Select |
| EInvoiceProviderOperation | API, business action, endpoint, body type, xác thực |
| EInvoiceProviderMapping | HEADER/BODY/QUERY/PATH/RESPONSE mapping |
| EInvoiceProviderBodyField | Document và collection để dựng payload |
| EInvoiceProviderResponseRule | Quy tắc xác định thành công/thất bại |
| EInvoiceProviderExecutionLog | Nhật ký thực thi và kết quả provider |
| Property_Einvoice | Đơn vị phát hành và DB nguồn |
| Property_EInvoiceProvider | Liên kết Property–Provider, mặc định/active |
| Property_EInvoiceProviderValue | Giá trị cấu hình theo Property |

Quyền dùng bảng sẵn có FormAndFunction, UserGroupPermission và PropertyPermission. Setup không tạo thay bộ bảng nghiệp vụ PMS.

DB nguồn cần FolioVAT, FolioVATDetail, Folio, Reservation, FolioDetail, tblUploadInvoiceLog, **tblPulishInvoiceLog** và SP tìm hóa đơn tương thích. Giữ đúng tên Pulish của hệ thống hiện tại.

## 3. Luồng xử lý

### 3.1. Tìm hóa đơn

InvoiceData → SearchInvoices → spEInvoice_PropertyConnection → mở DB nguồn → gọi tên SP trong InvoiceSearchSP → lọc tên khách/công ty trong C# → spEInvoice_InvoiceStates → trả danh sách.

SP nguồn thường là dbo.spVAT_SearchVAT_HDDT. Tên có thể đổi nhưng phải nhận đúng bộ tham số hiện tại:

| Tham số | Giá trị từ request |
|---|---|
| @ConfirmationNo | ConfNo hoặc chuỗi rỗng |
| @FolioNo | FolioNo |
| @SerialNo | Serial |
| @InvoiceNo | InvoiceNo |
| @FromDate | FromDate, đầu ngày |
| @ToDate | ToDate, cuối ngày |
| @Print | Print |
| @ResType | ResType, DTO mặc định -1 |
| @ZoneID | Chuỗi rỗng |

Ngày theo yyyy-MM-dd; timeout tìm kiếm 120 giây. Name không truyền xuống SP mà lọc Customer_Name/Customer_Company trong C#.

Kết quả cần ID là FolioVATID và cột phù hợp view. Service bổ sung trạng thái từ log của provider hiện tại; Publish/Issue ưu tiên Upload. Có thể cập nhật FormNo, SerialNo, EInvoiceNo, VATDate; trạng thái cần cột EStatus.

### 3.2. Dữ liệu đưa vào engine

PmsInvoiceContextBuilder.Create đọc SQL trực tiếp có tham số trên DB nguồn:

- Header: FolioVAT LEFT JOIN Folio, Reservation theo @FolioVATID.
- Detail: FolioVATDetail LEFT JOIN FolioDetail theo @FolioVATID, ORDER BY d.ID.
- Chuẩn hóa sai số tiền VND, tính lại tổng header từ detail, nhóm thuế và validate công thức trước khi gửi.

**PMS hiện không gọi InvoiceDetailSP hoặc spVAT_SearchVAT_Detail_HDDT.** Thay InvoiceDetailSP không làm đổi nguồn detail trong luồng PMS này.

Context có Invoice, InvoiceDetail, Property, Config, PreviousResponse, Session, System; collection Items và TaxBreakdowns. System.InvoiceDetailRows là positional array; System.ProviderKey có dạng {PropertyID}_{SourceType}_{FolioVATID}.

Quy ước hiện tại: header VND, email khách rỗng, Quantity <= 0 chuẩn hóa thành 1; VAT rate chấp nhận 0, 5, 8, 10, -1, -2, -9. Runtime còn kiểm tra tiền âm và công thức tổng nên cần Preview với dữ liệu thực tế.

### 3.3. Chọn operation, mapping, xác thực

ExecuteActionAsync nạp Property → provider active ưu tiên mặc định → operation theo BusinessActionCode → mapping/rule/config → context → xác thực nếu cần → executor.

BusinessActionCode là nghiệp vụ PMS dùng để chọn API. OperationCode là mã API cụ thể. ActionCode/ExternalMethodCode phục vụ cấu hình payload/giao thức của operation; không thay thế các mã này cho nhau.

Mapping gồm vị trí đích, TargetPath, SourceType/SourceValue, DataType, Required, DefaultValue, FormatString, OmitWhenNull, Prefix/Suffix, DisplayOrder và điều kiện áp dụng. CONFIG lấy field cấu hình; INVOICE lấy header; INVOICEDETAIL/COLLECTION lấy chi tiết; SYSTEM lấy giá trị hệ thống; RESPONSE lấy kết quả trước; DOCUMENT đọc định nghĩa BodyField.

Nếu IsAuthenticationRequired và có AuthenticationOperationID, runtime chạy operation đăng nhập cùng provider bằng ExecuteRawAsync. Response phải parse được JSON và được đặt vào PreviousResponse để mapping RESPONSE đưa token sang request chính. Xác thực tĩnh có thể đọc từ CONFIG.

### 3.4. Nghiệp vụ và batch

| Action | Xử lý |
|---|---|
| UPLOAD_INVOICE | Chạy operation upload |
| PUBLISH_INVOICE | Yêu cầu kết quả Upload thành công trước, parse được InvoiceNumber |
| ISSUE_INVOICE | Chạy operation lập/phát hành một bước |
| UPLOAD_AND_PUBLISH | Upload rồi Publish, không phải operation riêng |

Batch yêu cầu ConfirmSend=true, bỏ ID không hợp lệ/trùng, chạy tuần tự từng hóa đơn. Upload lỗi thì không Publish hóa đơn đó; lỗi một hóa đơn không buộc dừng các hóa đơn sau.

Kết quả gồm successCount, failedCount, results và thông tin completedStep/failedStep tùy trường hợp. Có lỗi từng phần thì success tổng là false. HTTP 200 vẫn có thể chứa lỗi nghiệp vụ khi response có data; client cần đọc success và results.

Publish lấy RawResponse Upload từ spEInvoice_RuntimePrevious. Parser xử lý các cấu trúc JSON cụ thể và SOAP dạng OK:pattern;serial-invoiceNo; thêm provider hai bước có thể cần mở rộng parser ngoài mapping.

### 3.5. HTTP, response, chống trùng và đồng bộ

Executor kiểm tra lần thành công theo PropertyID + SourceType + SourceRecordID + ProviderID + OperationID trước khi gửi API chính. Có log thành công thì từ chối chạy lại operation đó. Đây là kiểm tra trước gửi, không phải khóa nguyên tử bảo đảm chống mọi request đồng thời.

HTTP thành công chưa đủ: executor áp response mapping và response rule rồi dựng kết quả. Log được INSERT trực tiếp có tham số vào EInvoiceProviderExecutionLog trên DefaultConnection, lưu URL/body, raw response, HTTP status, ProviderSuccess, số hóa đơn, thời gian và thông báo. ErrorMessage cũng có thể chứa thông báo thành công.

Provider thành công → SynchronizeStatusAsync mở transaction trên DB nguồn:

- Upload: upsert tblUploadInvoiceLog theo FolioVATID.
- Publish/Issue: upsert cả tblUploadInvoiceLog và tblPulishInvoiceLog.
- Đọc sys.columns để chỉ ghi cột tồn tại; cần cột FolioVATID.
- Khi phát hành, ReturnValue của upload log được gán số hóa đơn hoặc OK để SP nguồn hiển thị Published.

API provider, log trung tâm và transaction PMS không cùng một transaction. Nếu provider thành công nhưng đồng bộ PMS lỗi, runtime giữ log thành công và báo lỗi đồng bộ riêng. Cần sửa/đối soát dữ liệu PMS, không gửi lại phát hành theo phản xạ. Timeout sau gửi cũng cần đối soát provider trước khi retry.

## 4. Stored procedure đang dùng

Tất cả spEInvoice_* dưới đây định nghĩa trong Database/EInvoice_Setup.sql và chạy trên DB cấu hình.

### 4.1. Quản trị Provider/API

| SP | Tham số chính | Công dụng |
|---|---|---|
| spEInvoice_ProviderSearch | Không có | Danh sách provider |
| spEInvoice_ProviderSave | @ID và dữ liệu provider | Tạo/sửa provider |
| spEInvoice_FieldSearch | @ProviderID | Field provider |
| spEInvoice_FieldSave | @ID, @ProviderID và dữ liệu field | Lưu field |
| spEInvoice_OptionSearch | @FieldID | Option field |
| spEInvoice_OptionSave | @ID và dữ liệu option | Lưu option |
| spEInvoice_OperationSearch | @ProviderID | API provider |
| spEInvoice_OperationSave | @ID, @ProviderID và cấu hình API | Lưu operation |
| spEInvoice_MappingSearch | @OperationID | Mapping operation |
| spEInvoice_MappingSave | @ID, @OperationID và cấu hình mapping | Lưu mapping |
| spEInvoice_RuleSearch | @OperationID | Đọc response rule quản trị |

Tham số đầy đủ và kiểu dữ liệu xem DTO + định nghĩa SQL. Các SP lưu dùng ID=0 để tạo mới. Controller hiện không có RuleSave hoặc màn CRUD riêng cho BodyField; cần script SQL được kiểm soát khi chỉnh hai phần này.

### 4.2. Property

| SP | Tham số | Công dụng |
|---|---|---|
| spEInvoice_PropertySearch | @ID | Danh sách/chi tiết Property |
| spEInvoice_PropertyConnection | @ID | Kết nối nguồn cho server |
| spEInvoice_PropertyFieldValues | @PropertyID, @ProviderID | Field và giá trị đã lưu |
| spEInvoice_OptionsByProvider | @ProviderID | Option active |
| spEInvoice_PropertySave | Dữ liệu Property, @ProviderID, @Values XML, @User | Lưu Property + provider mặc định + giá trị trong transaction |
| spEInvoice_PropertyDelete | @ID | Xóa khi chưa có execution log |

Values XML có dạng <values><v id="123" keep="0">value</v></values>; id là ProviderFieldID, keep=1 giữ giá trị đã lưu. DatabasePassword=NULL khi sửa giữ mật khẩu cũ. Khi đổi provider, cấu hình provider trước vẫn được giữ nhưng bỏ mặc định.

Property đã có lịch sử thì dùng Inactive. Test Connection và Encrypt Secrets dùng SQL/SqlConnection trong service, không có SP riêng. Test Connection chỉ kiểm tra mở DB và SP tồn tại, chưa xác nhận chữ ký tham số, cột kết quả hoặc API provider.

### 4.3. Runtime và vận hành

| SP | Tham số | Công dụng |
|---|---|---|
| spEInvoice_RuntimeProvider | @PropertyID | Provider active, ưu tiên mặc định |
| spEInvoice_RuntimeOperation | @ProviderID, @ActionCode, @OperationID | Operation theo ID hoặc business action |
| spEInvoice_RuntimeMappings | @OperationID | Mapping active theo DisplayOrder, ID |
| spEInvoice_RuntimeRules | @OperationID | Rule active theo Priority, ID |
| spEInvoice_RuntimeConfig | @PropertyID, @ProviderID | FieldCode/FieldValue/IsEncrypted của liên kết active và mặc định |
| spEInvoice_RuntimePrevious | @PropertyID, @FolioVATID | RawResponse Upload thành công mới nhất |
| spEInvoice_InvoiceStates | @PropertyID, @Ids XML | Trạng thái provider hiện tại |
| spEInvoice_AllowedProperties | @UserID, @UserGroupID, @SourceType | Property user được chọn |
| spEInvoice_ProviderActions | @PropertyID | Nghiệp vụ để hiện nút UI |
| spEInvoice_LogSearch | @PropertyID, @FolioVATID, @ActionCode, @Status, @FromDate, @ToDate | Tối đa 1.000 log, mới nhất trước |
| spEInvoice_ExecutionLogDetail | @ID bigint | Chi tiết request/response |

Ids XML: <ids><i v="123"/><i v="456"/></ids>. Status log: 1 thành công, 0 thất bại, NULL tất cả.

Ngoài SP, runtime đọc trực tiếp EInvoiceProviderBodyField, kiểm tra/ghi execution log và upsert log PMS bằng SQL có tham số; tài khoản chỉ có EXECUTE SP chưa đủ.

## 5. Phân quyền và mã hóa

| Quyền | Phạm vi |
|---|---|
| EInvoice_View | Màn PMS, tìm kiếm, danh sách log/Property/action |
| EInvoice_Issue | ExecuteBatch gửi nghiệp vụ |
| EInvoice_Config | Provider/Property, Preview, chi tiết log |

Menu dùng data-action; controller dùng RequireFunction kiểm tra UserID và PermissionCodes trong Session. Setup seed ba quyền và gán nhóm 14. Đăng nhập lại sau khi thay đổi quyền. User vận hành thường cần cả View và Issue.

AllowedProperties trả toàn bộ Property PMS active cho nhóm 14, nhóm khác theo PropertyPermission(UserID, PropertyID). Hiện lọc Property nằm ở endpoint danh sách; chưa thấy kiểm tra quyền Property tương ứng trong các service nhận ID trực tiếp để search/execute/log. Cần rà soát server-side nếu triển khai phân tách dữ liệu nhiều đơn vị.

EInvoiceSecret dùng AES-256-CBC, tiền tố enc:, Encryption:Key 32 byte UTF-8 và Encryption:IV 16 byte UTF-8. Dữ liệu không có tiền tố được coi là plaintext cũ. Mật khẩu DB và field IsEncrypted được mã hóa khi lưu; field encrypted không trả SavedValue ra UI, có cơ chế giữ giá trị cũ khi sửa.

Configure khóa hiện nằm trong constructor EInvoicePropertyService. Trên instance mới nên mở Property trước để khởi tạo/kiểm tra; triển khai lâu dài cần bảo đảm khởi tạo độc lập với thứ tự mở màn. Giữ đúng khóa/IV nếu tiếp tục sử dụng ciphertext cũ khi chuyển môi trường.

Preview che key nhạy cảm trong header/query/form/JSON theo tên key; không bảo đảm che mọi secret trong URL/path/document nhúng hoặc metadata mapping. Log thực thi không áp cùng bước masking Preview.

## 6. Các bước triển khai và chạy

### Bước 1: Chuẩn bị

- .NET SDK 9; SQL Server; SSMS hoặc sqlcmd hỗ trợ batch GO.
- Cấu hình WebApp: ConnectionStrings:DefaultConnection, Encryption:Key, Encryption:IV theo môi trường, không dùng giá trị bí mật trong tài liệu.
- WebApp kết nối được DB cấu hình, DB nguồn và API provider.
- Tài khoản DB cấu hình có quyền đọc/ghi bảng module và EXECUTE SP. Tài khoản triển khai có quyền tạo bảng/SP và seed quyền.
- Tài khoản DB nguồn có EXECUTE InvoiceSearchSP, SELECT bảng header/detail, đọc metadata và INSERT/UPDATE hai bảng log PMS.

### Bước 2: Setup DB cấu hình

Chọn đúng DB theo DefaultConnection trong SSMS, mở và chạy toàn bộ Database/EInvoice_Setup.sql. Script tạo bảng nếu chưa có và DROP/CREATE SP qua nhiều batch GO. Kiểm tra lỗi của mọi batch; batch cuối thành công không chứng minh toàn bộ setup thành công.

Không có EF migration tự chạy. Setup không tạo thay bảng nghiệp vụ PMS hoặc spVAT_SearchVAT_HDDT trong DB nguồn.

Kiểm tra:

~~~sql
SELECT Code, Name, Inactive FROM dbo.EInvoiceProvider;
SELECT Code FROM dbo.FormAndFunction
WHERE Code IN ('EInvoice_Config', 'EInvoice_Issue', 'EInvoice_View');
EXEC dbo.spEInvoice_ProviderSearch;
~~~

### Bước 3: Build và mở ứng dụng

Từ thư mục gốc repo:

~~~powershell
dotnet restore WebApp.sln
dotnet build WebApp/WebApp.csproj --no-restore
dotnet run --project WebApp/WebApp.csproj --launch-profile http
~~~

Profile http hiện dùng http://localhost:5012, môi trường Development. Nếu dependency/feed riêng cần quyền truy cập, cấu hình trước restore. Đăng nhập với quyền phù hợp, mở Property E-Invoice trước rồi vào màn PMS.

### Bước 4: Cấu hình Property hoặc chuyển dữ liệu

Cấu hình mới: SourceType=PMS, kết nối DB nguồn, InvoiceSearchSP=dbo.spVAT_SearchVAT_HDDT nếu DB dùng SP đó, provider mặc định và các field tương ứng. Test Connection, đọc cả warnings rồi lưu.

Chuyển từ hệ thống cũ: Database/EInvoice_Data_Migration.sql chạy sau setup. File chứa thông tin đăng nhập, đã bị gitignore loại và không có sẵn khi clone repo mới. Không đưa nội dung bí mật vào tài liệu/commit. Không cần migration nếu cấu hình mới qua UI.

Migration hiện chuyển Property PMS, liên kết và giá trị; không chuyển execution log hoặc PropertyPermission. Cần kiểm tra ID/Code và provider/field trước khi chạy: điều kiện bỏ qua bản ghi tồn tại không tự giải quyết xung đột ID.

Sau chuyển dữ liệu, dùng nút Encrypt Secrets ở Property E-Invoice: POST /EInvoiceProperty/PropertyEncryptSecrets. Đổi endpoint/tài khoản sang môi trường demo trước khi kiểm thử gửi hóa đơn.

### Bước 5: Preview và kiểm thử

Preview cần PropertyID, OperationID, FolioVATID hợp lệ. Chỉ dựng request, không gọi provider, không chạy AUTH_LOGIN và không ghi execution log. Mapping token required có thể lỗi khi Preview vì chưa có response đăng nhập.

Preview fallback API_URL → BASE_URL nhưng ExecuteAction hiện bắt buộc BASE_URL. Phải có BASE_URL để chạy thật.

Kiểm thử demo:

1. Tìm một hóa đơn; đối chiếu khách hàng, tiền, thuế, detail.
2. Xem Preview, so request với API contract; xét riêng GUID/ngày/token động.
3. Gửi một hóa đơn, kiểm tra HTTP + ProviderSuccess + số hóa đơn + log PMS.
4. Với luồng hai bước, kiểm tra Upload rồi Publish và trường hợp Upload thành công/Publish thất bại.
5. Kiểm tra batch có lỗi từng phần, gửi trùng, sai tài khoản, tiền không hợp lệ, quyền chức năng/Property.
6. Đối soát được trường hợp provider thành công nhưng PMS không cập nhật.

## 7. Endpoint và ví dụ request

Các ID bên dưới chỉ minh họa; dùng phiên đăng nhập và dữ liệu demo thực tế. GET danh sách trả {totalCount, data}; POST nghiệp vụ trả {success, message, data, errors}. Giữ cookie/session và token theo ứng dụng; DevTools Network giúp lấy đúng request của UI.

| Route | Mục đích |
|---|---|
| GET /EInvoiceProvider/ProviderManagement | Quản trị Provider/API |
| GET /EInvoiceProperty/PropertyEinvoice | Cấu hình Property |
| GET /EInvoiceInvoice/InvoiceManagement | Màn PMS |
| GET /EInvoiceInvoice/InvoicePreview | Partial view Preview |
| GET /EInvoiceInvoice/AllowedProperties | Property được chọn |
| GET /EInvoiceInvoice/ProviderActions?propertyId=1 | Nghiệp vụ provider |
| GET /EInvoiceInvoice/InvoiceData | Tìm hóa đơn |
| POST /EInvoiceInvoice/PreviewRequest | Dựng request |
| POST /EInvoiceInvoice/ExecuteBatch | Gửi nghiệp vụ |
| GET /EInvoiceInvoice/LogData | Danh sách log |
| GET /EInvoiceInvoice/LogDetail?id=100 | Chi tiết log, cần Config |

Tìm hóa đơn:

~~~http
GET /EInvoiceInvoice/InvoiceData?PropertyID=1&FromDate=2026-11-01&ToDate=2026-11-19&ResType=-1&Print=0
Accept: application/json
~~~

Preview:

~~~json
{
  "propertyID": 1,
  "operationID": 10,
  "folioVATID": 123
}
~~~

ExecuteBatch — có gửi API provider:

~~~json
{
  "propertyID": 1,
  "actionCode": "UPLOAD_INVOICE",
  "folioVatIds": [123],
  "confirmSend": true
}
~~~

Đổi ActionCode sang PUBLISH_INVOICE, ISSUE_INVOICE hoặc UPLOAD_AND_PUBLISH theo cấu hình hỗ trợ. Đọc log: /EInvoiceInvoice/LogData?PropertyID=1&FolioVatId=123.

## 8. Truy vấn đối soát và xử lý lỗi

Trên DB cấu hình:

~~~sql
DECLARE @PropertyID int = 1, @FolioVATID int = 123;
EXEC dbo.spEInvoice_RuntimeProvider @PropertyID = @PropertyID;
EXEC dbo.spEInvoice_ProviderActions @PropertyID = @PropertyID;
EXEC dbo.spEInvoice_LogSearch
    @PropertyID = @PropertyID, @FolioVATID = @FolioVATID;
SELECT TOP (20) ID, PropertyID, SourceRecordID, ProviderID, OperationID,
       OperationCode, HttpSuccess, HttpStatusCode, ProviderSuccess,
       InvoiceNo, ErrorMessage, CreatedDate
FROM dbo.EInvoiceProviderExecutionLog
WHERE PropertyID = @PropertyID AND SourceRecordID = @FolioVATID
ORDER BY ID DESC;
~~~

Trên DB nguồn của Property:

~~~sql
DECLARE @FolioVATID int = 123;
SELECT * FROM dbo.FolioVAT WHERE ID = @FolioVATID;
SELECT * FROM dbo.FolioVATDetail WHERE FolioVATID = @FolioVATID;
SELECT * FROM dbo.tblUploadInvoiceLog WHERE FolioVATID = @FolioVATID;
SELECT * FROM dbo.tblPulishInvoiceLog WHERE FolioVATID = @FolioVATID;
~~~

| Hiện tượng | Kiểm tra |
|---|---|
| Thiếu bảng/SP module | Đúng DefaultConnection; setup có batch lỗi không |
| Không thấy menu/403 | Mã quyền, UserGroupPermission, Session PermissionCodes; đăng nhập lại |
| Property trống | SourceType PMS, Inactive, UserID/UserGroupID, PropertyPermission |
| Test DB OK nhưng search lỗi | Chữ ký SP nguồn, quyền EXECUTE, cột kết quả |
| Không có operation nghiệp vụ | BusinessActionCode, Inactive, provider mặc định |
| Thiếu BASE_URL | FieldCode/giá trị của provider mặc định |
| Mapping/document lỗi | Required, SourceValue/TargetPath, BodyField, dữ liệu nguồn |
| Publish thiếu Upload trước | Log Upload thành công, RawResponse và parser; lịch sử có được chuyển không |
| HTTP 200 nhưng nghiệp vụ lỗi | ResponseRule, ErrorCode/ErrorMessage mapping, RawResponse |
| Báo đã xử lý thành công | Log cùng operation; đối soát provider và trạng thái PMS |
| Provider OK nhưng PMS lỗi | Quyền ghi, bảng/cột log PMS; xử lý đồng bộ, không tự phát hành lại |
| Decrypt/khóa chưa cấu hình | Đúng Key/IV và khởi tạo EInvoicePropertyService |
| Upload & Publish chạy lại bị chặn | Upload đã thành công; sau đối soát chỉ chạy Publish |

## 9. Giới hạn và hướng mở rộng

1. Chỉ bốn action được màn PMS cho phép. Có dữ liệu Adjust/Replace/Cancel không đồng nghĩa UI đã thực thi được các nghiệp vụ này.
2. Nhiều operation active cùng provider/business action: runtime chọn ID đầu tiên, khác thứ tự DisplayOrder hiển thị UI; nên cấu hình rõ ràng.
3. RuntimePrevious chưa lọc ProviderID khi đọc Upload thành công. Đổi provider cần kiểm tra kết quả cũ trước Publish.
4. AuthenticateAsync thay PreviousResponse bằng JSON đăng nhập. Provider cần đồng thời token và kết quả Upload để Publish phải kiểm tra kỹ dữ liệu context.
5. UI chấp nhận nhiều BodyType; thêm giao thức cần kiểm tra nhánh gửi thực tế trong ProviderExecutor.cs, không chỉ danh sách validate. Có nhánh SOAP11 riêng.
6. RequestBody trong log là JSON từ result.Request.Body của engine; với SOAP/form không mặc định coi đó là toàn bộ wire payload và headers.
7. Check trùng phụ thuộc log trung tâm, migration không chuyển log; hóa đơn cũ cần đối soát trước vận hành.
8. Runtime còn model/nhánh POS từ code port nhưng luồng controller/service hiện tại chỉ hỗ trợ PMS.
9. Chưa có RuleSave và CRUD BodyField riêng; thay đổi qua SQL có kiểm soát.
10. Không cần service riêng cho provider nếu engine đã hỗ trợ giao thức/payload. Nếu có dữ liệu nguồn, xác thực hoặc parser mới thì phải bổ sung runtime.

Quy trình thêm provider: Provider → Field/Option → Operation/BusinessActionCode → Mapping → ResponseRule/BodyField → Property/config → Preview → test demo → đối soát log và PMS. Chỉ xác nhận tích hợp hoàn tất sau khi kiểm chứng trên môi trường đích.

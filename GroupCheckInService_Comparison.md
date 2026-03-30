# GroupCheckInService Comparison

## Kết luận ngắn

Không thể khẳng định 100% đúng nghiệp vụ chỉ bằng đối chiếu code tĩnh.

Mức độ khớp hiện tại:

- Khớp tốt với flow lõi `ProfileGroupId`
- Khớp phần lớn với flow `ConfirmationNo`
- Còn 1 điểm chưa khớp hoàn toàn với code gốc: xử lý alert theo từng guest

So sánh này đối chiếu giữa:

- `C:\Users\Tuan\Downloads\CodeGoc.cs`
- `Reservation\Services\Implements\GroupCheckInService.cs`
- `Reservation\Services\Implements\CheckInService.cs`

## Các điểm đã khớp theo code gốc

| Nghiệp vụ | Code gốc | Code mới | Đánh giá |
| --- | --- | --- | --- |
| Engine check-in thật dùng `CreateCheckIn` | `CodeGoc.cs:11` | `GroupCheckInService.cs:195` gọi `CheckInService.CreateCheckIn(...)` | Khớp ý tưởng |
| Flow `ProfileGroupId` lấy danh sách từ `spGroupReservationToDelete` | `CodeGoc.cs:939` | `GroupCheckInService.cs:272` | Khớp |
| Flow `ProfileGroupId` gọi engine từng item với mode group | `CodeGoc.cs:955`, `966`, `977`, `988` đều dùng `..., 1)` | `GroupCheckInService.cs:187` dùng `Mode = 1` | Khớp |
| Flow `ProfileGroupId` không check alert từng reservation ở ngoài | Không có bước alert ở đoạn `mode == 0` của group form | `GroupCheckInService.cs:63` dùng `applyAlertRule: false` | Khớp |
| Flow `ConfirmationNo` xử lý danh sách từng item | `CodeGoc.cs:1070` | `GroupCheckInService.cs:104` | Khớp |
| Flow `ConfirmationNo` chỉ quan tâm alert có `Area == "Check-In"` | `CodeGoc.cs:1084` | `GroupCheckInService.cs:254` | Khớp |
| Nếu main guest bị bỏ qua thì block sharer cùng `ShareRoom` | `CodeGoc.cs:1103` | `GroupCheckInService.cs:165`, `175` | Khớp ý tưởng |
| `Clean / Inspected / All` được lọc trước khi check-in | `CodeGoc.cs:953`, `964`, `975`, `986` | `GroupCheckInService.cs:154`, `213` | Khớp |

## Các thay đổi có chủ đích

| Thay đổi | Lý do | Ảnh hưởng |
| --- | --- | --- |
| Bỏ toàn bộ code WinForms khỏi `GroupCheckInService.cs` | File cũ đang lẫn `MessageBox`, `Thread`, `lblStatus`, `frm*`, không đúng vai trò service backend và không compile ổn định | Cần thiết phải sửa |
| Dùng `CheckInService.CheckInRequest` để map rồi gọi engine | File cũ gọi sai chữ ký hàm `CreateCheckIn(item, userId)` | Sửa lỗi compile và đúng contract hiện tại |
| Đọc alert bằng `ReservationAlertsModel.Description` | `GetReservationAlerts()` trả `ArrayList`, model thật có `Description`, không có `Message` | Sửa lỗi compile và lỗi logic |
| Giữ thứ tự `SelectedReservationIds` | Flow block sharer phụ thuộc thứ tự xử lý main guest trước sharer | Tốt hơn bản cũ trong service web |
| Sửa điều kiện `CleanOrInspected` thành `hk == 1 || hk == 4` rõ ràng | `CodeGoc.cs:975` có bug precedence | Đây là sửa bug cũ, không nên bê nguyên |

## Điểm chưa khớp hoàn toàn với code gốc

### 1. Alert trong flow `ConfirmationNo`

Code gốc:

- Nếu guest có alert `Check-In`, form hiển thị alert
- Sau đó hỏi riêng guest đó: `"Do you want to check in this guest?"` ở `CodeGoc.cs:1097`
- User có thể:
  - cho guest này vào
  - hoặc bỏ guest này
  - nếu bỏ main guest thì sharer cùng `ShareRoom` bị block

Code mới:

- `GroupCheckInService.cs:159` dùng `request.IgnoreAlert`
- Nghĩa hiện tại chỉ có 2 kiểu:
  - `IgnoreAlert = true`: bỏ qua tất cả alert
  - `IgnoreAlert = false`: guest nào có alert sẽ bị fail

Kết luận:

- Đây là điểm **chưa khớp 100%** với code gốc
- Nó đang gần với cơ chế:
  - `IgnoreAlert = true` tương đương "Yes to all alert guests"
  - `IgnoreAlert = false` tương đương "No to all alert guests"
- Nó **không hỗ trợ quyết định riêng từng guest** như WinForms cũ

### 2. Xác nhận trước khi chạy group

Code gốc có bước hỏi:

- `"Are you sure you want to check in this group?"`

Code mới không có bước này trong service vì đây là trách nhiệm của UI/API caller, không phải business service.

Kết luận:

- Khác về tầng triển khai
- Không phải lệch nghiệp vụ lõi

## Những lỗi cụ thể đã được sửa

### 1. Lỗi gọi sai `CreateCheckIn`

File cũ gọi:

- `CheckInService.CreateCheckIn(item, request.UserId)`

Nhưng hàm thật nhận:

- `CheckInService.CheckInRequest`

Đã sửa:

- `GroupCheckInService.cs:180-193` map sang request đúng
- `GroupCheckInService.cs:195` gọi đúng overload

### 2. Lỗi `FirstOrDefault`

Nguyên nhân cũ:

- `GetReservationAlerts()` trả `ArrayList`
- code lại gọi `alerts.FirstOrDefault()?.Message`
- model thật là `ReservationAlertsModel.Description`

Đã sửa:

- `GroupCheckInService.cs:242-261`
- duyệt `ArrayList`
- cast đúng `ReservationAlertsModel`
- chỉ lấy alert `Area == "Check-In"`
- đọc `Description`

## Mức độ tin cậy theo từng flow

### Flow `ProfileGroupId`

Độ tin cậy: cao

Lý do:

- đối chiếu khá thẳng với `CodeGoc.cs:933-999`
- không có phần confirm theo từng guest
- gọi engine với `Mode = 1` đúng code gốc

### Flow `ConfirmationNo`

Độ tin cậy: trung bình đến cao

Lý do:

- thứ tự xử lý, alert `Check-In`, block sharer đều đã được giữ
- nhưng phần quyết định alert theo từng guest chưa khớp hoàn toàn

## Trường hợp cần test tay để chốt nghiệp vụ

1. `ProfileGroupId` với tất cả phòng `HKStatusId = 4`
2. `ProfileGroupId` với `FilterType = 3` có cả room clean và inspected
3. `ConfirmationNo` có main guest bị alert `Check-In`, `IgnoreAlert = false`
4. `ConfirmationNo` có sharer đi sau main guest cùng `ShareRoom`
5. `ConfirmationNo` có nhiều guest có alert, cần xác nhận xem business muốn:
   - bỏ qua tất cả
   - chặn tất cả
   - hay chọn từng guest như code gốc

## Kết luận cuối

Nếu tiêu chí là:

- sửa file cho chạy được
- bỏ lỗi compile
- giữ phần lớn logic gốc

thì bản hiện tại đạt.

Nếu tiêu chí là:

- khớp 100% hành vi WinForms cũ

thì **chưa đạt 100%** vì còn thiếu cơ chế xác nhận alert theo từng guest ở flow `ConfirmationNo`.

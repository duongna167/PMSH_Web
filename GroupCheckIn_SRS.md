# Group Check-In SRS

## 1. Muc dich tai lieu

Tai lieu nay mo ta chuc nang `Group Check-In` sau khi da sua lai luong backend trong he thong web.
Muc tieu la de:

- Doc lai nghiep vu dang duoc ap dung.
- Doi chieu voi code da sua.
- Co checklist kiem thu chuc nang.
- Nhan biet ro diem nao dang bam logic goc, diem nao la gioi han hien tai.

Tai lieu nay chi tap trung vao luong group check-in moi sua, khong mo ta toan bo module reservation.

## 2. Pham vi chuc nang

Chuc nang cho phep check-in nhieu reservation trong cung mot nhom.

He thong hien tai ho tro 2 kieu dau vao:

- Theo `ProfileGroupId`
- Theo `ConfirmationNo`

Trong UI hien tai, luong dang duoc noi vao controller la luong theo `ConfirmationNo`.

## 3. Thanh phan lien quan

### 3.1 View

File: `WebApp/Views/Reservation/GroupAdmin/GroupCheckIn.cshtml`

Trach nhiem:

- Lay danh sach reservation duoc chon tren grid.
- Lay `confirmationNo`.
- Lay `filterType` tu radio button.
- Lay `userId` tu `localStorage`.
- Goi AJAX `POST /Reservation/GroupCheckIn`.

Payload hien tai:

```json
{
  "confirmationNo": "12345",
  "userId": 1,
  "filterType": 3,
  "ignoreAlert": false,
  "selectedReservationIds": [101, 102, 103]
}
```

### 3.2 Controller

File: `Reservation/Controllers/ReservationController.cs`

Action hien tai:

- Nhan `CheckInGroupDTO.CheckInRequest`
- Neu `UserId <= 0` thi lay lai tu `Session["UserID"]`
- Remap `filterType` tu quy uoc UI sang quy uoc cua service
- Goi `_iGroupCheckInService.CheckIn(request)`
- Tra JSON ket qua cho frontend

### 3.3 Service contract

File: `Reservation/Services/Interfaces/IGroupCheckInService.cs`

Co 3 method:

- `CheckIn(CheckInRequest request)`
- `CheckInByProfileGroup(CheckInRequest request)`
- `CheckInByConfirmation(CheckInRequest request)`

### 3.4 Service nghiep vu group

File: `Reservation/Services/Implements/GroupCheckInService.cs`

Trach nhiem:

- Xac dinh dang di theo `ProfileGroupId` hay `ConfirmationNo`
- Doc danh sach reservation ung vien
- Loc theo `filterType`
- Kiem tra alert `Check-In` neu la luong `ConfirmationNo`
- Chan sharer neu main guest bi bo qua do alert
- Map tung item sang `CheckInService.CheckInRequest`
- Goi `CheckInService.CreateCheckIn(...)`
- Tong hop ket qua theo batch

### 3.5 Engine check-in thuc te

File: `Reservation/Services/Implements/CheckInService.cs`

Day la lop xu ly check-in thuc su cho tung reservation.
`GroupCheckInService` khong tu update trang thai phong/reservation bang tay nua ma goi xuong engine nay.

## 4. Mo hinh du lieu

### 4.1 Request model

File: `Reservation/Dto/CheckInGroupDTO.cs`

`CheckInGroupDTO.CheckInRequest` gom:

- `ProfileGroupId`: nullable, dung cho flow theo group
- `ConfirmationNo`: dung cho flow theo so xac nhan
- `UserId`: user thuc hien
- `FilterType`: kieu loc phong
- `IgnoreAlert`: bo qua alert hay khong
- `SelectedReservationIds`: thu tu reservation UI da chon

### 4.2 Result model

`CheckInGroupDTO.CheckInResult` gom:

- `Success`: batch co it nhat 1 item thanh cong hay khong
- `TotalRequested`: tong so item duoc dua vao xu ly
- `TotalCheckedIn`: tong so item thanh cong
- `Message`: thong bao tong
- `Messages`: danh sach thong bao chi tiet
- `Items`: ket qua tung reservation

`CheckInItemResult` gom:

- `ReservationId`
- `RoomNo`
- `Success`
- `Message`

## 5. Quy uoc filterType

Can luu y day la diem de nham nhat trong luong nay.

### 5.1 Quy uoc tren UI

Radio button tren man hinh hien tai dang gui:

- `1`: Clean Rooms Only
- `2`: Clean Non-checked Rooms Only
- `3`: All Rooms
- `4`: Clean Non-checked or Clean Rooms

### 5.2 Quy uoc trong GroupCheckInService

Service dang xu ly:

- `1`: InspectedOnly
- `2`: CleanOnly
- `3`: CleanOrInspected
- `4`: All

### 5.3 Cach controller xu ly

Controller dang remap:

- Neu UI gui `3` thi doi thanh `4`
- Neu UI gui `4` thi doi thanh `3`

Muc dich:

- Giu UI hien tai khong can sua them
- Van de service xu ly dung logic phong

### 5.4 Rule room status trong service

`GroupCheckInService.MatchRoomStatus(...)` dang dung:

- `FilterType = 1`: chi nhan `HKStatusId = 4`
- `FilterType = 2`: chi nhan `HKStatusId = 1`
- `FilterType = 3`: nhan `HKStatusId = 1` hoac `4`
- `FilterType = 4`: nhan tat ca

Theo quy uoc hien tai:

- `HKStatusId = 1`: Clean
- `HKStatusId = 4`: Inspected

## 6. Luong xu ly tong quat

### 6.1 Luong tu UI den engine

1. User chon reservation tren grid group check-in.
2. Frontend tao request JSON va goi `POST /Reservation/GroupCheckIn`.
3. `ReservationController.GroupCheckIn(...)` nhan request.
4. Controller bo sung `UserId` neu can va remap `FilterType`.
5. Controller goi `_iGroupCheckInService.CheckIn(request)`.
6. `GroupCheckInService` xac dinh flow theo `ProfileGroupId` hoac `ConfirmationNo`.
7. Service doc danh sach reservation ung vien.
8. Service duyet tung item:
   - Kiem tra co phong hay khong
   - Kiem tra dat dieu kien room status hay khong
   - Neu la flow `ConfirmationNo` thi kiem tra alert `Check-In`
   - Neu main guest bi chan vi alert thi sharer cung bi chan
   - Neu hop le thi map sang request cua `CheckInService`
   - Goi `CheckInService.CreateCheckIn(...)`
9. Service tong hop ket qua va tra ve controller.
10. Controller tra JSON cho frontend.
11. Frontend hien `showToast(...)`.

## 7. Chi tiet theo tung flow

### 7.1 Flow A - Check-in theo ProfileGroupId

Dieu kien vao:

- `ProfileGroupId > 0`
- `FilterType` hop le

Nguon du lieu:

- Store `spGroupReservationToDelete`

Cach xu ly:

1. Lay danh sach reservation theo `ProfileGroupId`
2. Map thanh `ReservationCheckInDto`
3. Khong ap dung alert rule ben ngoai
4. Goi `CheckInService.CreateCheckIn(...)` cho tung item
5. Tong hop ket qua

Luu y nghiep vu:

- Flow nay dang bam huong xu ly cua code goc: khong pre-check alert theo tung reservation o tang group service.
- `Mode` khi goi engine dang de `1`

### 7.2 Flow B - Check-in theo ConfirmationNo

Dieu kien vao:

- `ConfirmationNo` khong rong
- `ConfirmationNo` phai parse duoc sang so
- `FilterType` hop le

Nguon du lieu:

- `_reservationService.ResConfNoList(Convert.ToInt32(confirmationNo))`

Cach xu ly:

1. Lay danh sach reservation theo confirmation number
2. Neu co `SelectedReservationIds` thi sap lai theo dung thu tu UI da chon
3. Duyet tung reservation
4. Kiem tra alert `Check-In`
5. Neu co alert va `IgnoreAlert = false` thi item bi fail
6. Neu item fail la main guest thi danh dau `blockedShareRoom`
7. Cac sharer cung `ShareRoom` se bi chan o cac vong lap sau
8. Neu khong vi pham thi goi `CheckInService.CreateCheckIn(...)`

Luu y nghiep vu:

- Rule chan sharer hien tai chi ap dung cho flow `ConfirmationNo`
- Thu tu `SelectedReservationIds` duoc giu lai de tranh sai logic main guest/sharer

## 8. Map sang CheckInService

Moi `ReservationCheckInDto` hop le se duoc map thanh `CheckInService.CheckInRequest` nhu sau:

- `ReservationId = item.ReservationId`
- `RoomId = item.RoomId`
- `SelectedRoomId = item.RoomId`
- `NoOfRoom = item.NoOfRoom`
- `UserId = request.UserId`
- `Mode = 1`
- `CheckInSharers = false`
- `IgnoreSharerAlerts = false`
- `PaymentMethod = ""`
- `CreditCardNo = ""`
- `ExpireDate = null`

Y nghia:

- Group service chi lam vai tro dieu phoi batch
- Engine `CheckInService` moi la noi xu ly transaction check-in that su
- Khong nen update `Reservation.Status` thu cong o controller nua

## 9. Quy tac nghiep vu dang ap dung

### 9.1 Rule hop le co ban

- Request null thi fail
- Khong co `ProfileGroupId` va cung khong co `ConfirmationNo` thi fail
- `ConfirmationNo` khong parse duoc sang so thi fail
- `FilterType` ngoai khoang `1..4` thi fail
- Item khong co `RoomId > 0` thi fail

### 9.2 Rule alert

Service chi doc alert co:

- `Area == "Check-In"`

Nguon alert:

- `ReservationUtil.GetReservationAlerts(reservationId)`

Luu y:

- Ham nay tra `ArrayList`, khong phai `List<T>`
- Message dung de hien thi hien tai lay tu `ReservationAlertsModel.Description`

### 9.3 Rule sharer

- Neu current item la main guest
- Va bi chan do alert `Check-In`
- Thi `blockedShareRoom = item.ShareRoom`
- Cac item sau do neu:
  - khong phai main guest
  - `item.ShareRoom == blockedShareRoom`
  - thi se bi fail voi thong diep main guest chua duoc check-in

### 9.4 Rule thanh cong batch

Batch duoc xem la `Success = true` neu:

- Co it nhat 1 item check-in thanh cong

Khong bat buoc toan bo item thanh cong moi tra `Success = true`.

## 10. Doi chieu voi code goc

### 10.1 Diem da bam logic goc

- Group flow khong con update status thu cong o controller
- Group flow goi engine check-in thay vi thao tac SQL ngan gon
- `Mode = 1` khi di theo group
- Flow `ConfirmationNo` co rule alert `Check-In`
- Main guest fail do alert se chan sharer
- Thu tu reservation da chon duoc giu lai

### 10.2 Diem hien tai chua giong 100% code goc

- Code goc cho phep ra quyet dinh theo tung guest khi gap alert
- Ban web hien tai chi co `IgnoreAlert` cho toan request

He qua:

- Neu `IgnoreAlert = false`, moi item co alert se bi chan
- Neu muon hanh vi giong hoan toan code goc, can them co che hoi/xac nhan theo tung reservation

### 10.3 Diem sua de phu hop backend web

- Tach ro `Controller` va `Service`
- Dung DI thong qua `IGroupCheckInService`
- Dung DTO request/response ro rang
- Dung engine `CheckInService` cho side effects day du hon

## 11. Tinh huong loi da sua

Day la cac loi chinh da duoc sua trong dot nay:

- Controller cu nhan sai payload `List<int>` trong khi frontend gui object request
- Controller cu bypass business flow, chi update status truc tiep
- `GroupCheckInService` cu goi sai chu ky `CheckInService.CreateCheckIn`
- `GroupCheckInService` cu dung sai `FirstOrDefault` tren `ArrayList`
- `GroupCheckInService` cu doc sai field alert
- Chua giu thu tu `SelectedReservationIds`

## 12. Checklist kiem thu de nghi

### 12.1 Smoke test

1. Mo man hinh Group Check-In
2. Chon it nhat 1 reservation
3. Bam check-in
4. Xac nhan frontend goi `/Reservation/GroupCheckIn`
5. Xac nhan API tra JSON co `code`, `msg`, `result`

Ket qua mong doi:

- Khong loi model binding
- Khong loi null request
- Co toast thanh cong hoac that bai ro rang

### 12.2 Test theo filter

Case A:

- Chon reservation co `HKStatusId = 4`
- Gui `FilterType` UI = `1`

Mong doi:

- Item hop le

Case B:

- Chon reservation co `HKStatusId = 1`
- Gui `FilterType` UI = `2`

Mong doi:

- Item hop le

Case C:

- Chon reservation co `HKStatusId = 1` va `4`
- Gui `FilterType` UI = `4`

Mong doi:

- Sau remap, service xu ly nhu `CleanOrInspected`

Case D:

- Gui `FilterType` UI = `3`

Mong doi:

- Sau remap, service xu ly nhu `All`

### 12.3 Test alert

Case E:

- Reservation co alert `Area = "Check-In"`
- `IgnoreAlert = false`

Mong doi:

- Item fail
- `ItemResult.Message` co noi dung alert

Case F:

- Reservation co alert `Area = "Other"`
- `IgnoreAlert = false`

Mong doi:

- Khong bi chan boi rule alert group service

Case G:

- Reservation co alert `Area = "Check-In"`
- `IgnoreAlert = true`

Mong doi:

- Group service bo qua rule alert ben ngoai va cho phep goi engine

### 12.4 Test sharer

Case H:

- Main guest va sharer cung `ShareRoom`
- Main guest co alert `Check-In`
- `IgnoreAlert = false`

Mong doi:

- Main guest fail
- Sharer sau do fail voi ly do bi chan boi main guest

Case I:

- Main guest khong bi chan
- Sharer cung `ShareRoom`

Mong doi:

- Sharer khong bi chan boi group service

### 12.5 Test request khong hop le

Case J:

- Request null

Mong doi:

- Tra `code = 1`, message phu hop

Case K:

- Khong co `ProfileGroupId`, khong co `ConfirmationNo`

Mong doi:

- Fail som tai service

Case L:

- `ConfirmationNo = "ABC"`

Mong doi:

- Fail vi confirmation phai la so

Case M:

- `FilterType = 0` hoac `5`

Mong doi:

- Fail vi filter khong hop le

### 12.6 Test item khong co phong

Case N:

- Reservation co `RoomId = 0`

Mong doi:

- Item fail voi message khong co phong duoc gan

### 12.7 Test tong hop ket qua

Case O:

- Batch co 3 item, 1 item thanh cong, 2 item fail

Mong doi:

- `Success = true`
- `TotalRequested = 3`
- `TotalCheckedIn = 1`
- `Items.Count = 3`

Case P:

- Batch tat ca deu fail

Mong doi:

- `Success = false`
- `Message` la thong diep tong the that bai

## 13. Du lieu test toi thieu can chuan bi

Nen co it nhat cac nhom du lieu sau:

- 1 reservation clean
- 1 reservation inspected
- 1 reservation co alert `Check-In`
- 1 cap main guest + sharer cung `ShareRoom`
- 1 reservation khong co room
- 1 confirmation number co nhieu reservation de test thu tu duoc chon

## 14. Tieu chi nghiem thu

Chuc nang duoc xem la dat toi thieu khi:

- Frontend gui dung payload va controller bind duoc
- Controller goi service qua `IGroupCheckInService`
- Service goi `CheckInService.CreateCheckIn(...)` cho item hop le
- Rule alert `Check-In` hoat dong dung cho flow `ConfirmationNo`
- Rule chan sharer hoat dong dung
- Ket qua batch tra ve day du `Success`, `Message`, `Items`

## 15. Gioi han hien tai

- UI hien tai chu yeu dang dung flow theo `ConfirmationNo`
- Chua co co che xac nhan alert theo tung guest nhu code goc desktop
- `IgnoreAlert` hien la quyet dinh ap dung cho toan request

## 16. File lien quan de doc tiep

- `Reservation/Dto/CheckInGroupDTO.cs`
- `Reservation/Services/Interfaces/IGroupCheckInService.cs`
- `Reservation/Services/Implements/GroupCheckInService.cs`
- `Reservation/Services/Implements/CheckInService.cs`
- `Reservation/Controllers/ReservationController.cs`
- `WebApp/Views/Reservation/GroupAdmin/GroupCheckIn.cshtml`
- `GroupCheckInService_Comparison.md`


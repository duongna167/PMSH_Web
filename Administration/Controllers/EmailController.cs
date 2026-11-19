using Administration.Services.Implements;
using Administration.Services.Interfaces;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using DevExpress.Office.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using static System.Runtime.InteropServices.JavaScript.JSType;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Administration.Controllers
{
    public class EmailController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IEmailService _iEmailService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public EmailController(ILogger<EmailController> logger,
                IMemoryCache cache, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, IEmailService iEmailService)
        {
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _iEmailService = iEmailService;
        }
        public IActionResult SetupEmail()
        {
            return View();
        }

        public IActionResult SendEmail()
        {
            return View();
        }
        public IActionResult SendSMS()
        {
            return View();
        }
        public IActionResult EmailSendingHistory()
        {
            return View();
        }
        public IActionResult SendEmailBirthday()
        {
            return View();

        }
        #region DatVP __ SetupEmail: Get config email in config system
        [HttpGet]

        public IActionResult GetEmailConfig()
        {
            try
            {
                PmsMailConfig.EnsureVietAnhSmtpConfig();
                string senderName = PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindByAttribute("KeyName", "MAIL_FROMNAME")).FirstOrDefault().KeyValue;
                string host = PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindByAttribute("KeyName", "MAIL_SERVERNAME")).FirstOrDefault().KeyValue;
                string port = PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindByAttribute("KeyName", "MAIL_SERVERPORT")).FirstOrDefault().KeyValue;
                string userName = PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindByAttribute("KeyName", "MAIL_FROM")).FirstOrDefault().KeyValue;
                string password = PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindByAttribute("KeyName", "MAIL_PASSWORD")).FirstOrDefault().KeyValue;
                string ssl = PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindByAttribute("KeyName", "MAIL_USE_SSL")).FirstOrDefault().KeyValue;

                return Json(new
                {
                    senderName = senderName,
                    host = host,
                    port = port,
                    userName = userName,
                    password = password,
                    ssl = ssl
                });
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

        }
        #endregion

        #region DatVP __ SetUpEmail: Save config email
        [HttpPost]
        public ActionResult SaveConfigEmail()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                #region lưu sender name
                ConfigSystemModel senderName = PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindByAttribute("KeyName", "MAIL_FROMNAME")).FirstOrDefault();
                if (senderName == null)
                {
                    senderName.KeyName = "MAIL_FROMNAME";
                    senderName.KeyValue = Request.Form["senderName"];
                    senderName.Desciption = "Sender Name";
                    senderName.UserInsertID = senderName.UserUpdateID = int.Parse(Request.Form["userID"]);
                    senderName.UpdateDate = senderName.CreateDate = DateTime.Now;
                    ConfigSystemBO.Instance.Insert(senderName);
                }
                else
                {
                    senderName.KeyValue = Request.Form["senderName"];
                    senderName.UserUpdateID = int.Parse(Request.Form["userID"]);
                    senderName.UpdateDate = DateTime.Now;
                    ConfigSystemBO.Instance.Update(senderName);

                }

                #endregion


                #region lưu host
                ConfigSystemModel host = PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindByAttribute("KeyName", "MAIL_SERVERNAME")).FirstOrDefault();
                if (host == null)
                {
                    host.KeyName = "MAIL_SERVERNAME";
                    host.KeyValue = Request.Form["host"];
                    host.Desciption = "Server host";
                    host.UserInsertID = host.UserUpdateID = int.Parse(Request.Form["userID"]);
                    host.UpdateDate = host.CreateDate = DateTime.Now;
                    ConfigSystemBO.Instance.Insert(host);
                }
                else
                {
                    host.KeyValue = Request.Form["host"];
                    host.UserUpdateID = int.Parse(Request.Form["userID"]);
                    host.UpdateDate = DateTime.Now;
                    ConfigSystemBO.Instance.Update(host);

                }
                #endregion

                #region  lưu port
                ConfigSystemModel port = PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindByAttribute("KeyName", "MAIL_SERVERPORT")).FirstOrDefault();
                if (port == null)
                {
                    port.KeyName = "MAIL_SERVERPORT";
                    port.KeyValue = Request.Form["port"];
                    port.Desciption = "Server port";
                    port.UserInsertID = port.UserUpdateID = int.Parse(Request.Form["userID"]);
                    port.UpdateDate = port.CreateDate = DateTime.Now;
                    ConfigSystemBO.Instance.Insert(port);
                }
                else
                {
                    port.KeyValue = Request.Form["port"];
                    port.UserUpdateID = int.Parse(Request.Form["userID"]);
                    port.UpdateDate = DateTime.Now;
                    ConfigSystemBO.Instance.Update(port);

                }
                #endregion

                #region lưu user name
                ConfigSystemModel userName = PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindByAttribute("KeyName", "MAIL_FROM")).FirstOrDefault();
                if (userName == null)
                {
                    userName.KeyName = "MAIL_FROM";
                    userName.KeyValue = Request.Form["userNameEmail"];
                    userName.Desciption = "user name";
                    userName.UserInsertID = userName.UserUpdateID = int.Parse(Request.Form["userID"]);
                    userName.UpdateDate = userName.CreateDate = DateTime.Now;
                    ConfigSystemBO.Instance.Insert(userName);
                }
                else
                {
                    userName.KeyValue = Request.Form["userNameEmail"];
                    userName.UserUpdateID = int.Parse(Request.Form["userID"]);
                    userName.UpdateDate = DateTime.Now;
                    ConfigSystemBO.Instance.Update(userName);

                }
                #endregion

                #region lưu password
                ConfigSystemModel password = PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindByAttribute("KeyName", "MAIL_PASSWORD")).FirstOrDefault();
                if (password == null)
                {
                    password.KeyName = "MAIL_PASSWORD";
                    password.KeyValue = Request.Form["password"];
                    password.Desciption = "password";
                    password.UserInsertID = password.UserUpdateID = int.Parse(Request.Form["userID"]);
                    password.UpdateDate = password.CreateDate = DateTime.Now;
                    ConfigSystemBO.Instance.Insert(password);
                }
                else
                {
                    password.KeyValue = Request.Form["password"];
                    password.UserUpdateID = int.Parse(Request.Form["userID"]);
                    password.UpdateDate = DateTime.Now;
                    ConfigSystemBO.Instance.Update(password);

                }
                #endregion

                #region lưu ssl
                ConfigSystemModel ssl = PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindByAttribute("KeyName", "MAIL_USE_SSL")).FirstOrDefault();
                if (ssl == null)
                {
                    ssl.KeyName = "MAIL_USE_SSL";
                    ssl.KeyValue = Request.Form["ssl"];
                    ssl.Desciption = "ssl";
                    ssl.UserInsertID = ssl.UserUpdateID = int.Parse(Request.Form["userID"]);
                    ssl.UpdateDate = ssl.CreateDate = DateTime.Now;
                    ConfigSystemBO.Instance.Insert(ssl);
                }
                else
                {
                    ssl.KeyValue = Request.Form["ssl"];
                    ssl.UserUpdateID = int.Parse(Request.Form["userID"]);
                    ssl.UpdateDate = DateTime.Now;
                    ConfigSystemBO.Instance.Update(ssl);

                }
                #endregion


                pt.CommitTransaction();
                return Json(new { code = 0, msg = $"Saved config system" });

            }
            catch (Exception ex)
            {
                pt.RollBack();
                return Json(new { code = 1, msg = ex.Message });
            }
            finally
            {
                pt.CloseConnection();

            }
        }
        #endregion

        #region DatVP __ SetupEmail: Get all parameter
        [HttpGet]
        public IActionResult GetAllParameter()
        {
            try
            {
                var senderName = PropertyUtils.ConvertToList<ParameterMailModel>(ParameterMailBO.Instance.FindAll()).ToList();


                return Json(senderName);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

        }
        #endregion

        #region DatVP __ SetupEmail: Save param
        [HttpPost]
        public ActionResult SaveParam()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                #region lưu sender name
                ParameterMailModel param = PropertyUtils.ConvertToList<ParameterMailModel>(ParameterMailBO.Instance.FindByAttribute("Parameter", Request.Form["param"])).FirstOrDefault();
                if (param != null)
                {
                    return Json(new { code = 1, msg = "Param invalid" });

                }
                ParameterMailModel paramSave = new ParameterMailModel();
                paramSave.Parameter = Request.Form["param"];
                ParameterMailBO.Instance.InsertStringNoneId(paramSave);
                #endregion



                pt.CommitTransaction();
                return Json(new { code = 0, msg = $"Saved param" });

            }
            catch (Exception ex)
            {
                pt.RollBack();
                return Json(new { code = 1, msg = ex.Message });
            }
            finally
            {
                pt.CloseConnection();

            }
        }
        #endregion

        #region DatVP __  SetupEmail: Get all template email
        [HttpGet]
        public IActionResult GetAllTemplateEmail()
        {
            try
            {
                PmsMailConfig.EnsureResetPasswordTemplate();
                var senderName = PropertyUtils.ConvertToList<ContactEmailTemplateModel>(ContactEmailTemplateBO.Instance.FindAll()).ToList();


                return Json(senderName);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

        }
        #endregion

        #region DatVP __ SetupEmail: Save contact template email
        [HttpPost]
        public ActionResult SaveContactTemplateEmail()
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();
                ContactEmailTemplateModel model = new ContactEmailTemplateModel();
                model.Name = Request.Form["subject"];
                model.Content = Request.Form["content"];
                model.Language = 1;
                ContactEmailTemplateBO.Instance.Insert(model);


                pt.CommitTransaction();
                return Json(new { code = 0, msg = $"Saved contact email template" });

            }
            catch (Exception ex)
            {
                pt.RollBack();
                return Json(new { code = 1, msg = ex.Message });
            }
            finally
            {
                pt.CloseConnection();

            }
        }
        #endregion

        #region DatVP __ SendEmail: Get Email of guest
        [HttpGet]
        public IActionResult GetAllEmailOfGuest(DateTime fromDate,DateTime toDate, int status)
        {
            try
            {
                var data = _iEmailService.GetAllEmailOfGuest(fromDate,toDate, status);

                var result = (from d in data.AsEnumerable()
                              select d.Table.Columns.Cast<DataColumn>()
                                  .ToDictionary(
                                      col => col.ColumnName,
                                      col => d[col.ColumnName]?.ToString()
                                  )).ToList();
                return Json(result);


            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

        }
        #endregion

        #region DatVP __ SendEmail: Send Email
        [HttpPost]
        public ActionResult SendEmailGuest(int templateID, List<int> profileIDs)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                // Lấy mẫu email
                ContactEmailTemplateModel contactEmail = (ContactEmailTemplateModel)ContactEmailTemplateBO.Instance.FindByPrimaryKey(templateID);
                if (contactEmail == null)
                {
                    return Json(new { code = 1, msg = "Không tìm thấy mẫu email" });
                }
                string subject = contactEmail.Name;
                string htmlContent = contactEmail.Content;

                // Xử lý từng hồ sơ
                foreach (int profileId in profileIDs)
                {
                    ProfileModel profile = (ProfileModel)ProfileBO.Instance.FindByPrimaryKey(profileId);
                    if (profile == null)
                    {
                        return Json(new { code = 1, msg = $"Không tìm thấy hồ sơ với ID {profileId}." });
                    }

                    // Đảm bảo hồ sơ có email
                    if (string.IsNullOrEmpty(profile.Email))
                    {
                        continue;
                    }

                    // Thay thế placeholder trong nội dung email
                    string pattern = @"##(\w+)##";
                    string personalizedContent = Regex.Replace(htmlContent, pattern, match =>
                    {
                        string propertyName = match.Groups[1].Value;
                        PropertyInfo propertyInfo = profile.GetType().GetProperty(propertyName);
                        if (propertyInfo != null)
                        {
                            object value = propertyInfo.GetValue(profile);
                            return value?.ToString() ?? string.Empty;
                        }
                        return match.Value;
                    });

                    // Tạo tệp Word
                    MemoryStream wordDocumentStream = GenerateReservationWordDocument(profile, contactEmail);

                    // Gửi email với tệp đính kèm
                    SendEmail(profile.Email, subject, personalizedContent, wordDocumentStream);

                    // Lưu lịch sử email
                    ContactEmailHistoryModel history = new ContactEmailHistoryModel
                    {
                        GroupID = 0,
                        Subject = contactEmail.Name,
                        Content = contactEmail.Content,
                        ContactRelateID = 0,
                        SendBy = 0,
                        SendDate = DateTime.Now,
                        SendOK = 1,
                        SendTo = profile.Email,
                        Reason = "",
                        SuccessSendDate = DateTime.Now,
                        ContactEmailTemplateID = templateID
                    };
                    ContactEmailHistoryBO.Instance.Insert(history);

                    // Đóng luồng sau khi sử dụng
                    wordDocumentStream.Dispose();
                }

                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Gửi email thành công" });
            }
            catch (Exception ex)
            {
                pt.RollBack();
                return Json(new { code = 1, msg = ex.Message });
            }
            finally
            {
                pt.CloseConnection();
            }
        }
        private void SendEmail(string recipient, string subject, string body, MemoryStream attachmentStream = null)
        {
            try
            {
                // Cấu hình SMTP
                string smtpHost = PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindByAttribute("KeyName", "MAIL_SERVERNAME")).FirstOrDefault().KeyValue;
                int smtpPort = int.Parse(PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindByAttribute("KeyName", "MAIL_SERVERPORT")).FirstOrDefault().KeyValue);
                string smtpUsername = PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindByAttribute("KeyName", "MAIL_FROM")).FirstOrDefault().KeyValue;
                string smtpPassword = PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindByAttribute("KeyName", "MAIL_PASSWORD")).FirstOrDefault().KeyValue;
                bool enableSsl = true;

                // Tạo và cấu hình email
                MailMessage mail = new MailMessage
                {
                    From = new MailAddress(smtpUsername, PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindByAttribute("KeyName", "MAIL_FROMNAME")).FirstOrDefault().KeyValue),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mail.To.Add(recipient);

                // Đính kèm tệp Word nếu có
                if (attachmentStream != null)
                {
                    attachmentStream.Position = 0; // Đặt lại vị trí của luồng
                    mail.Attachments.Add(new Attachment(attachmentStream, "Reservation_Confirmation.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"));
                }

                // Cấu hình SMTP client
                using (SmtpClient smtpClient = new SmtpClient(smtpHost, smtpPort))
                {
                    smtpClient.EnableSsl = enableSsl;
                    smtpClient.Credentials = new System.Net.NetworkCredential(smtpUsername, smtpPassword);
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

                    // Gửi email
                    smtpClient.Send(mail);
                }
            }
            catch (SmtpException ex)
            {
                throw new Exception($"Gửi email đến {recipient} thất bại: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xử lý email cho {recipient}: {ex.Message}");
            }
        }

        private MemoryStream GenerateReservationWordDocument(ProfileModel profile, ContactEmailTemplateModel contactEmail)
        {
            MemoryStream memoryStream = new MemoryStream();
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document, true))
            {
                // Tạo cấu trúc tài liệu Word cơ bản
                MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = mainPart.Document.AppendChild(new Body());

                // Thêm tiêu đề
                Paragraph title = body.AppendChild(new Paragraph());
                Run titleRun = title.AppendChild(new Run());
                titleRun.AppendChild(new Text("XÁC NHẬN ĐẶT PHÒNG"));
                titleRun.RunProperties = new RunProperties(new Bold(), new FontSize() { Val = "28" });

                // Thêm thông tin công ty và liên hệ
                body.AppendChild(new Paragraph(new Run(new Text($"Công ty: abc"))));
                body.AppendChild(new Paragraph(new Run(new Text($"Người liên hệ: Vu Phat Dat"))));
                body.AppendChild(new Paragraph(new Run(new Text($"Ngày xác nhận: {DateTime.Now.ToString("dd.MM.yyyy")}"))));
                body.AppendChild(new Paragraph(new Run(new Text($"Số điện thoại: 0379516359"))));
                body.AppendChild(new Paragraph(new Run(new Text($"Email: {profile.Email}"))));

                // Thêm lời chào
                body.AppendChild(new Paragraph(new Run(new Text("Kính gửi Quý khách,"))));
                body.AppendChild(new Paragraph(new Run(new Text("Cảm ơn quý khách đã tin tưởng và lựa chọn sử dụng dịch vụ tại Khách sạn Kovie Hải Phòng. Khách sạn xác nhận thông tin đặt phòng của quý khách chi tiết như sau:"))));

                // Thêm bảng thông tin đặt phòng
                Table table = body.AppendChild(new Table());
                TableProperties tblProps = new TableProperties(
                    new TableBorders(
                        new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new InsideHorizontalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new InsideVerticalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 }
                    )
                );
                table.AppendChild(tblProps);

                // Tiêu đề bảng
                TableRow headerRow = table.AppendChild(new TableRow());
                headerRow.Append(
                    new TableCell(new Paragraph(new Run(new Text("Số xác nhận")))),
                    new TableCell(new Paragraph(new Run(new Text("Tên khách")))),
                    new TableCell(new Paragraph(new Run(new Text("Hạng phòng")))),
                    new TableCell(new Paragraph(new Run(new Text("Chặng ở")))),
                    new TableCell(new Paragraph(new Run(new Text("Giá phòng/đêm"))))
                );

                // Dữ liệu đặt phòng
                TableRow dataRow = table.AppendChild(new TableRow());
                dataRow.Append(
                    new TableCell(new Paragraph(new Run(new Text("31231321")))),
                    new TableCell(new Paragraph(new Run(new Text("dadư")))),
                    new TableCell(new Paragraph(new Run(new Text("dăđư")))),
                    new TableCell(new Paragraph(new Run(new Text("dădư")))),
                    new TableCell(new Paragraph(new Run(new Text("dađư"))))
                );

                // Thêm phần lưu ý và quyền lợi
                body.AppendChild(new Paragraph(new Run(new Text("LƯU Ý:"))));
                body.AppendChild(new Paragraph(new Run(new Text("• Giá trên được tính theo tiền Việt Nam Đồng và áp dụng cho 01 người/phòng/đêm. Giá trên đã bao gồm 10% Thuế GTGT và 5% phí dịch vụ."))));

                body.AppendChild(new Paragraph(new Run(new Text("QUYỀN LỢI:"))));
                body.AppendChild(new Paragraph(new Run(new Text("• Bữa sáng hàng ngày"))));
                body.AppendChild(new Paragraph(new Run(new Text("• Miễn phí 02 chai nước lọc/người/đêm"))));
                body.AppendChild(new Paragraph(new Run(new Text("• Miễn phí 05 đồ giặt là/người/ngày (không bao gồm đồ giặt nhanh, giặt khô, đồ nặng và không cộng dồn số đồ giặt.)"))));
                body.AppendChild(new Paragraph(new Run(new Text("• Trà và cà phê trong phòng"))));
                body.AppendChild(new Paragraph(new Run(new Text("• Miễn phí sử dụng các tiện ích bể bơi, phòng tập Gym, xông hơi, phòng tập Golf"))));
                body.AppendChild(new Paragraph(new Run(new Text("• Miễn phí xe đưa đón Kovie ↔ KCN Tràng Duệ theo lịch trình xe cố định của khách sạn"))));
                body.AppendChild(new Paragraph(new Run(new Text("• Internet tốc độ cao"))));
                body.AppendChild(new Paragraph(new Run(new Text("• Dịch vụ dọn phòng hàng ngày"))));

                // Thêm các phần khác (Dịch vụ xe, Thanh toán, Thông tin chung, Chính sách hủy phòng, v.v.)
                body.AppendChild(new Paragraph(new Run(new Text("LƯU Ý ĐẶT CHỖ:"))));
                body.AppendChild(new Paragraph(new Run(new Text($"Dịch vụ xe: Đón đă, Tiễn đă"))));
                body.AppendChild(new Paragraph(new Run(new Text("ĐIỀU KHOẢN THANH TOÁN:"))));
                body.AppendChild(new Paragraph(new Run(new Text("KHÁCH LƯU TRÚ vui lòng thanh toán toàn bộ tiền phòng khi nhận phòng và chi phí phát sinh (nếu có) khi trả phòng."))));
                body.AppendChild(new Paragraph(new Run(new Text("THÔNG TIN TÀI KHOẢN:"))));
                body.AppendChild(new Paragraph(new Run(new Text($"Tên tài khoản: đă"))));
                body.AppendChild(new Paragraph(new Run(new Text($"Số tài khoản thụ hưởng: đă"))));
                body.AppendChild(new Paragraph(new Run(new Text($"Ngân hàng: đă"))));

                // Thêm chính sách nhận/trả phòng
                body.AppendChild(new Paragraph(new Run(new Text("THÔNG TIN CHUNG:"))));
                body.AppendChild(new Paragraph(new Run(new Text("1. Chính sách nhận/trả phòng"))));
                body.AppendChild(new Paragraph(new Run(new Text("• Giờ nhận phòng tiêu chuẩn: 14:00; Giờ trả phòng tiêu chuẩn: 12:00 trưa."))));
                body.AppendChild(new Paragraph(new Run(new Text("• Nhận phòng sớm trước 9:00: phụ thu 01 đêm tiền phòng; Nhận phòng sớm từ 9:00 – 13:59: phụ thu 50% tiền phòng 01 đêm."))));
                body.AppendChild(new Paragraph(new Run(new Text("• Trả phòng muộn trước 18:00: phụ thu 50% tiền phòng 01 đêm; Trả phòng muộn sau 18:00: phụ thu tiền phòng 01 đêm."))));

                // Thêm chính sách hủy phòng
                body.AppendChild(new Paragraph(new Run(new Text("CHÍNH SÁCH HỦY PHÒNG:"))));
                body.AppendChild(new Paragraph(new Run(new Text("• Dưới 03 ngày trước ngày nhận phòng đã xác nhận hoặc khách không đến mà không thông báo trước: 100% tiền phòng đêm đầu tiên theo số lượng phòng đặt."))));
                body.AppendChild(new Paragraph(new Run(new Text("• 03 ngày trước ngày nhận phòng đã xác nhận: 50% tiền phòng đêm đầu tiên theo số lượng phòng đặt."))));
                body.AppendChild(new Paragraph(new Run(new Text("• 07 ngày trước ngày nhận phòng đã xác nhận: Không áp dụng phí phạt."))));

                // Thêm chính sách trả phòng sớm
                body.AppendChild(new Paragraph(new Run(new Text("CHÍNH SÁCH TRẢ PHÒNG SỚM:"))));
                body.AppendChild(new Paragraph(new Run(new Text("• 30 ngày trước ngày trả phòng: Không áp dụng phí phạt."))));
                body.AppendChild(new Paragraph(new Run(new Text("• 15 – 29 ngày trước ngày trả phòng: 10% giá phòng những đêm còn lại."))));
                body.AppendChild(new Paragraph(new Run(new Text("• 01 – 14 ngày trước ngày trả phòng: 20% giá phòng những đêm còn lại."))));

                // Thêm lời cảm ơn
                body.AppendChild(new Paragraph(new Run(new Text("Chân thành cảm ơn và rất mong được đón tiếp quý khách hàng tại Khách sạn Kovie."))));
                body.AppendChild(new Paragraph(new Run(new Text("Xác nhận bởi: Kovie Hotel"))));
            }
            return memoryStream;
        }
        #endregion

        #region DatVP __ SendEmail:  Get Profile by dob
        [HttpGet]
        public async Task<IActionResult> GetAllProfiles(DateTime fromDate, DateTime toDate)
        {
            try
            {
                // Lấy danh sách profile và lọc theo điều kiện
                var profiles = ProfileBO.GetListProfileByBOD(fromDate,toDate);



                // Trả về dữ liệu cùng với thông tin phân trang
                return Json(profiles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        #endregion

        #region DatVP __ SendEmail: Email Sending History
        [HttpGet]
        public async Task<IActionResult> GetHistorySendEmail(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var profile = PropertyUtils.ConvertToList<ContactEmailHistoryModel>(ContactEmailHistoryBO.Instance.FindAll()).Where(x => fromDate <= x.SendDate && toDate >= x.SendDate ).ToList();
                return Json(profile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        #endregion
        private void SendEmail2(string recipient, string subject, string body)
        {
            try
            {
                // Configure SMTP settings (replace with your SMTP server details)
                string smtpHost = PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindByAttribute("KeyName", "MAIL_SERVERNAME")).FirstOrDefault().KeyValue;
                int smtpPort = int.Parse(PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindByAttribute("KeyName", "MAIL_SERVERPORT")).FirstOrDefault().KeyValue);
                string smtpUsername = PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindByAttribute("KeyName", "MAIL_FROM")).FirstOrDefault().KeyValue;
                string smtpPassword = PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindByAttribute("KeyName", "MAIL_PASSWORD")).FirstOrDefault().KeyValue;
                bool enableSsl = true;

                // Create and configure the email message
                MailMessage mail = new MailMessage
                {
                    From = new MailAddress(smtpUsername, PropertyUtils.ConvertToList<ConfigSystemModel>(ConfigSystemBO.Instance.FindByAttribute("KeyName", "MAIL_FROMNAME")).FirstOrDefault().KeyValue),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true // Since htmlContent contains HTML
                };
                mail.To.Add(recipient);

                // Configure SMTP client
                using (SmtpClient smtpClient = new SmtpClient(smtpHost, smtpPort))
                {
                    smtpClient.EnableSsl = enableSsl;
                    smtpClient.Credentials = new System.Net.NetworkCredential(smtpUsername, smtpPassword);
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

                    // Send the email
                    smtpClient.Send(mail);
                }
            }
            catch (SmtpException ex)
            {
                throw new Exception($"Failed to send email to {recipient}: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing email for {recipient}: {ex.Message}");
            }
        }
        #region DatVP __ SendEmailBirthday
        [HttpPost]
        public ActionResult SendEmailGuestBirthday(int templateID, List<int> profileIDs)
        {
            ProcessTransactions pt = new ProcessTransactions();
            try
            {
                pt.OpenConnection();
                pt.BeginTransaction();

                // Fetch email template
                ContactEmailTemplateModel contactEmail = (ContactEmailTemplateModel)ContactEmailTemplateBO.Instance.FindByPrimaryKey(templateID);
                if (contactEmail == null)
                {
                    return Json(new { code = 1, msg = "Can not find temokate" });
                }
                string subject = contactEmail.Name;
                string htmlContent = contactEmail.Content;

                // Process each profile
                foreach (int profileId in profileIDs)
                {
                    ProfileModel profile = (ProfileModel)ProfileBO.Instance.FindByPrimaryKey(profileId);
                    if (profile == null)
                    {
                        return Json(new { code = 1, msg = $"Profile with ID {profileId} not found." });

                    }

                    // Ensure profile has an Email property
                    if (string.IsNullOrEmpty(profile.Email))
                    {
                        continue;

                    }

                    // Replace placeholders like ##Account## with profile properties
                    string pattern = @"##(\w+)##";
                    string personalizedContent = Regex.Replace(htmlContent, pattern, match =>
                    {
                        string propertyName = match.Groups[1].Value;
                        PropertyInfo propertyInfo = profile.GetType().GetProperty(propertyName);
                        if (propertyInfo != null)
                        {
                            object value = propertyInfo.GetValue(profile);
                            return value?.ToString() ?? string.Empty;
                        }
                        return match.Value; // Keep original placeholder if property not found
                    });

                    // Send email to profile.Email with personalized content
                    SendEmail2(profile.Email, subject, personalizedContent);
                    ContactEmailHistoryModel history = new ContactEmailHistoryModel();
                    history.GroupID = 0;
                    history.Subject = contactEmail.Name;
                    history.Content = contactEmail.Content;
                    history.ContactRelateID = 0;
                    history.SendBy = 0;
                    history.SendDate = DateTime.Now;
                    history.SendOK = 1;
                    history.SendTo = profile.Email;
                    history.Reason = "";
                    history.SuccessSendDate = DateTime.Now;
                    history.ContactEmailTemplateID = templateID;
                    ContactEmailHistoryBO.Instance.Insert(history);
                }

                pt.CommitTransaction();
                return Json(new { code = 0, msg = "Emails processed successfully" });
            }
            catch (Exception ex)
            {
                pt.RollBack();
                return Json(new { code = 1, msg = ex.Message });
            }
            finally
            {
                pt.CloseConnection();
            }
        }
        #endregion
    }
}

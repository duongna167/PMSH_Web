using System;
using System.Collections.Generic;
using BaseBusiness.BO;

namespace FrontDesk.Services.Interfaces
{
    public interface INotificationService
    {
        DateTime BusinessDate();
        (int birthday, int message, int trace) Summary(int userId);
        List<BirthdayNotifRow> Birthdays();
        List<InternalMessageNotifRow> Messages(int userId);
        string LoginDepartmentName(int userId);
        int SaveMessage(string message, DateTime fromDate, DateTime toDate, int userId, string toDepartmentIds);
        void MarkReceived(int messageId, int userId);
        List<TraceNotifRow> OpenTraces(int userId);
        string ResolveTraces(int userId, string userName, List<int> selectedIds);
    }
}

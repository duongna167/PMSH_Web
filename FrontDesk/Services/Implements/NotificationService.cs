using System;
using System.Collections.Generic;
using BaseBusiness.BO;
using BaseBusiness.Model;
using FrontDesk.Services.Interfaces;

namespace FrontDesk.Services.Implements
{
    public class NotificationService : INotificationService
    {
        public DateTime BusinessDate() => FOInternalMessageBO.Instance.BusinessDate();

        public (int birthday, int message, int trace) Summary(int userId)
        {
            var biz = BusinessDate();
            return (
                FOInternalMessageBO.Instance.CountBirthdays(biz),
                FOInternalMessageBO.Instance.CountUnreadMessages(biz, userId),
                FOInternalMessageBO.Instance.CountOpenTraces(biz, userId)
            );
        }

        public List<BirthdayNotifRow> Birthdays()
            => FOInternalMessageBO.Instance.ListBirthdays(BusinessDate());

        public List<InternalMessageNotifRow> Messages(int userId)
            => FOInternalMessageBO.Instance.ListMessages(BusinessDate(), userId);

        public string LoginDepartmentName(int userId)
            => FOInternalMessageBO.Instance.GetUserDepartmentName(userId);

        public int SaveMessage(string message, DateTime fromDate, DateTime toDate, int userId, string toDepartmentIds)
            => FOInternalMessageBO.Instance.InsertMessage(message, fromDate, toDate, userId, toDepartmentIds);

        public void MarkReceived(int messageId, int userId)
            => FOInternalMessageBO.Instance.MarkReceived(messageId, userId);

        public List<TraceNotifRow> OpenTraces(int userId)
            => FOInternalMessageBO.Instance.ListOpenTraces(BusinessDate(), userId);

        public string ResolveTraces(int userId, string userName, List<int> selectedIds)
        {
            if (selectedIds == null || selectedIds.Count == 0)
                return "Please choose at least 1 traces";

            foreach (var id in selectedIds)
            {
                var traces = (ReservationTracesModel)ReservationTracesBO.Instance.FindByPrimaryKey(id);
                if (traces == null || traces.ID == 0)
                    return "Could not trace in trace list";
            }

            foreach (var id in selectedIds)
            {
                var traces = (ReservationTracesModel)ReservationTracesBO.Instance.FindByPrimaryKey(id);
                if (traces.Resolved == 1)
                    continue;
                traces.Resolved = 1;
                traces.ResolvedBy = userName;
                traces.UserUpdateID = userId;
                traces.UpdateDate = DateTime.Now;
                traces.ResolvedDate = DateTime.Now;
                ReservationTracesBO.Instance.Update(traces);
            }

            return null;
        }
    }
}

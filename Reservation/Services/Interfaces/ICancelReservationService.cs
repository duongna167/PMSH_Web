using BaseBusiness.Model;
using BaseBusiness.util;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Reservation.Dto.ReservationPackageDTO;
using static Reservation.Services.Implements.CancelReservationService;

namespace Reservation.Services.Interfaces
{
    public interface ICancelReservationService
    {
        public ProcessResult ProcessProStayCancel(ProcessTransactions pt, ReservationModel currentRsv, string selectedIdsRaw, int userId, string userName, string reasonCancellation, string description);
        public ProcessResult ProcessCheckedInReinstates(ProcessTransactions pt, ReservationModel rsv, int userId, string userName, bool changeRoomToDirty);
    }
}

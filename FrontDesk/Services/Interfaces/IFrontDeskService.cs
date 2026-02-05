using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseBusiness.Model;

namespace FrontDesk.Services.Interfaces
{

    public interface IFrontDeskService
    {


        public DataTable TelephoneBook(string categoryId, string categoryCode, string telephoneCode);
        public DataTable GetTelephoneBookByCategory(string categoryId, string searchTerm);
        public DataTable TelephoneSwitch(string roomNo, int foStatus);
        public DataTable DialingInformation(DateTime fromDate, DateTime toDate, string phoneNo, int view, string zone);
        public DataTable WakeUpCallFindRoom(string roomNoset, string reservationHolder, string zone, string confirmNo);
        public DataTable WakeUpCallSearch(DateTime currentDate, string searchforName, int isSpecial);
        public DataTable ViewWakeUpCall(string name, string group, string roomview, DateTime fromDateview, DateTime toDateview, string hour, string minute, int roomClass);
        public DataTable ViewWakeUpCallAccount(int roomID, int shareRoom);
        public DataTable GetRoomPFOS(int isVacant);
        int GetReservationCount(int roomId, bool isVacant);
        (string roomNo, string statusName) ExecuteUpdateStatus(RoomModel roomModel, bool isVacant, int userID);
    }
}

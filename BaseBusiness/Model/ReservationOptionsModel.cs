using BaseBusiness.bc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.Model
{
    public class ReservationOptionsModel : BaseModel
    {
        public int ID { get; set; }
        public int ReservationID { get; set; }
        public bool Accompany { get; set; }
        public bool AddOn { get; set; }
        public bool AgentCompany { get; set; }
        public bool Alerts { get; set; }
        public bool Billing { get; set; }
        public bool CallerInfo { get; set; }
        public bool Cancel { get; set; }
        public bool Changes { get; set; }
        public bool Confirmation { get; set; }
        public bool CreditCards { get; set; }
        public bool Del { get; set; }
        public bool DepositCancelation { get; set; }
        public bool FacilityScheduler { get; set; }
        public bool FixedCharges { get; set; }
        public bool History { get; set; }
        public bool HouseKeeping { get; set; }
        public bool Locator { get; set; }
        public bool Messages { get; set; }
        public bool PackageOption { get; set; }
        public bool Party { get; set; }
        public bool Privileges { get; set; }
        public bool Queue { get; set; }
        public bool RateInfo { get; set; }
        public bool RegisterCard { get; set; }
        public bool RoomMove { get; set; }
        public bool Routing { get; set; }
        public bool Shares { get; set; }
        public bool Traces { get; set; }
        public bool TrackIt { get; set; }
        public bool WaitList { get; set; }
        public bool WakeUpCall { get; set; }
        public bool ItemInv { get; set; }
        public bool GroupOptions { get; set; }
        public bool MoreFields { get; set; }
    }
}

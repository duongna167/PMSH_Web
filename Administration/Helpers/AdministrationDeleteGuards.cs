using System.Collections;
using BaseBusiness.BO;
using BaseBusiness.Model;

namespace Administration.Helpers
{
    /// <summary>Pre-delete reference checks for administration master data.</summary>
    public static class AdministrationDeleteGuards
    {
        private static int Count(ArrayList arr) => arr?.Count ?? 0;

        private static string Blocked(string masterLabel, string usedIn, int n)
        {
            if (n <= 0) return null;
            return $"Cannot delete this {masterLabel} because it is used in {usedIn} ({n} record(s)).";
        }

        #region Reservation / Profile shortcuts

        private static int ReservationCount(string attribute, object value) =>
            Count(ReservationBO.Instance.FindByAttribute(attribute, value?.ToString()));

        private static int ProfileCount(string attribute, object value) =>
            Count(ProfileBO.Instance.FindByAttribute(attribute, value?.ToString()));

        #endregion

        public static string GetDeleteMemberCategoryBlockReason(int id) =>
            Blocked("member category", "Member Type", Count(MemberTypeBO.Instance.FindByAttribute("MemberCategoryID", id)));

        public static string GetDeleteMemberTypeBlockReason(int id) =>
            Blocked("member type", "Profile Member Card", Count(ProfileMemberCardBO.Instance.FindByAttribute("MemberTypeID", id)));

        public static string GetDeletePersonInChargeGroupBlockReason(int id) =>
            Blocked("Person In Charge group", "Person In Charge", Count(PersonInChargeBO.Instance.FindByAttribute("PersonInChargeGroupID", id)));

        public static string GetDeletePersonInChargeZoneBlockReason(int id) =>
            Blocked("Person In Charge zone", "Person In Charge", Count(PersonInChargeBO.Instance.FindByAttribute("PersonInChargeZoneID", id)));

        public static string GetDeleteDepositRuleBlockReason(int id)
        {
            return Blocked("deposit rule", "Rate Code Detail", Count(RateCodeDetailBO.Instance.FindByAttribute("DepositRuleID", id)))
                ?? Blocked("deposit rule", "Deposit Rule Schedule", Count(DepositRuleScheduleBO.Instance.FindByAttribute("DepositRuleID", id)))
                ?? Blocked("deposit rule", "Deposit Request", Count(DepositRsqBO.Instance.FindByAttribute("DepositRuleID", id)));
        }

        public static string GetDeleteCancellationRuleBlockReason(int id)
        {
            return Blocked("cancellation rule", "Rate Code Detail", Count(RateCodeDetailBO.Instance.FindByAttribute("DepositCancellationRuleID", id)))
                ?? Blocked("cancellation rule", "Cancellation Rule Schedule", Count(CancellationRuleScheduleBO.Instance.FindByAttribute("CancellationRuleID", id)));
        }

        public static string GetDeleteCityBlockReason(int id) =>
            Blocked("city", "AR Account", Count(ARAccountReceivableBO.Instance.FindByAttribute("CityID", id)));

        public static string GetDeleteCountryBlockReason(int id)
        {
            return Blocked("country", "City", Count(CityBO.Instance.FindByAttribute("CountryID", id)))
                ?? Blocked("country", "Profile", ProfileCount("CountryID", id))
                ?? Blocked("country", "Pickup / Drop place", Count(PickupDropPlaceBO.Instance.FindByAttribute("CountryID", id)));
        }

        public static string GetDeleteLanguageBlockReason(int id) =>
            Blocked("language", "Profile", ProfileCount("LanguageID", id));

        public static string GetDeleteNationalityBlockReason(int id) =>
            Blocked("nationality", "Profile", ProfileCount("NationalityID", id));

        public static string GetDeleteTitleBlockReason(int id) =>
            Blocked("title", "Profile", ProfileCount("TitleID", id));

        public static string GetDeleteTerritoryBlockReason(int id) =>
            Blocked("territory", "Profile", ProfileCount("TerritoryID", id));

        public static string GetDeleteStateBlockReason(int id) =>
            Blocked("state / province", "Profile", ProfileCount("StateID", id));

        public static string GetDeleteVipBlockReason(int id)
        {
            return Blocked("VIP", "Reservation", ReservationCount("VipID", id))
                ?? Blocked("VIP", "Profile", ProfileCount("VIPID", id));
        }

        public static string GetDeleteMarketBlockReason(int id)
        {
            return Blocked("market", "Reservation", ReservationCount("MarketID", id))
                ?? Blocked("market", "Profile", ProfileCount("MarketID", id))
                ?? Blocked("market", "Reservation Rate", Count(ReservationRateBO.Instance.FindByAttribute("MarketID", id)));
        }

        public static string GetDeleteMarketTypeBlockReason(int id) =>
            Blocked("market type", "Market", Count(MarketBO.Instance.FindByAttribute("MarketTypeID", id)));

        public static string GetDeletePickupDropPlaceBlockReason(int id)
        {
            var place = PickupDropPlaceBO.Instance.FindByPrimaryKey(id) as PickupDropPlaceModel;
            if (place == null || string.IsNullOrEmpty(place.Code))
                return null;
            var code = place.Code;
            var pickup = ReservationCount("PickupStationCode", code);
            var drop = ReservationCount("DropOffStationCode", code);
            if (pickup > 0) return Blocked("pickup / drop place", "Reservation (pickup station)", pickup);
            if (drop > 0) return Blocked("pickup / drop place", "Reservation (drop-off station)", drop);
            return null;
        }

        public static string GetDeleteTransportTypeBlockReason(int id)
        {
            var tt = TransportTypeBO.Instance.FindByPrimaryKey(id) as TransportTypeModel;
            if (tt == null || string.IsNullOrEmpty(tt.Code))
                return null;
            var code = tt.Code;
            var pickup = ReservationCount("PickupTransportType", code);
            var drop = ReservationCount("DropOffTransportType", code);
            if (pickup > 0) return Blocked("transport type", "Reservation (pickup)", pickup);
            if (drop > 0) return Blocked("transport type", "Reservation (drop-off)", drop);
            return null;
        }

        public static string GetDeleteReservationTypeBlockReason(int id)
        {
            return Blocked("reservation type", "Reservation", ReservationCount("ReservationTypeID", id))
                ?? Blocked("reservation type", "Rate Code Detail", Count(RateCodeDetailBO.Instance.FindByAttribute("ReservationTypeID", id)));
        }

        public static string GetDeleteReasonBlockReason(int id)
        {
            return Blocked("reason", "Wait List", Count(WaitListBO.Instance.FindByAttribute("ReasonID", id)))
                ?? Blocked("reason", "Business Block", Count(BusinessBlockBO.Instance.FindByAttribute("ReasonID", id)));
        }

        public static string GetDeleteOriginBlockReason(int id) =>
            Blocked("origin", "Reservation", ReservationCount("OriginID", id));

        public static string GetDeleteSourceBlockReason(int id)
        {
            return Blocked("source", "Reservation", ReservationCount("SourceID", id))
                ?? Blocked("source", "Reservation Rate", Count(ReservationRateBO.Instance.FindByAttribute("SourceID", id)));
        }

        public static string GetDeleteAlertsSetupBlockReason(int id) =>
            Blocked("alert", "Reservation Alert", Count(ReservationAlertsBO.Instance.FindByAttribute("OriginAlertID", id)));

        public static string GetDeleteCommentTypeBlockReason(int id) =>
            Blocked("comment type", "Comment", Count(CommentBO.Instance.FindByAttribute("CommentTypeID", id)));

        public static string GetDeleteSeasonBlockReason(int id)
        {
            return Blocked("season", "Rate Code Detail", Count(RateCodeDetailBO.Instance.FindByAttribute("SeasonID", id)))
                ?? Blocked("season", "Package Detail", Count(PackageDetailBO.Instance.FindByAttribute("SeasonID", id)));
        }

        public static string GetDeleteZoneBlockReason(int id)
        {
            return Blocked("zone", "Room Type", Count(RoomTypeBO.Instance.FindByAttribute("ZoneID", id)))
                ?? Blocked("zone", "Room", Count(RoomBO.Instance.FindByAttribute("ZoneID", id)))
                ?? Blocked("zone", "Lost And Found", Count(lafLostAndFoundBO.Instance.FindByAttribute("ZoneID", id)));
        }

        public static string GetDeleteDepartmentBlockReason(int id)
        {
            return Blocked("department", "User", Count(UsersBO.Instance.FindByAttribute("DepartmentID", id)))
                ?? Blocked("department", "Item", Count(ItemBO.Instance.FindByAttribute("DepartmentID", id)))
                ?? Blocked("department", "Reservation Trace", Count(ReservationTracesBO.Instance.FindByAttribute("DepartmentID", id)))
                ?? Blocked("department", "Cashier", Count(CashierBO.Instance.FindByAttribute("DepartmentID", id)));
        }

        public static string GetDeleteOccupancyBlockReason(int id) =>
            Blocked("occupancy", "Room Type (default occupancy)", Count(RoomTypeBO.Instance.FindByAttribute("DefOccupancy", id)));

        public static string GetDeleteOwnerBlockReason(int id) =>
            Blocked("owner", "Profile", ProfileCount("OwnerID", id));

        public static string GetDeletePropertyTypeBlockReason(int id) =>
            Blocked("property type", "Property", Count(PropertyBO.Instance.FindByAttribute("PropertyTypeID", id)));

        public static string GetDeletePropertyBlockReason(int id) =>
            Blocked("property", "Property Permission", Count(PropertyPermissionBO.Instance.FindByAttribute("PropertyID", id)));

        public static string GetDeletePackageForecastGroupBlockReason(int id) =>
            Blocked("package forecast group", "Package", Count(PackageBO.Instance.FindByAttribute("ForecastGroupID", id)));

        public static string GetDeletePreferenceGroupBlockReason(int id) =>
            Blocked("preference group", "Preference", Count(PreferenceBO.Instance.FindByAttribute("PreferenceGroupID", id)));

        public static string GetDeleteGroupOwnerBlockReason(int id) =>
            Blocked("group owner", "Group And Owner", Count(GroupAndOwnerBO.Instance.FindByAttribute("GroupOwnerID", id)));

        public static string GetDeleteRoomOwnerProfileBlockReason(int id) =>
            Blocked("room owner profile", "Group And Owner", Count(GroupAndOwnerBO.Instance.FindByAttribute("RoomOwnerID", id)));

        public static string GetDeletePriorityBlockReason(int id) =>
            Blocked("priority", "Wait List", Count(WaitListBO.Instance.FindByAttribute("PriorityID", id)));

        public static string GetDeletePromotionBlockReason(int id) =>
            Blocked("promotion", "Rate Code Detail", Count(RateCodeDetailBO.Instance.FindByAttribute("PromotionID", id)));
    }
}

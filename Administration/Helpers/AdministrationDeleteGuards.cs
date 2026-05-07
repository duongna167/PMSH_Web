using System.Collections.Generic;
using System.Collections;
using System.Linq;
using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;

namespace Administration.Helpers
{
    /// <summary>Pre-delete reference checks for administration master data.</summary>
    public static class AdministrationDeleteGuards
    {
        private static int Count(ArrayList arr) => arr?.Count ?? 0;

        /// <summary>Id &lt;= 0 makes FindByAttribute match ZoneID/DepartmentID = 0 etc. and falsely block delete.</summary>
        private static string BlockRefCheckIfInvalidId(int id) =>
            id <= 0
                ? "Cannot delete this record because the selection is invalid. Please refresh the list and select a row again."
                : null;

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
            BlockRefCheckIfInvalidId(id)
            ?? Blocked("member category", "Member Type", Count(MemberTypeBO.Instance.FindByAttribute("MemberCategoryID", id)));

        public static string GetDeleteMemberTypeBlockReason(int id) =>
            BlockRefCheckIfInvalidId(id)
            ?? Blocked("member type", "Profile Member Card", Count(ProfileMemberCardBO.Instance.FindByAttribute("MemberTypeID", id)));

        public static string GetDeletePersonInChargeGroupBlockReason(int id) =>
            BlockRefCheckIfInvalidId(id)
            ?? Blocked("Person In Charge group", "Person In Charge", Count(PersonInChargeBO.Instance.FindByAttribute("PersonInChargeGroupID", id)));

        public static string GetDeletePersonInChargeZoneBlockReason(int id) =>
            BlockRefCheckIfInvalidId(id)
            ?? Blocked("Person In Charge zone", "Person In Charge", Count(PersonInChargeBO.Instance.FindByAttribute("PersonInChargeZoneID", id)));

        public static string GetDeleteDepositRuleBlockReason(int id)
        {
            return BlockRefCheckIfInvalidId(id)
                ?? Blocked("deposit rule", "Rate Code Detail", Count(RateCodeDetailBO.Instance.FindByAttribute("DepositRuleID", id)))
                ?? Blocked("deposit rule", "Deposit Rule Schedule", Count(DepositRuleScheduleBO.Instance.FindByAttribute("DepositRuleID", id)))
                ?? Blocked("deposit rule", "Deposit Request", Count(DepositRsqBO.Instance.FindByAttribute("DepositRuleID", id)));
        }

        public static string GetDeleteCancellationRuleBlockReason(int id)
        {
            return BlockRefCheckIfInvalidId(id)
                ?? Blocked("cancellation rule", "Rate Code Detail", Count(RateCodeDetailBO.Instance.FindByAttribute("DepositCancellationRuleID", id)))
                ?? Blocked("cancellation rule", "Cancellation Rule Schedule", Count(CancellationRuleScheduleBO.Instance.FindByAttribute("CancellationRuleID", id)));
        }

        public static string GetDeleteCityBlockReason(int id) =>
            BlockRefCheckIfInvalidId(id)
            ?? Blocked("city", "AR Account", Count(ARAccountReceivableBO.Instance.FindByAttribute("CityID", id)));

        public static string GetDeleteCountryBlockReason(int id)
        {
            return BlockRefCheckIfInvalidId(id)
                ?? Blocked("country", "City", Count(CityBO.Instance.FindByAttribute("CountryID", id)))
                ?? Blocked("country", "Profile", ProfileCount("CountryID", id))
                ?? Blocked("country", "Pickup / Drop place", Count(PickupDropPlaceBO.Instance.FindByAttribute("CountryID", id)));
        }

        public static string GetDeleteLanguageBlockReason(int id) =>
            BlockRefCheckIfInvalidId(id)
            ?? Blocked("language", "Profile", ProfileCount("LanguageID", id));

        public static string GetDeleteNationalityBlockReason(int id) =>
            BlockRefCheckIfInvalidId(id)
            ?? Blocked("nationality", "Profile", ProfileCount("NationalityID", id));

        public static string GetDeleteTitleBlockReason(int id) =>
            BlockRefCheckIfInvalidId(id)
            ?? Blocked("title", "Profile", ProfileCount("TitleID", id));

        public static string GetDeleteTerritoryBlockReason(int id) =>
            BlockRefCheckIfInvalidId(id)
            ?? Blocked("territory", "Profile", ProfileCount("TerritoryID", id));

        public static string GetDeleteStateBlockReason(int id) =>
            BlockRefCheckIfInvalidId(id)
            ?? Blocked("state / province", "Profile", ProfileCount("StateID", id));

        public static string GetDeleteVipBlockReason(int id)
        {
            return BlockRefCheckIfInvalidId(id)
                ?? Blocked("VIP", "Reservation", ReservationCount("VipID", id))
                ?? Blocked("VIP", "Profile", ProfileCount("VIPID", id));
        }

        public static string GetDeleteMarketBlockReason(int id)
        {
            return BlockRefCheckIfInvalidId(id)
                ?? Blocked("market", "Reservation", ReservationCount("MarketID", id))
                ?? Blocked("market", "Profile", ProfileCount("MarketID", id))
                ?? Blocked("market", "Reservation Rate", Count(ReservationRateBO.Instance.FindByAttribute("MarketID", id)));
        }

        public static string GetDeleteMarketTypeBlockReason(int id) =>
            BlockRefCheckIfInvalidId(id)
            ?? Blocked("market type", "Market", Count(MarketBO.Instance.FindByAttribute("MarketTypeID", id)));

        public static string GetDeletePickupDropPlaceBlockReason(int id)
        {
            var badId = BlockRefCheckIfInvalidId(id);
            if (badId != null) return badId;
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
            var badId = BlockRefCheckIfInvalidId(id);
            if (badId != null) return badId;
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
            return BlockRefCheckIfInvalidId(id)
                ?? Blocked("reservation type", "Reservation", ReservationCount("ReservationTypeID", id))
                ?? Blocked("reservation type", "Rate Code Detail", Count(RateCodeDetailBO.Instance.FindByAttribute("ReservationTypeID", id)));
        }

        public static string GetDeleteReasonBlockReason(int id)
        {
            return BlockRefCheckIfInvalidId(id)
                ?? Blocked("reason", "Wait List", Count(WaitListBO.Instance.FindByAttribute("ReasonID", id)))
                ?? Blocked("reason", "Business Block", Count(BusinessBlockBO.Instance.FindByAttribute("ReasonID", id)));
        }

        public static string GetDeleteOriginBlockReason(int id) =>
            BlockRefCheckIfInvalidId(id)
            ?? Blocked("origin", "Reservation", ReservationCount("OriginID", id));

        public static string GetDeleteSourceBlockReason(int id)
        {
            return BlockRefCheckIfInvalidId(id)
                ?? Blocked("source", "Reservation", ReservationCount("SourceID", id))
                ?? Blocked("source", "Reservation Rate", Count(ReservationRateBO.Instance.FindByAttribute("SourceID", id)));
        }

        public static string GetDeleteAlertsSetupBlockReason(int id) =>
            BlockRefCheckIfInvalidId(id)
            ?? Blocked("alert", "Reservation Alert", Count(ReservationAlertsBO.Instance.FindByAttribute("OriginAlertID", id)));

        public static string GetDeleteCommentTypeBlockReason(int id) =>
            BlockRefCheckIfInvalidId(id)
            ?? Blocked("comment type", "Comment", Count(CommentBO.Instance.FindByAttribute("CommentTypeID", id)));

        public static string GetDeleteSeasonBlockReason(int id)
        {
            return BlockRefCheckIfInvalidId(id)
                ?? Blocked("season", "Rate Code Detail", Count(RateCodeDetailBO.Instance.FindByAttribute("SeasonID", id)))
                ?? Blocked("season", "Package Detail", Count(PackageDetailBO.Instance.FindByAttribute("SeasonID", id)));
        }

        public static string GetDeleteZoneBlockReason(int id)
        {
            var badId = BlockRefCheckIfInvalidId(id);
            if (badId != null) return badId;

            // IMPORTANT:
            // Some BO.FindByAttribute implementations behave like string match and can falsely "find" references.
            // Use exact numeric comparisons on full lists to avoid blocking delete right after create.
            var roomTypes = PropertyUtils.ConvertToList<RoomTypeModel>(RoomTypeBO.Instance.FindAll()) ?? new List<RoomTypeModel>();
            var rooms = PropertyUtils.ConvertToList<RoomModel>(RoomBO.Instance.FindAll()) ?? new List<RoomModel>();
            var lafs = PropertyUtils.ConvertToList<lafLostAndFoundModel>(lafLostAndFoundBO.Instance.FindAll()) ?? new List<lafLostAndFoundModel>();

            var roomTypeCount = roomTypes.Count(x => x.ZoneID == id);
            if (roomTypeCount > 0) return Blocked("zone", "Room Type", roomTypeCount);

            var roomCount = rooms.Count(x => x.ZoneID == id);
            if (roomCount > 0) return Blocked("zone", "Room", roomCount);

            var lafCount = lafs.Count(x => x.ZoneID == id);
            if (lafCount > 0) return Blocked("zone", "Lost And Found", lafCount);

            return null;
        }

        public static string GetDeleteDepartmentBlockReason(int id)
        {
            return BlockRefCheckIfInvalidId(id)
                ?? Blocked("department", "User", Count(UsersBO.Instance.FindByAttribute("DepartmentID", id)))
                ?? Blocked("department", "Item", Count(ItemBO.Instance.FindByAttribute("DepartmentID", id)))
                ?? Blocked("department", "Reservation Trace", Count(ReservationTracesBO.Instance.FindByAttribute("DepartmentID", id)))
                ?? Blocked("department", "Cashier", Count(CashierBO.Instance.FindByAttribute("DepartmentID", id)));
        }

        public static string GetDeleteOccupancyBlockReason(int id) =>
            BlockRefCheckIfInvalidId(id)
            ?? Blocked("occupancy", "Room Type (default occupancy)", Count(RoomTypeBO.Instance.FindByAttribute("DefOccupancy", id)));

        public static string GetDeleteOwnerBlockReason(int id) =>
            BlockRefCheckIfInvalidId(id)
            ?? Blocked("owner", "Profile", ProfileCount("OwnerID", id));

        public static string GetDeletePropertyTypeBlockReason(int id) =>
            BlockRefCheckIfInvalidId(id)
            ?? Blocked("property type", "Property", Count(PropertyBO.Instance.FindByAttribute("PropertyTypeID", id)));

        public static string GetDeletePropertyBlockReason(int id) =>
            BlockRefCheckIfInvalidId(id)
            ?? Blocked("property", "Property Permission", Count(PropertyPermissionBO.Instance.FindByAttribute("PropertyID", id)));

        public static string GetDeletePackageForecastGroupBlockReason(int id) =>
            BlockRefCheckIfInvalidId(id)
            ?? Blocked("package forecast group", "Package", Count(PackageBO.Instance.FindByAttribute("ForecastGroupID", id)));

        public static string GetDeletePreferenceGroupBlockReason(int id) =>
            BlockRefCheckIfInvalidId(id)
            ?? Blocked("preference group", "Preference", Count(PreferenceBO.Instance.FindByAttribute("PreferenceGroupID", id)));

        public static string GetDeleteGroupOwnerBlockReason(int id) =>
            BlockRefCheckIfInvalidId(id)
            ?? Blocked("group owner", "Group And Owner", Count(GroupAndOwnerBO.Instance.FindByAttribute("GroupOwnerID", id)));

        public static string GetDeleteRoomOwnerProfileBlockReason(int id) =>
            BlockRefCheckIfInvalidId(id)
            ?? Blocked("room owner profile", "Group And Owner", Count(GroupAndOwnerBO.Instance.FindByAttribute("RoomOwnerID", id)));

        public static string GetDeletePriorityBlockReason(int id) =>
            BlockRefCheckIfInvalidId(id)
            ?? Blocked("priority", "Wait List", Count(WaitListBO.Instance.FindByAttribute("PriorityID", id)));

        public static string GetDeletePromotionBlockReason(int id) =>
            BlockRefCheckIfInvalidId(id)
            ?? Blocked("promotion", "Rate Code Detail", Count(RateCodeDetailBO.Instance.FindByAttribute("PromotionID", id)));
    }
}

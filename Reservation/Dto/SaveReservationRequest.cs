using Microsoft.AspNetCore.Mvc;

namespace Reservation.Dto
{
    /// <summary>
    /// Form fields posted from New Reservation save (multipart/form-data).
    /// Property names follow form keys; use <see cref="PerrsonInCharge"/> for the typo key <c>perrsonInCharge</c>.
    /// </summary>
    public class SaveReservationRequest
    {
        public string? ReservationId { get; set; }
        public string? ProfileAgentId { get; set; }
        public string? AgentName { get; set; }
        public string? ProfileCompanyId { get; set; }
        public string? CompanyName { get; set; }
        public string? GroupCode { get; set; }
        public string? ProfileContactId { get; set; }
        public string? ContactName { get; set; }
        public string? ContactPhone { get; set; }
        public string? ProfileIndividualId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Title { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? MemberType { get; set; }
        public string? MemberNo { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? IdentityCard { get; set; }
        public string? DateOfBirth { get; set; }
        public string? Nationality { get; set; }
        public string? Arrival { get; set; }
        public string? Departure { get; set; }
        public string? NoOfAdult { get; set; }
        public string? NoOfChild { get; set; }
        public string? NoOfChild1 { get; set; }
        public string? NoOfChild2 { get; set; }
        public string? NoOfNight { get; set; }
        public string? NoOfRoom { get; set; }
        public string? RoomTypeId { get; set; }
        public string? VipId { get; set; }
        public string? RtcId { get; set; }
        public string? RoomId { get; set; }
        public string? RoomNo { get; set; }
        public string? Eta { get; set; }
        public string? Etd { get; set; }
        public string? ReservationType { get; set; }
        public string? ReservationTypeCode { get; set; }
        public string? MarketId { get; set; }
        public string? MarketCode { get; set; }
        public string? SourceId { get; set; }
        public string? SourceCode { get; set; }
        public string? BookerId { get; set; }
        public string? BookerName { get; set; }
        public string? DiscountAmount { get; set; }
        public string? DiscountRate { get; set; }
        public string? DiscountReason { get; set; }
        public string? Comment { get; set; }
        public string? PickedId { get; set; }
        public string? PickUpTransportType { get; set; }
        public string? PickUpStationCode { get; set; }
        public string? PickUpCarrierCode { get; set; }
        public string? PickUpTime { get; set; }
        public string? PickUpDate { get; set; }
        public string? PickUpTransportNo { get; set; }
        public string? PickUpDescription { get; set; }
        public string? DropOffId { get; set; }
        public string? DropOffCarrierCode { get; set; }
        public string? DropOffTransportType { get; set; }
        public string? DropOffStationCode { get; set; }
        public string? DropOffTime { get; set; }
        public string? DropOffTransportNo { get; set; }
        public string? DropOffDate { get; set; }
        public string? DropOffDescription { get; set; }
        public string? PackageId { get; set; }
        public string? Passport { get; set; }
        public string? Packages { get; set; }
        public string? RateCodeId { get; set; }
        public string? RateCode { get; set; }
        public string? RateAmount { get; set; }
        public string? RateAfter { get; set; }
        public string? Color { get; set; }
        public string? ItemInventory { get; set; }
        public string? Specials { get; set; }
        public string? AllotmentId { get; set; }
        public string? AllotmentCode { get; set; }
        public string? RoomNight { get; set; }
        public string? UserName { get; set; }
        public string? UserId { get; set; }
        public string? WalkIn { get; set; }
        public string? PrintRate { get; set; }
        public string? NoPost { get; set; }

        [FromForm(Name = "perrsonInCharge")]
        public string? PerrsonInCharge { get; set; }
    }
}

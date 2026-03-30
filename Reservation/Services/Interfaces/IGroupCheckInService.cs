using static Reservation.Dto.CheckInGroupDTO;

namespace Reservation.Services.Interfaces
{
    public interface IGroupCheckInService
    {
        CheckInResult CheckIn(CheckInRequest request);

        CheckInResult CheckInByProfileGroup(CheckInRequest request);

        CheckInResult CheckInByConfirmation(CheckInRequest request);
    }
}

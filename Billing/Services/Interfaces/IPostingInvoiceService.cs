using BaseBusiness.Contants;
using Billing.Dto;

namespace Billing.Services.Interfaces
{
    public interface IPostingInvoiceService
    {
        ApiResponse PostInvoiceFromRequest(PostingInvoiceRequestDto request);
    }
}

using System.Data;
using EInvoice.Dto;

namespace EInvoice.Services.Interfaces
{
    /// <summary>Quản lý nhà cung cấp hóa đơn điện tử: Provider, Field cấu hình, Option, và API (Operation) cùng Mapping/Response Rule.</summary>
    public interface IEInvoiceProviderService
    {
        DataTable SearchProviders();
        OpResult SaveProvider(ProviderSaveRequest request, string user);

        DataTable SearchFields(int providerId);
        OpResult SaveField(ProviderFieldSaveRequest request, string user);

        DataTable SearchOptions(int fieldId);
        OpResult SaveOption(ProviderFieldOptionSaveRequest request, string user);

        DataTable SearchOperations(int providerId);
        OpResult SaveOperation(OperationSaveRequest request, string user);

        DataTable SearchMappings(int operationId);
        OpResult SaveMapping(MappingSaveRequest request, string user);

        DataTable SearchRules(int operationId);
    }
}

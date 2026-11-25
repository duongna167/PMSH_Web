using System.Data;

namespace NightAudit.Services.Interfaces
{
    /// <summary>Map Transaction Code với mã tài khoản kế toán (bảng InterfaceToAcc).</summary>
    public interface IMapTransactionService
    {
        DataTable GetAll();

        /// <summary>Thêm một dòng map; trả về ID mới.</summary>
        int Add(Dictionary<string, string?> values);

        /// <summary>Cập nhật các cột được gửi lên (chỉ các cột trong danh sách cho phép) của một dòng.</summary>
        bool Update(int id, Dictionary<string, string?> values);

        bool Delete(int id);
    }
}

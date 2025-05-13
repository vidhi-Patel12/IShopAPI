using ECommerceAPI.Models;

namespace ECommerceAPI.Interface
{
    public interface ICoupan
    {
        Task<IEnumerable<Coupan>> GetCoupansAsync();
        Task<Coupan> GetCoupanByIdAsync(int id);
        Task<string> AddOrUpdateCoupanAsync(Coupan model);
        Task<bool> DeleteCoupanAsync(int id);
        Task<double?> ValidateCoupanAsync(string coupanCode);

    }
}

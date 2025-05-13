using ECommerceAPI.Models;

namespace ECommerceAPI.Interface
{
    public interface IProduct
    {
        Task<List<Products>> GetAllProductsAsync();
        Task<Products> GetProductByIdAsync(int productId, int? productsImageId = null);
        Task<int> CreateProductsAsync(List<Products> products, IFormFile largeImage, IFormFile mediumImage, IFormFile smallImage, string userId);

        Task<List<int>> UpdateProductsAsync(List<Products> products, IFormFile largeImage, IFormFile mediumImage, IFormFile smallImage, string userId);




        Task<List<string>> GetImagePathsAsync(int productsImageId);
        Task SoftDeleteProductImageAsync(int productsImageId);
        Task<int> GetActiveImageCountAsync(int productId);
        Task SoftDeleteProductAsync(int productId);

        Task<bool> DeleteProductImageAsync(int productId, int productsImageId, string rootPath);

    }
}

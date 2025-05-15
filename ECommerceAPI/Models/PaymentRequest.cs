namespace ECommerceAPI.Models
{
    public class PaymentRequest
    {
        public string OrderId { get; set; }  
        public string Nonce { get; set; } 
        public string PaymentMode { get; set; }
    }
}

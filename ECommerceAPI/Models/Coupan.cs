﻿using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.Models
{
    public class Coupan
    {
        public int CoupanId { get; set; }
        [Required]
        public string CoupanName { get; set; }
        public string CoupanType { get; set; }

        public string CoupanCode { get; set; }
        [Required]
        public double Discount { get; set; }
        public DateTime ExpiryDate { get; set; }
        public double ValidAmount { get; set; }
        public bool IsActive { get; set; }
    }
}

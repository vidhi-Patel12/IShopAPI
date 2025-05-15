﻿using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.Models
{
    public class Register
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IShopId { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "Last Name is required.")]
        public string LastName { get; set; }
        [Required]
        [Range(1000000000, 9999999999, ErrorMessage = "Mobile number must be 10 digits.")]
        public long Mobile { get; set; }

        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$",
        ErrorMessage = "Password must be at least 6 characters long and include at least one letter, one number, and one special character.")]
        public string Password { get; set; }
        [DataType(DataType.Date)]
        public DateOnly? Birthdate { get; set; }
        public int Role { get; set; }
        public bool IsActive { get; set; }
    }
}

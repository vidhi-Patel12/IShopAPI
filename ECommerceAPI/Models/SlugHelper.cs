﻿namespace ECommerceAPI.Models
{
    public class SlugHelper
    {
        public static string GenerateSlug(string title)
        {
            return title.ToLower()
                        .Replace(" ", "-")
                        .Replace("'", "")
                        .Replace("&", "and")
                        .Replace("/", "-")
                        .Replace(".", "")
                        .Replace(",", "")
                        .Replace(":", "")
                        .Replace(";", "")
                        .Replace("?", "")
                        .Replace("!", "")
                        .Replace("\"", "")
                        .Replace("#", "")
                        .Replace("$", "")
                        .Replace("%", "")
                        .Replace("*", "")
                        .Replace("@", "")
                        .Replace("+", "")
                        .Replace("=", "")
                        .Trim('-');
        }
    }
}

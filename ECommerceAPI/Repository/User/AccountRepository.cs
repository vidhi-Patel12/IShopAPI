using ECommerceAPI.Data;
using ECommerceAPI.Interface;
using ECommerceAPI.Models;
using System;

namespace ECommerceAPI.Repository.User
{
    public class AccountRepository : IAccount
    {
        private readonly ApplicationDbContext _context;

        public AccountRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool MobileExists(long mobile)
        {
            return _context.Register.Any(u => u.Mobile == mobile);
        }

        public void Add(Register model)
        {
            _context.Register.Add(model);
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        public Register GetUserByMobile(long mobile)
        {
            return _context.Register.FirstOrDefault(u => u.Mobile == mobile);
        }

        public void SaveOTP(Login loginEntry)
        {
            _context.Login.Add(loginEntry);
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }

        public Login GetLatestValidLoginEntry(int shopId)
        {
            return _context.Login
                .Where(l => l.IShopId == shopId && l.IsValid)
                .OrderByDescending(l => l.GeneratedAt)
                .FirstOrDefault();
        }

        public void InvalidateOTP(Login loginEntry)
        {
            if (loginEntry != null)
            {
                loginEntry.IsValid = false;
            }
        }

        public void ExpireOtp ()
        {
            var expiredOtps = _context.Login
                .Where(l => l.IsValid && DateTime.Now > l.GeneratedAt.AddMinutes(5))
                .ToList();

            foreach (var otp in expiredOtps)
            {
                otp.IsValid = false;
            }
        }

        public void LogUserLogout(int shopId)
        {
            // Optional: implement logout logging, e.g.
            // var log = new LogoutLog { IShopId = shopId, LogoutTime = DateTime.Now };
            // _context.LogoutLogs.Add(log);
        }

    }
}

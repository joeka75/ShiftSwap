using Microsoft.AspNetCore.Identity;
using ShiftSwap.Models;

namespace ShiftSwap.Data
{
    public static class DbInitializer
    {
        public static void Seed(AppDbContext db)
        {
            if (db.Companies.Any())
                return;

            var passwordHasher = new PasswordHasher<User>();

            var company = new Company
            {
                Name = "Demo Klinika Kft."
            };

            var location = new Location
            {
                Name = "Fő telephely",
                Address = "Budapest, Demo utca 1.",
                Company = company
            };

            var manager = new User
            {
                Company = company,
                Location = location,
                FullName = "Kiss Béla (Manager)",
                Email = "manager@demo.local",
                Role = UserRole.Manager,
                IsActive = true
            };
            manager.PasswordHash = passwordHasher.HashPassword(manager, "demo123");

            var worker1 = new User
            {
                Company = company,
                Location = location,
                FullName = "Nagy Anna (Worker 1)",
                Email = "anna@demo.local",
                Role = UserRole.Worker,
                IsActive = true
            };
            worker1.PasswordHash = passwordHasher.HashPassword(worker1, "demo123");

            var worker2 = new User
            {
                Company = company,
                Location = location,
                FullName = "Szabó Péter (Worker 2)",
                Email = "peter@demo.local",
                Role = UserRole.Worker,
                IsActive = true
            };
            worker2.PasswordHash = passwordHasher.HashPassword(worker2, "demo123");

            var tomorrow = DateTime.Today.AddDays(1);
            var dayAfter = DateTime.Today.AddDays(2);

            var shift1 = new Shift
            {
                Location = location,
                User = worker1,
                ShiftDate = tomorrow,
                StartDateTime = tomorrow.AddHours(8),
                EndDateTime = tomorrow.AddHours(16),
                Status = ShiftStatus.Assigned
            };

            var shift2 = new Shift
            {
                Location = location,
                User = worker1,
                ShiftDate = dayAfter,
                StartDateTime = dayAfter.AddHours(8),
                EndDateTime = dayAfter.AddHours(16),
                Status = ShiftStatus.Assigned
            };

            var shift3 = new Shift
            {
                Location = location,
                User = worker2,
                ShiftDate = dayAfter,
                StartDateTime = dayAfter.AddHours(16),
                EndDateTime = dayAfter.AddHours(24),
                Status = ShiftStatus.Assigned
            };

            db.Companies.Add(company);
            db.Locations.Add(location);
            db.Users.AddRange(manager, worker1, worker2);
            db.Shifts.AddRange(shift1, shift2, shift3);

            db.SaveChanges();
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;
//using Microsoft.AspNet.Identity;
using Microsoft.EntityFrameworkCore;
using WebStore.DAL.Context;
using WebStore.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;


namespace WebStore.Data
{ 
    public class WebStoreDBInitializer
    {
        private readonly WebStoreDB _db;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public WebStoreDBInitializer(WebStoreDB db, UserManager<User> userManager, RoleManager<Role> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public void Initialize() => InitializeAsync().Wait();

        public async Task InitializeAsync()
        {
            var db = _db.Database;

            //if (await db.EnsureDeletedAsync().ConfigureAwait(false)) //При удаленной БД содает новую
            //{
            //    if(!await db.EnsureCreatedAsync().ConfigureAwait(false))
            //        throw new InvalidOperationException("Не удалось создать БД");
            //}

            await db.MigrateAsync().ConfigureAwait(false);

            await InitializeIdentityAsync().ConfigureAwait(false);

            await InitializeProductsAsync().ConfigureAwait(false);
        }

        private async Task InitializeProductsAsync()
        {
            if (await _db.Products.AnyAsync() && await _db.Customers.AnyAsync()) //Инициализация товарами
                return;
            var db = _db.Database;

            using (var transaction = await db.BeginTransactionAsync().ConfigureAwait(false)) //Инициализация товарами
            {
                await _db.Sections.AddRangeAsync(TestData.Sections).ConfigureAwait(false);

                await db.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[Sections] ON");
                await _db.SaveChangesAsync().ConfigureAwait(false);
                await db.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[Sections] OFF");

                await transaction.CommitAsync().ConfigureAwait(false);
            }

            using (var transaction = await db.BeginTransactionAsync().ConfigureAwait(false))
            {
                await _db.Brands.AddRangeAsync(TestData.Brands).ConfigureAwait(false);

                await db.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[Brands] ON");
                await _db.SaveChangesAsync().ConfigureAwait(false);
                await db.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[Brands] OFF");

                await transaction.CommitAsync().ConfigureAwait(false);
            }

            using (var transaction = await db.BeginTransactionAsync().ConfigureAwait(false))
            {
                await _db.Products.AddRangeAsync(TestData.Products).ConfigureAwait(false);

                await db.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[Products] ON");
                await _db.SaveChangesAsync().ConfigureAwait(false);
                await db.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[Products] OFF");

                await transaction.CommitAsync().ConfigureAwait(false);
            }

            using (var transaction = await db.BeginTransactionAsync().ConfigureAwait(false))
            {
                await _db.Customers.AddRangeAsync(TestData.Customers).ConfigureAwait(false);

                await db.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[Customers] ON");
                await _db.SaveChangesAsync().ConfigureAwait(false);
                await db.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[Customers] OFF");

                await transaction.CommitAsync().ConfigureAwait(false);
            }
        }

        private async Task InitializeIdentityAsync()
        {
            if (!await _roleManager.RoleExistsAsync(Role.Administrator))
                await _roleManager.CreateAsync(new Role {Name = Role.Administrator});

            if (!await _roleManager.RoleExistsAsync(Role.User))
                await _roleManager.CreateAsync(new Role { Name = Role.User });

            if (await _userManager.FindByNameAsync(User.Administrator) is null)
            {
                var admin = new User
                {
                    UserName = User.Administrator,
                    //Email = "amin@server.com"
                };

                var create_result = await _userManager.CreateAsync(admin, User.AdminDefaultPassword);

                if (create_result.Succeeded)
                    await _userManager.AddToRoleAsync(admin, Role.Administrator);
                else
                {
                    var errors = create_result.Errors.Select(error => error.Description);
                    throw new InvalidOperationException(
                        $"Ошибка при создании пользователя - Администратора: { string.Join(", ", errors )}");
                }
            }

        }
    }
}

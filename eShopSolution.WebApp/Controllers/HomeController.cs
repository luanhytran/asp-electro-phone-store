﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using eShopSolution.ApiIntegration.Categories;
using eShopSolution.ApiIntegration.Products;
using eShopSolution.Utilities.Constants;
using eShopSolution.ViewModels.Catalog.Products;
using eShopSolution.WebApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;

namespace eShopSolution.WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductApiClient _productApiClient;
        private readonly ICategoryApiClient _categoryApiClient;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, 
            IProductApiClient productApiClient, ICategoryApiClient categoryApiClient, IConfiguration configuration)
        {
            _productApiClient = productApiClient;
            _categoryApiClient = categoryApiClient;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                string cookie = Request.Cookies["customerToken"];
                if (cookie != null)
                {
                    var userPrincipal = this.ValidateToken(cookie);

                    var authProperties = new AuthenticationProperties
                    {
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMonths(1),
                        IsPersistent = true,
                        IssuedUtc = DateTimeOffset.UtcNow.AddMonths(1),
                    };

                    HttpContext.Session.SetString(SystemConstants.AppSettings.DefaultLanguageId, _configuration[SystemConstants.AppSettings.DefaultLanguageId]);

                    HttpContext.Session.SetString(SystemConstants.AppSettings.Token, cookie);

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        userPrincipal,
                        authProperties);

                    return RedirectToAction("Index", "Home");
                }
            }
           
            var culture = CultureInfo.CurrentCulture.Name;

            var viewModel = new HomeViewModel
            {
                FeaturedProducts = await _productApiClient.GetFeaturedProducts(culture, SystemConstants.ProductSettings.NumberOfFeturedProducts),
                LatestProducts = await _productApiClient.GetLatestProducts(culture, SystemConstants.ProductSettings.NumberOfLatestProducts),
                //Categories = await _categoryApiClient.GetAll()
            };


            foreach (var item in viewModel.LatestProducts)
            {
                var category = await _categoryApiClient.GetById(item.Id);
                item.Category = category;
            }

            foreach (var item in viewModel.FeaturedProducts)
            {
                var category = await _categoryApiClient.GetById(item.Id);
                item.Category = category;
            }

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> ViewByCategory(string sortOrder, int cateId, int pageIndex = 1, int pageSize = 8)
        {
            var request = new GetManageProductPagingRequest()
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                CategoryId = cateId,
                SortOption = sortOrder
            };

            var data = await _productApiClient.GetPagings(request);

            List<string> sortOption = new List<string>()
            {
                "Tên A-Z",
                "Giá thấp đến cao",
                "Giá cao đến thấp"
            };

            ViewBag.SortOption = sortOption;
            ViewBag.CurrentSortOrder = sortOrder;

            foreach (var item in data.Items)
            {
                var category = await _categoryApiClient.GetById(item.CategoryId);
                item.Category = category;
            }

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> ViewBySearchProduct(string keyword, int? categoryId, int pageIndex = 1, int pageSize = 8)
        {
            var request = new GetManageProductPagingRequest()
            {
                Keyword = keyword,
                PageIndex = pageIndex,
                PageSize = pageSize,
                CategoryId = categoryId
            };

            var data = await _productApiClient.GetPagings(request);
            ViewBag.Keyword = keyword;

            foreach (var item in data.Items)
            {
                var category = await _categoryApiClient.GetById(item.CategoryId);
                item.Category = category;
            }

            if (TempData["result"] != null)
            {
                ViewBag.SuccessMsg = TempData["result"];
            }
            return View(data);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public IActionResult SetCultureCookie(string cltr, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(cltr)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
                );

            return LocalRedirect(returnUrl);
        }

        private ClaimsPrincipal ValidateToken(string jwtToken)
        {
            IdentityModelEventSource.ShowPII = true;

            SecurityToken validatedToken;
            TokenValidationParameters validationParameters = new TokenValidationParameters();

            validationParameters.ValidateLifetime = true;

            validationParameters.ValidAudience = _configuration["Tokens:Issuer"];
            validationParameters.ValidIssuer = _configuration["Tokens:Issuer"];
            validationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Tokens:Key"]));

            ClaimsPrincipal principal = new JwtSecurityTokenHandler().ValidateToken(jwtToken, validationParameters, out validatedToken);

            return principal;
        }
    }
}

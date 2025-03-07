﻿using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using eShopSolution.ApiIntegration.Orders;
using eShopSolution.ApiIntegration.Products;
using eShopSolution.ApiIntegration.Users;
using eShopSolution.ViewModels.System.Users;
using Microsoft.AspNetCore.Mvc;

namespace eShopSolution.WebApp.Controllers
{
    public class AccountController : BaseController
    {
        private readonly IUserApiClient _userApiClient;
        private readonly IOrderApiClient _orderApiClient;
        private readonly IProductApiClient _productApiClient;

        public AccountController(IUserApiClient userApiClient, IOrderApiClient orderApiClient, IProductApiClient productApiClient)
        {
            _userApiClient = userApiClient;
            _orderApiClient = orderApiClient;
            _productApiClient = productApiClient;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var claims = User.Claims.ToList();
            Guid id = new Guid(claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value);
            var orders = await _orderApiClient.GetOrderByUser(id.ToString());

            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid userid)
        {
            var result = await _userApiClient.GetById(userid);
            if (result.IsSuccessed)
            {
                var user = result.ResultObj;
                var updateRequest = new UserUpdateRequest()
                {
                    Email = user.Email,
                    Address = user.Address,
                    UserName = user.UserName,
                    Name = user.Name,
                    PhoneNumber = user.PhoneNumber,
                    Id = userid
                };
                return View(updateRequest);
            }
            return RedirectToAction("Error", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(UserUpdateRequest request)
        {
            if (!ModelState.IsValid)
                return View();

            var result = await _userApiClient.UpdateUser(request.Id, request);
            if (result.IsSuccessed)
            {
                var claims = User.Claims.ToList();
                Guid id = new Guid(claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value);
                var orders = await _orderApiClient.GetOrderByUser(id.ToString());

                TempData["UpdateAccountSuccess"] = "Cập nhật thông tin cá nhân thành công";
                return View("Index", orders);
            }

            ModelState.AddModelError("", result.Message);
            return View(request);
        }

        [HttpGet]
        public async Task<IActionResult> OrderDetail(string name, int orderId)
        {
            var order = await _orderApiClient.GetOrderById(orderId);
            order.Name = name;

            foreach (var item in order.OrderDetails)
            {
                var product = await _productApiClient.GetById(item.ProductId);
                item.Name = product.Name;
                item.Price = product.Price;
                item.ThumbnailImage = product.ThumbnailImage;
            }

            return View(order);
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View();

            var result = await _userApiClient.ChangePassword(model);

            if (result.IsSuccessed)
            {
                var claims = User.Claims.ToList();
                Guid id = new Guid(claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value);
                var orders = await _orderApiClient.GetOrderByUser(id.ToString());

                TempData["ChangePasswordSuccess"] = "Cập nhật mật khẩu thành công";
                return View("Index", orders);
            }

            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CancelOrderStatus(int orderId)
        {
            var result = await _orderApiClient.CancelOrderStatus(orderId);

            if (result)
            {
                TempData["CancelOrderSuccess"] = "Huỷ đơn hàng thành công";
                return RedirectToAction("Index", "Account");
            }

            //ModelState.AddModelError("", "Huỷ đơn hàng thành công");
            TempData["CancelOrderFail"] = "Huỷ đơn hàng không thành công";
            return RedirectToAction("Index", "Account");
        }
    }
}
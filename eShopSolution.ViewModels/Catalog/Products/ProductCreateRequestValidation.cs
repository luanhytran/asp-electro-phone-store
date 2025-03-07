﻿using FluentValidation;

namespace eShopSolution.ViewModels.Catalog.Products
{
    public class ProductCreateRequestValidation : AbstractValidator<ProductCreateRequest>
    {
        public ProductCreateRequestValidation()
        {
            RuleFor(x => x.Price).NotEmpty().WithMessage("Giá không được để trống")
                .LessThan(100000000).WithMessage("Giá tiền phải nhỏ hơn 100.000.000")
                .GreaterThan(0).WithMessage("Giá tiền phải lớn hơn 0");

            RuleFor(x => x.Stock).NotEmpty().WithMessage("Số lượng không được để trống")
                .LessThan(1000).WithMessage("Só lượng phải lớn hơn 0")
                .GreaterThanOrEqualTo(0).WithMessage("Số lượng phải nhỏ hơn 1.000");

            RuleFor(x => x.Name).NotEmpty().WithMessage("Tên sản phẩm không được để trống")
               .MaximumLength(200).WithMessage("Tên không được quá 200 ký tự");

            RuleFor(x => x.Description).NotEmpty().WithMessage("Thông số kỹ thuật không được để trống")
               .MaximumLength(4000).WithMessage("Mô tả không được quá 4000 ký tự");

            RuleFor(x => x.Details)
                .MaximumLength(10000).WithMessage("Mô tả chi tiết không được quá 10.000 ký tự");
        }
    }
}
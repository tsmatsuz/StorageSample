using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Web.Mvc;
using System.Web.Security;
using System.Text.RegularExpressions;

namespace WordSampleWebRole.Models
{
    public class OrderModel
    {
        [Display(Name = "Deliver Information")]
        public DeliverInfoBaseModel Deliver;

        [Display(Name = "Products")]
        public List<ProductItemModel> Products;
    }

    public abstract class DeliverInfoBaseModel : IValidatableObject
    {
        [Display(Name = "氏名")]
        public abstract string Name { get; set; }

        [Display(Name = "住所")]
        public abstract string Address { get; set; }

        [Display(Name = "郵便番号")]
        public abstract string ZipCode { get; set; }

        [Display(Name = "電話番号")]
        [DataType(DataType.PhoneNumber)]
        public string Telephone { get; set; }

        [Display(Name = "メールアドレス")]
        [DataType(DataType.EmailAddress)]
        public string EmailAddress { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!String.IsNullOrEmpty(Telephone) && !Regex.IsMatch(Telephone, @"^[0-9-]{6,9}$|^[0-9-]{12}$|^\d{1,4}-\d{4}$|^\d{2,5}-\d{1,4}-\d{4}$"))
                yield return new ValidationResult(
                    "Telephone number is invalid. (Ex: 03-3333-3333, 0333333333, etc)",
                    new[] { "Telephone" });
            if (!String.IsNullOrEmpty(ZipCode) && !Regex.IsMatch(ZipCode, @"^(\d{3}-\d{4}|\d{7})$"))
                yield return new ValidationResult(
                    "Zip code is invalid. (Ex: 1234567, 123-4567, etc)",
                    new[] { "ZipCode" });
            if (!String.IsNullOrEmpty(EmailAddress) && !Regex.IsMatch(EmailAddress, @"^([0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*@([0-9a-zA-Z][-\w]*[0-9a-zA-Z]\.)+[a-zA-Z]{2,9})$"))
                yield return new ValidationResult(
                    "E-mail address is invalid. (Ex: demo@example.com, etc)",
                    new[] { "EmailAddress" });
        }
    }

    public class DeliverInfoNewModel : DeliverInfoBaseModel
    {
        public override string Name { get; set; }

        public override string Address { get; set; }

        public override string ZipCode { get; set; }
    }

    public class DeliverInfoModel : DeliverInfoBaseModel
    {
        [Required]
        public override string Name { get; set; }

        [Required]
        public override string Address { get; set; }

        [Required]
        public override string ZipCode { get; set; }
    }

    public class ProductItemModel : IValidatableObject
    {
        [Required]
        [Display(Name = "Product Id")]
        public int ProductId { get; set; }

        [Required]
        [Display(Name = "商品名")]
        public string ProductName { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        [Display(Name = "商品単価")]
        public decimal ProductUnitPrice { get; set; }

        [Required]
        [Display(Name = "数量")]
        public int ProductCount { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!(ProductCount > 0))
                yield return new ValidationResult(
                    "ProductCount must be positive number.",
                    new[] { "ProductCount" });
        }
    }

    public class O365SaveInfoModel
    {
        public string SiteUrl { get; set; }
        public string DocLibName { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }
    }

    public class SkydriveSaveInfoModel
    {
        public string AccessToken { get; set; }
        public string FolderLocation { get; set; }
    }

    public class BlobSaveInfoModel
    {
        [Required]
        [Display(Name = "Account")]
        public string Account { get; set; }
        [Required]
        [Display(Name = "Access Key")]
        public string AccessKey { get; set; }
        [Required]
        [Display(Name = "Container")]
        public string Container { get; set; }
    }
}

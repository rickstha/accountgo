using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Dto.Common
{
    public class PriceTaxDto
    {
        public int Id { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Error 404.")]
        public decimal Price { get; set; }

        [Required]
        [Range(0, 100, ErrorMessage = "Tax rate must be between 0 and 100.")]
        public decimal TaxRatePercent { get; set; }

    
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }
    }

namespace AccountGoWeb.Controllers
{
    public class PriceTaxController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View(new PriceTaxDto());
        }

        [HttpPost]
        [Route("api/pricetax/calculate")]
        public IActionResult Calculate([FromBody] PriceTaxDto model)
        {
            if (model == null)
                return BadRequest("Request body is required.");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToArray();

                return BadRequest(errors);
            }

            var result = CalculateTax(model.Price, model.TaxRatePercent);

            return Ok(new
            {
                price = model.Price,
                taxRatePercent = model.TaxRatePercent,
                taxAmount = result.taxAmount,
                total = result.total
            });
        }

        [HttpPost]
        [Route("api/pricetax/save")]
        public IActionResult Save([FromBody] PriceTaxDto model)
        {
            if (model == null)
                return BadRequest("Request body is required.");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToArray();

                return BadRequest(errors);
            }

            try
            {
                var (taxAmount, total) = CalculateTax(model.Price, model.TaxRatePercent);
                model.TaxAmount = taxAmount;
                model.Total = total;

                return Ok(new
                {
                    message = "Saved successfully.",
                    data = model
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        private static (decimal taxAmount, decimal total) CalculateTax(decimal price, decimal taxRatePercent)
        {
            var taxAmount = Math.Round(price * (taxRatePercent / 100m), 2, MidpointRounding.AwayFromZero);
            var total = price + taxAmount;
            return (taxAmount, total);
        }
    }
}
}
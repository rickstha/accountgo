using Microsoft.AspNetCore.Mvc;
using Dto.TaxSystem;
using Services.TaxSystem;
using System.Collections.Generic;
using System.Linq;
using Core.Domain;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaxController : BaseController
    {
        private readonly ITaxService _taxService;

        public TaxController(ITaxService taxService)
        {
            _taxService = taxService;
        }

        #region Taxes

        [HttpGet("GetTax")]
        public IActionResult GetTax(int itemId, int partyId, int type)
        {
            if (itemId <= 0)
                return BadRequest("Invalid item.");

            if (partyId <= 0)
                return BadRequest("Invalid party.");

            if (!System.Enum.IsDefined(typeof(PartyTypes), type))
                return BadRequest("Invalid party type.");

            var taxes = _taxService.GetIntersectionTaxes(
                itemId,
                partyId,
                (PartyTypes)type);

            return Ok(taxes?.Select(MapTax) ?? Enumerable.Empty<Tax>());
        }

        [HttpGet("Taxes")]
        public IActionResult Taxes()
        {
            var dto = new TaxSystemDto
            {
                Taxes = _taxService.GetTaxes(true)?
                    .Select(MapTax)
                    .ToList()
                    ?? new List<Tax>(),

                TaxGroups = _taxService.GetTaxGroups()?
                    .Select(MapTaxGroup)
                    .ToList()
                    ?? new List<TaxGroup>(),

                ItemTaxGroups = _taxService.GetItemTaxGroups()?
                    .Select(MapItemTaxGroup)
                    .ToList()
                    ?? new List<ItemTaxGroup>()
            };

            return Ok(dto);
        }

        #endregion

        #region Tax Groups

        [HttpGet("TaxGroups")]
        public IActionResult TaxGroups()
        {
            var groups = _taxService.GetTaxGroups()?
                .Select(MapTaxGroup)
                .ToList()
                ?? new List<TaxGroup>();

            return Ok(groups);
        }

        #endregion

        #region Item Tax Groups

        [HttpGet("ItemTaxGroups")]
        public IActionResult ItemTaxGroups()
        {
            var groups = _taxService.GetItemTaxGroups()?
                .Select(MapItemTaxGroup)
                .ToList()
                ?? new List<ItemTaxGroup>();

            return Ok(groups);
        }

        #endregion

        #region Mapping

        private static Tax MapTax(dynamic tax)
        {
            return new Tax
            {
                Id = tax.Id,
                TaxCode = tax.TaxCode,
                TaxName = tax.TaxName,
                Rate = tax.Rate,
                IsActive = tax.IsActive
            };
        }

        private static TaxGroup MapTaxGroup(dynamic group)
        {
            return new TaxGroup
            {
                Id = group.Id,
                Description = group.Description,
                IsActive = group.IsActive,
                TaxAppliedToShipping = group.TaxAppliedToShipping,

                Taxes = group.TaxGroupTax?
                    .Select(x => new TaxGroupTax
                    {
                        Id = x.Id,
                        TaxId = x.TaxId,
                        TaxGroupId = x.TaxGroupId
                    })
                    .ToList()
                    ?? new List<TaxGroupTax>()
            };
        }

        private static ItemTaxGroup MapItemTaxGroup(dynamic group)
        {
            return new ItemTaxGroup
            {
                Id = group.Id,
                Name = group.Name,
                IsFullyExempt = group.IsFullyExempt,

                Taxes = group.ItemTaxGroupTax?
                    .Select(x => new ItemTaxGroupTax
                    {
                        Id = x.Id,
                        TaxId = x.TaxId,
                        ItemTaxGroupId = x.ItemTaxGroupId
                    })
                    .ToList()
                    ?? new List<ItemTaxGroupTax>()
            };
        }

        #endregion
    }
}
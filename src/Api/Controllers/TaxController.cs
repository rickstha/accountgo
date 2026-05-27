using Microsoft.AspNetCore.Mvc;
using Dto.TaxSystem;
using Services.TaxSystem;
using System.Collections.Generic;
using System.Linq;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaxController : BaseController
    {
        private readonly ITaxService _taxService;

        public TaxController(ITaxService taxService)
        {
            _taxService = taxService;
        }

        /// <summary>
        /// Get tax intersection based on item and party.
        /// </summary>
        [HttpGet]
        [Route("GetTax")]
        public IActionResult GetTax(int itemId, int partyId, int type = 0)
        {
            if (type == 0)
            {
                return BadRequest("Type is required.");
            }

            // Validate enum
            if (!System.Enum.IsDefined(typeof(Core.Domain.PartyTypes), type))
            {
                return BadRequest("Invalid party type.");
            }

            var partyType = (Core.Domain.PartyTypes)type;

            var taxes = _taxService.GetIntersectionTaxes(itemId, partyId, partyType);

            if (taxes == null)
            {
                return NotFound();
            }

            var taxesDto = taxes.Select(t => new Tax
            {
                Id = t.Id,
                TaxCode = t.TaxCode,
                TaxName = t.TaxName,
                Rate = t.Rate,
                IsActive = t.IsActive
            }).ToList();

            return Ok(taxesDto);
        }

        // =========================================
        // TAX GROUPS
        // =========================================

        [HttpGet]
        [Route("TaxGroups")]
        public IActionResult TaxGroups()
        {
            var taxGroups = _taxService.GetTaxGroups();

            if (taxGroups == null)
            {
                return Ok(new List<TaxGroup>());
            }

            var taxGroupsDto = taxGroups.Select(group => new TaxGroup
            {
                Id = group.Id,
                Description = group.Description,
                IsActive = group.IsActive,
                TaxAppliedToShipping = group.TaxAppliedToShipping
            }).ToList();

            return Ok(taxGroupsDto);
        }

        // =========================================
        // ITEM TAX GROUPS
        // =========================================

        [HttpGet]
        [Route("ItemTaxGroups")]
        public IActionResult ItemTaxGroups()
        {
            var itemTaxGroups = _taxService.GetItemTaxGroups();

            if (itemTaxGroups == null)
            {
                return Ok(new List<ItemTaxGroup>());
            }

            var itemTaxGroupsDto = itemTaxGroups.Select(group => new ItemTaxGroup
            {
                Id = group.Id,
                Name = group.Name,
                IsFullyExempt = group.IsFullyExempt
            }).ToList();

            return Ok(itemTaxGroupsDto);
        }

        // =========================================
        // COMPLETE TAX SYSTEM
        // =========================================

        [HttpGet]
        [Route("Taxes")]
        public IActionResult Taxes()
        {
            var taxSystemDto = new TaxSystemDto
            {
                Taxes = new List<Tax>(),
                TaxGroups = new List<TaxGroup>(),
                ItemTaxGroups = new List<ItemTaxGroup>()
            };

            // =====================================
            // TAXES
            // =====================================

            var taxes = _taxService.GetTaxes(true);

            if (taxes != null)
            {
                taxSystemDto.Taxes = taxes.Select(t => new Tax
                {
                    Id = t.Id,
                    TaxCode = t.TaxCode,
                    TaxName = t.TaxName,
                    Rate = t.Rate,
                    IsActive = t.IsActive
                }).ToList();
            }

            // =====================================
            // TAX GROUPS
            // =====================================

            var taxGroups = _taxService.GetTaxGroups();

            if (taxGroups != null)
            {
                foreach (var group in taxGroups)
                {
                    var groupDto = new TaxGroup
                    {
                        Id = group.Id,
                        Description = group.Description,
                        IsActive = group.IsActive,
                        TaxAppliedToShipping = group.TaxAppliedToShipping,
                        Taxes = new List<TaxGroupTax>()
                    };

                    if (group.TaxGroupTax != null)
                    {
                        foreach (var tax in group.TaxGroupTax)
                        {
                            groupDto.Taxes.Add(new TaxGroupTax
                            {
                                Id = tax.Id,
                                TaxId = tax.TaxId,
                                TaxGroupId = tax.TaxGroupId
                            });
                        }
                    }

                    taxSystemDto.TaxGroups.Add(groupDto);
                }
            }

            // =====================================
            // ITEM TAX GROUPS
            // =====================================

            var itemTaxGroups = _taxService.GetItemTaxGroups();

            if (itemTaxGroups != null)
            {
                foreach (var group in itemTaxGroups)
                {
                    var groupDto = new ItemTaxGroup
                    {
                        Id = group.Id,
                        Name = group.Name,
                        IsFullyExempt = group.IsFullyExempt,
                        Taxes = new List<ItemTaxGroupTax>()
                    };

                    if (group.ItemTaxGroupTax != null)
                    {
                        foreach (var tax in group.ItemTaxGroupTax)
                        {
                            groupDto.Taxes.Add(new ItemTaxGroupTax
                            {
                                Id = tax.Id,
                                TaxId = tax.TaxId,
                                ItemTaxGroupId = tax.ItemTaxGroupId
                            });
                        }
                    }

                    taxSystemDto.ItemTaxGroups.Add(groupDto);
                }
            }

            return Ok(taxSystemDto);
        }
    }
}
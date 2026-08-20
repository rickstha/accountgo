using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Services.Sales;
using Services.Purchasing;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactController : BaseController
    {
        private readonly ISalesService _salesService;
        private readonly IPurchasingService _purchasingService;

        private const int PartyTypeCustomer = 1;
        private const int PartyTypeVendor = 2;

        public ContactController(ISalesService salesService, IPurchasingService purchasingService)
        {
            _salesService = salesService;
            _purchasingService = purchasingService;
        }

        [HttpGet]
        [Route("Contacts")]
        public IActionResult Contacts(int partyId = 0, int partyType = 0)
        {
            if (partyId <= 0)
            {
                return BadRequest("partyId is required.");
            }

            var contactsDto = new List<Dto.Common.Contact>();

            if (partyType == PartyTypeCustomer)
            {
                var customer = _salesService.GetCustomerById(partyId);
                if (customer?.CustomerContact == null)
                {
                    return Ok(contactsDto);
                }

                foreach (var contact in customer.CustomerContact
                             .Where(cc => cc.Contact != null)
                             .Select(cc => cc.Contact))
                {
                    contactsDto.Add(new Dto.Common.Contact
                    {
                        Id = contact.Id,
                        FirstName = contact.FirstName,
                        LastName = contact.LastName,
                        HoldingPartyId = partyId,
                        HoldingPartyType = PartyTypeCustomer
                    });
                }
            }
            else if (partyType == PartyTypeVendor)
            {
                var vendor = _purchasingService.GetVendorById(partyId);
                if (vendor?.VendorContact == null)
                {
                    return Ok(contactsDto);
                }

                foreach (var contact in vendor.VendorContact
                             .Where(vc => vc.Contact != null)
                             .Select(vc => vc.Contact))
                {
                    contactsDto.Add(new Dto.Common.Contact
                    {
                        Id = contact.Id,
                        FirstName = contact.FirstName,
                        LastName = contact.LastName,
                        HoldingPartyId = partyId,
                        HoldingPartyType = PartyTypeVendor
                    });
                }
            }
            else
            {
                return BadRequest("Invalid partyType. Use 1 for Customer or 2 for Vendor.");
            }

            return Ok(contactsDto);
        }

        [HttpGet]
        [Route("Contact")]
        public IActionResult Contact(int id, int partyId, int partyType)
        {
            if (id <= 0)
            {
                return BadRequest("id is required.");
            }

            // NOTE: If your service still has the typo "GetContacyById", change this back.
            var contact = _salesService.GetContactById(id);
            if (contact == null)
            {
                return NotFound($"Contact with id {id} not found.");
            }

            var contactDto = new Dto.Common.Contact
            {
                Id = contact.Id,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                MiddleName = contact.MiddleName,
                HoldingPartyId = partyId,
                HoldingPartyType = partyType,
                Party = contact.Party == null
                    ? null
                    : new Dto.Common.Party
                    {
                        Id = contact.Party.Id,
                        Email = contact.Party.Email,
                        Fax = contact.Party.Fax,
                        Phone = contact.Party.Phone,
                        Website = contact.Party.Website
                    }
            };

            return Ok(contactDto);
        }

        [HttpPost]
        [Route("SaveContact")]
        public IActionResult SaveContact([FromBody] Dto.Common.Contact model)
        {
            if (model == null)
            {
                return BadRequest("Contact data is required.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                Core.Domain.Contact contact;

                if (model.Id == 0)
                {
                    contact = new Core.Domain.Contact
                    {
                        Party = new Core.Domain.Party
                        {
                            PartyType = model.HoldingPartyType == PartyTypeCustomer
                                ? Core.Domain.PartyTypes.Customer
                                : Core.Domain.PartyTypes.Vendor
                        }
                    };
                }
                else
                {
                    contact = _salesService.GetContactById(model.Id);
                    if (contact == null)
                    {
                        return NotFound($"Contact with id {model.Id} not found.");
                    }
                }

                // Map Contact fields
                contact.Id = model.Id;
                contact.ContactType = (Core.Domain.ContactTypes)model.HoldingPartyType;
                contact.FirstName = model.FirstName;
                contact.MiddleName = model.MiddleName;
                contact.LastName = model.LastName;

                // Map Party fields
                if (model.Party != null)
                {
                    contact.Party ??= new Core.Domain.Party();
                    contact.Party.Website = model.Party.Website;
                    contact.Party.Email = model.Party.Email;
                    contact.Party.Phone = model.Party.Phone;
                    contact.Party.Fax = model.Party.Fax;
                }

                if (contact.Id > 0)
                {
                    _salesService.SaveContact(contact);
                }
                else
                {
                    if (model.HoldingPartyType == PartyTypeCustomer)
                    {
                        var customer = _salesService.GetCustomerById(model.HoldingPartyId);
                        if (customer == null)
                        {
                            return NotFound($"Customer with id {model.HoldingPartyId} not found.");
                        }

                        if (customer.PrimaryContact == null)
                        {
                            customer.PrimaryContact = contact;
                        }

                        var customerContact = new Core.Domain.CustomerContact
                        {
                            Contact = contact,
                            CustomerId = customer.Id
                        };

                        customer.CustomerContact ??= new List<Core.Domain.CustomerContact>();
                        customer.CustomerContact.Add(customerContact);

                        _salesService.UpdateCustomer(customer);
                    }
                    else if (model.HoldingPartyType == PartyTypeVendor)
                    {
                        var vendor = _purchasingService.GetVendorById(model.HoldingPartyId);
                        if (vendor == null)
                        {
                            return NotFound($"Vendor with id {model.HoldingPartyId} not found.");
                        }

                        if (vendor.PrimaryContact == null)
                        {
                            vendor.PrimaryContact = contact;
                        }

                        var vendorContact = new Core.Domain.VendorContact
                        {
                            Contact = contact,
                            VendorId = vendor.Id
                        };

                        vendor.VendorContact ??= new List<Core.Domain.VendorContact>();
                        vendor.VendorContact.Add(vendorContact);

                        _purchasingService.UpdateVendor(vendor);
                    }
                    else
                    {
                        return BadRequest("Invalid HoldingPartyType. Use 1 for Customer or 2 for Vendor.");
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new[] { message });
            }
        }
    }
}
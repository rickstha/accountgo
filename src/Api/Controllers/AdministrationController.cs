using Dto.Administration;
using Dto.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Services.Administration;
using Services.Financial;
using Services.Inventory;
using Services.Purchasing;
using Services.Sales;
using Services.Security;
using Services.TaxSystem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Api.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class AdministrationController : BaseController
    {
        private readonly IAdministrationService _adminService;
        private readonly IFinancialService _financialService;
        private readonly ISalesService _salesService;
        private readonly IPurchasingService _purchasingService;
        private readonly IInventoryService _inventoryService;
        private readonly ISecurityService _securityService;
        private readonly ITaxService _taxService;
        private readonly IMainAcount _mainAccount;
        private readonly ICompanycode _companyCode;
        private readonly IIncomeSummaryAccount _incomeSummaryAccount;
        private readonly ILogger<AdministrationController> _logger;

        public AdministrationController(
            IAdministrationService adminService,
            IFinancialService financialService,
            ISalesService salesService,
            IPurchasingService purchasingService,
            IInventoryService inventoryService,
            ISecurityService securityService,
            ITaxService taxService,
            IMainAcount mainAccount,
            ICompanycode companyCode,
            IIncomeSummaryAccount IncomeSummaryAccount,
            I
            ILogger<AdministrationController> logger)
        {
            _adminService = adminService;
            _financialService = financialService;
            _salesService = salesService;
            _purchasingService = purchasingService;
            _inventoryService = inventoryService;
            _securityService = securityService;
            _taxService = taxService;
            _ainAcount = mainAccount;
            _companycode = companyCode;
            _IncomeSummaryAccount = IncomeSummaryAccount;
            _logger = logger;
        }

        // =========================================
        // SETUP
        // =========================================

        [HttpPost("setup")]
        public IActionResult Setup()
        {
            try
            {
                var initializer = CreateInitializer();
                bool success = initializer.Setup();

                if (success)
                {
                    return Ok(new { message = "Initialization completed successfully." });
                }

                return BadRequest(new { message = "Initialization failed." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Setup failed.");
                return StatusCode(500, new { message = "An error occurred while running setup." });
            }
        }

        // =========================================
        // CLEAR DATABASE
        // =========================================

        // Extremely destructive → POST (not GET)
        [HttpPost("clear")]
        public IActionResult Clear()
        {
            try
            {
                var initializer = CreateInitializer();
                bool success = initializer.Clear();

                if (success)
                {
                    return Ok(new { message = "Database cleared successfully." });
                }

                return BadRequest(new { message = "Database clearing failed." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database clear failed.");
                return StatusCode(500, new { message = "An error occurred while clearing the database." });
            }
        }

        // =========================================
        // COMPANY
        // =========================================

        [HttpGet("company")]
        public IActionResult Company(string? companyCode)
        {
            try
            {
                var company = _adminService.GetDefaultCompany();

                if (company == null)
                {
                    return NotFound(new { message = "Company not found." });
                }

                if (!string.IsNullOrWhiteSpace(companyCode) &&
                    !string.Equals(company.CompanyCode, companyCode, StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(new { message = "Company not found for the provided code." });
                }

                return Ok(company);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve company.");
                return StatusCode(500, new { message = "An error occurred while retrieving the company." });
            }
        }

        // =========================================
        // AUDIT LOGS
        // =========================================

        [HttpGet("auditlogs")]
        public IActionResult AuditLogs()
        {
            try
            {
                var auditLogs = _adminService.AuditLogs()
                                ?? Enumerable.Empty<Core.Domain.Auditing.AuditLog>();

                var auditLogsDto = auditLogs.Select(log => new AuditLog
                {
                    Id = log.Id,
                    UserName = log.UserName,
                    AuditEventDateUTC = log.AuditEventDateUTC,
                    AuditEventType = log.AuditEventType,
                    TableName = log.TableName,
                    RecordId = log.RecordId,
                    FieldName = log.FieldName,
                    OriginalValue = log.OriginalValue,
                    NewValue = log.NewValue
                }).ToList();

                return Ok(auditLogsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve audit logs.");
                return StatusCode(500, new { message = "An error occurred while retrieving audit logs." });
            }
        }

        // =========================================
        // USERS
        // =========================================

        [HttpGet("users")]
        public IActionResult Users()
        {
            try
            {
                var users = _securityService.GetAllUser()
                            ?? Enumerable.Empty<Core.Domain.Security.User>();

                var usersDto = users.Select(user => new User
                {
                    Id = user.Id,
                    FirstName = user.Firstname,
                    LastName = user.Lastname,
                    Email = user.EmailAddress,
                    UserName = user.UserName,
                    Roles = MapUserRoles(user.Roles)
                }).ToList();

                return Ok(usersDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve users.");
                return StatusCode(500, new { message = "An error occurred while retrieving users." });
            }
        }

        // =========================================
        // ROLES
        // =========================================

        [HttpGet("roles")]
        public IActionResult Roles()
        {
            try
            {
                var roles = _securityService.GetAllSecurityRole()
                            ?? Enumerable.Empty<Core.Domain.Security.SecurityRole>();

                var rolesDto = roles.Select(role => new Role
                {
                    Id = role.Id,
                    Name = role.Name,
                    DisplayName = role.DisplayName,
                    Permissions = (role.Permissions ?? Enumerable.Empty<Core.Domain.Security.SecurityRolePermission>())
                        .Select(permission => new Permission
                        {
                            Id = permission.Id,
                            Name = permission.SecurityPermission?.Name,
                            DisplayName = permission.SecurityPermission?.DisplayName
                        })
                        .ToList()
                }).ToList();

                return Ok(rolesDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve roles.");
                return StatusCode(500, new { message = "An error occurred while retrieving roles." });
            }
        }

        // =========================================
        // MAIN GROUPS
        // =========================================

        [HttpGet("maingroups")]
        public IActionResult MainGroups()
        {
            try
            {
                var mainGroups = _securityService.GetAllSecurityMainGroup()
                                 ?? Enumerable.Empty<Core.Domain.Security.SecurityGroup>();

                var groupsDto = mainGroups.Select(mainGroup => new Group
                {
                    Id = mainGroup.Id,
                    Name = mainGroup.Name,
                    DisplayName = mainGroup.DisplayName,
                    Permissions = (mainGroup.Permissions ?? Enumerable.Empty<Core.Domain.Security.SecurityPermission>())
                        .Select(permission => new Permission
                        {
                            Id = permission.Id,
                            Name = permission.Name,
                            DisplayName = permission.DisplayName
                        })
                        .ToList()
                }).ToList();

                return Ok(groupsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve main groups.");
                return StatusCode(500, new { message = "An error occurred while retrieving main groups." });
            }
        }

        // =========================================
        // GROUPS
        // =========================================

        [HttpGet("groups")]
        public IActionResult Groups()
        {
            try
            {
                var groups = _securityService.GetAllSecurityGroup()
                             ?? Enumerable.Empty<Core.Domain.Security.SecurityGroup>();

                var groupsDto = groups.Select(group => new Group
                {
                    Id = group.Id,
                    Name = group.Name,
                    DisplayName = group.DisplayName,
                    Permissions = (group.Permissions ?? Enumerable.Empty<Core.Domain.Security.SecurityPermission>())
                        .Select(permission => new Permission
                        {
                            Id = permission.Id,
                            Name = permission.Name,
                            DisplayName = permission.DisplayName
                        })
                        .ToList()
                }).ToList();

                return Ok(groupsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve groups.");
                return StatusCode(500, new { message = "An error occurred while retrieving groups." });
            }
        }

        // =========================================
        // GET USER
        // =========================================

        [HttpGet("getuser")]
        public IActionResult GetUser(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest(new { message = "Username is required." });
            }

            try
            {
                var user = _securityService.GetUser(username);

                if (user == null)
                {
                    return NotFound(new { message = "User not found." });
                }

                var userDto = new User
                {
                    Id = user.Id,
                    FirstName = user.Firstname,
                    LastName = user.Lastname,
                    UserName = user.UserName,
                    Email = user.EmailAddress,
                    Roles = MapUserRoles(user.Roles)
                };

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve user {Username}.", username);
                return StatusCode(500, new { message = "An error occurred while retrieving the user." });
            }
        }

        // =========================================
        // SAVE COMPANY
        // =========================================

        [HttpPost("savecompany")]
        public IActionResult SaveCompany([FromBody] Company companyDto)
        {
            if (companyDto == null)
            {
                return BadRequest(new { message = "Company data is required." });
            }

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
                Core.Domain.Company company;

                if (companyDto.Id == 0)
                {
                    company = new Core.Domain.Company();
                }
                else
                {
                    company = _adminService.GetCompanyById(companyDto.Id);

                    if (company == null)
                    {
                        return NotFound(new { message = "Company not found." });
                    }
                }

                company.CompanyCode = companyDto.CompanyCode;
                company.Name = companyDto.Name;
                company.ShortName = companyDto.ShortName;

                _adminService.SaveCompany(company);

                return Ok(new
                {
                    message = "Company saved successfully.",
                    id = company.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save company.");
                return StatusCode(500, new { message = "An error occurred while saving the company." });
            }
        }

        // ------------------------------------------------------------------
        // Private helpers
        // ------------------------------------------------------------------

        private Api.Data.Initializer CreateInitializer()
        {
            return new Api.Data.Initializer(
                _adminService,
                _financialService,
                _salesService,
                _purchasingService,
                _inventoryService,
                _securityService);
        }

        private static List<Role> MapUserRoles(IEnumerable<Core.Domain.Security.SecurityUserRole>? userRoles)
        {
            var rolesDto = new List<Role>();

            if (userRoles == null)
            {
                return rolesDto;
            }

            foreach (var role in userRoles)
            {
                if (role == null)
                {
                    continue;
                }

                var roleDto = new Role
                {
                    Id = role.SecurityRoleId,
                    Name = role.SecurityRole?.Name,
                    DisplayName = role.SecurityRole?.DisplayName,
                    SysAdmin = role.SecurityRole?.SysAdmin ?? false,
                    Permissions = new List<Permission>()
                };

                if (role.SecurityRole?.Permissions != null)
                {
                    foreach (var permission in role.SecurityRole.Permissions)
                    {
                        if (permission == null)
                        {
                            continue;
                        }

                        roleDto.Permissions.Add(new Permission
                        {
                            Id = permission.SecurityPermissionId,
                            Name = permission.SecurityPermission?.Name,
                            Group = new Group
                            {
                                Name = permission.SecurityPermission?.Group?.Name
                            }
                        });
                    }
                }

                rolesDto.Add(roleDto);
            }

            return rolesDto;
        }
    }
}
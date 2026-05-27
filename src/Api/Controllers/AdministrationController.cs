using Dto.Administration;
using Dto.Security;
using Microsoft.AspNetCore.Mvc;
using Services.Administration;
using Services.Financial;
using Services.Inventory;
using Services.Purchasing;
using Services.Sales;
using Services.Security;
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

        public AdministrationController(
            IAdministrationService adminService,
            IFinancialService financialService,
            ISalesService salesService,
            IPurchasingService purchasingService,
            IInventoryService inventoryService,
            ISecurityService securityService)
        {
            _adminService = adminService;
            _financialService = financialService;
            _salesService = salesService;
            _purchasingService = purchasingService;
            _inventoryService = inventoryService;
            _securityService = securityService;
        }

        // =========================================
        // SETUP
        // =========================================

        [HttpGet("setup")]
        public IActionResult Setup()
        {
            try
            {
                var initializer = new Api.Data.Initializer(
                    _adminService,
                    _financialService,
                    _salesService,
                    _purchasingService,
                    _inventoryService,
                    _securityService);

                bool success = initializer.Setup();

                if (success)
                {
                    return Ok(new
                    {
                        message = "Initialization completed successfully."
                    });
                }

                return BadRequest(new
                {
                    message = "Initialization failed."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }

        // =========================================
        // CLEAR DATABASE
        // =========================================

        [HttpGet("clear")]
        public IActionResult Clear()
        {
            try
            {
                var initializer = new Api.Data.Initializer(
                    _adminService,
                    _financialService,
                    _salesService,
                    _purchasingService,
                    _inventoryService,
                    _securityService);

                bool success = initializer.Clear();

                if (success)
                {
                    return Ok(new
                    {
                        message = "Database cleared successfully."
                    });
                }

                return BadRequest(new
                {
                    message = "Database clearing failed."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }

        // =========================================
        // COMPANY
        // =========================================

        [HttpGet("company")]
        public IActionResult Company(string? companyCode)
        {
            var company = _adminService.GetDefaultCompany();

            if (company == null)
            {
                return NotFound(new
                {
                    message = "Company not found."
                });
            }

            return Ok(company);
        }

        // =========================================
        // AUDIT LOGS
        // =========================================

        [HttpGet("auditlogs")]
        public IActionResult AuditLogs()
        {
            var auditLogs = _adminService.AuditLogs();

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

        // =========================================
        // USERS
        // =========================================

        [HttpGet("users")]
        public IActionResult Users()
        {
            var users = _securityService.GetAllUser();

            var usersDto = new List<User>();

            foreach (var user in users)
            {
                var userDto = new User
                {
                    Id = user.Id,
                    FirstName = user.Firstname,
                    LastName = user.Lastname,
                    Email = user.EmailAddress,
                    UserName = user.UserName,
                    Roles = new List<Role>()
                };

                if (user.Roles != null)
                {
                    foreach (var role in user.Roles)
                    {
                        var roleDto = new Role
                        {
                            Id = role.Id,
                            Name = role.SecurityRole?.Name,
                            DisplayName = role.SecurityRole?.DisplayName,
                            Permissions = new List<Permission>()
                        };

                        userDto.Roles.Add(roleDto);
                    }
                }

                usersDto.Add(userDto);
            }

            return Ok(usersDto);
        }

        // =========================================
        // ROLES
        // =========================================

        [HttpGet("roles")]
        public IActionResult Roles()
        {
            var roles = _securityService.GetAllSecurityRole();

            var rolesDto = new List<Role>();

            foreach (var role in roles)
            {
                var roleDto = new Role
                {
                    Id = role.Id,
                    Name = role.Name,
                    DisplayName = role.DisplayName,
                    Permissions = new List<Permission>()
                };

                if (role.Permissions != null)
                {
                    foreach (var permission in role.Permissions)
                    {
                        var permissionDto = new Permission
                        {
                            Id = permission.Id,
                            Name = permission.SecurityPermission?.Name,
                            DisplayName = permission.SecurityPermission?.DisplayName
                        };

                        roleDto.Permissions.Add(permissionDto);
                    }
                }

                rolesDto.Add(roleDto);
            }

            return Ok(rolesDto);
        }

        // =========================================
        // MAIN GROUPS
        // =========================================

        [HttpGet("maingroups")]
        public IActionResult MainGroups()
        {
            var mainGroups = _securityService.GetAllSecurityMainGroup();

            var groupsDto = new List<Group>();

            foreach (var mainGroup in mainGroups)
            {
                var groupDto = new Group
                {
                    Id = mainGroup.Id,
                    Name = mainGroup.Name,
                    DisplayName = mainGroup.DisplayName,
                    Permissions = new List<Permission>()
                };

                if (mainGroup.Permissions != null)
                {
                    foreach (var permission in mainGroup.Permissions)
                    {
                        var permissionDto = new Permission
                        {
                            Id = permission.Id,
                            Name = permission.Name,
                            DisplayName = permission.DisplayName
                        };

                        groupDto.Permissions.Add(permissionDto);
                    }
                }

                groupsDto.Add(groupDto);
            }

            return Ok(groupsDto);
        }

        // =========================================
        // GROUPS
        // =========================================

        [HttpGet("groups")]
        public IActionResult Groups()
        {
            var groups = _securityService.GetAllSecurityGroup();

            var groupsDto = new List<Group>();

            foreach (var group in groups)
            {
                var groupDto = new Group
                {
                    Id = group.Id,
                    Name = group.Name,
                    DisplayName = group.DisplayName,
                    Permissions = new List<Permission>()
                };

                if (group.Permissions != null)
                {
                    foreach (var permission in group.Permissions)
                    {
                        var permissionDto = new Permission
                        {
                            Id = permission.Id,
                            Name = permission.Name,
                            DisplayName = permission.DisplayName
                        };

                        groupDto.Permissions.Add(permissionDto);
                    }
                }

                groupsDto.Add(groupDto);
            }

            return Ok(groupsDto);
        }

        // =========================================
        // GET USER
        // =========================================

        [HttpGet("getuser")]
        public IActionResult GetUser(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest(new
                {
                    message = "Username is required."
                });
            }

            var user = _securityService.GetUser(username);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User not found."
                });
            }

            var userDto = new User
            {
                Id = user.Id,
                FirstName = user.Firstname,
                LastName = user.Lastname,
                UserName = user.UserName,
                Email = user.EmailAddress,
                Roles = new List<Role>()
            };

            if (user.Roles != null)
            {
                foreach (var role in user.Roles)
                {
                    var roleDto = new Role
                    {
                        Id = role.SecurityRoleId,
                        Name = role.SecurityRole?.Name,
                        SysAdmin = role.SecurityRole?.SysAdmin ?? false,
                        Permissions = new List<Permission>()
                    };

                    if (role.SecurityRole?.Permissions != null)
                    {
                        foreach (var permission in role.SecurityRole.Permissions)
                        {
                            var permissionDto = new Permission
                            {
                                Id = permission.SecurityPermissionId,
                                Name = permission.SecurityPermission?.Name,
                                Group = new Group
                                {
                                    Name = permission.SecurityPermission?.Group?.Name
                                }
                            };

                            roleDto.Permissions.Add(permissionDto);
                        }
                    }

                    userDto.Roles.Add(roleDto);
                }
            }

            return Ok(userDto);
        }

        // =========================================
        // SAVE COMPANY
        // =========================================

        [HttpPost("savecompany")]
        public IActionResult SaveCompany([FromBody] Company companyDto)
        {
            try
            {
                if (companyDto == null)
                {
                    return BadRequest(new
                    {
                        message = "Company data is required."
                    });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToArray();

                    return BadRequest(errors);
                }

                Core.Domain.Company company;

                if (companyDto.Id == 0)
                {
                    company = new Core.Domain.Company();
                }
                else
                {
                    company = _adminService.GetDefaultCompany();

                    if (company == null)
                    {
                        return NotFound(new
                        {
                            message = "Company not found."
                        });
                    }
                }

                company.CompanyCode = companyDto.CompanyCode;
                company.Name = companyDto.Name;
                company.ShortName = companyDto.ShortName;

                _adminService.SaveCompany(company);

                return Ok(new
                {
                    message = "Company saved successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.InnerException?.Message ?? ex.Message
                });
            }
        }
    }
}
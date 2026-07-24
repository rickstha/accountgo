using Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Services.Administration;
using Services.Security;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    public class AccountController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAdministrationService _administrationService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            IAdministrationService administrationService
            )
        {
            _userManager = userManager;
            _administrationService = administrationService;
        }

        [HttpPost]
        [Route("SignIn")]
        public async System.Threading.Tasks.Task<IActionResult> SignIn([FromBody] dynamic loginViewModel)
        {
            if (loginViewModel == null)
            {
                throw new System.ArgumentNullException(nameof(loginViewModel));
            }

            string password = loginViewModel.Password;
            string username = loginViewModel.Email;

            var user = await _userManager.FindByEmailAsync(username);
            if (user == null)
            {
                return new BadRequestObjectResult("Invalid login attempt.");
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                return new BadRequestObjectResult("User account is locked out.");
            }

            try
            {
                if (await _userManager.CheckPasswordAsync(user, password))
                {
                    await _userManager.ResetAccessFailedCountAsync(user);
                    return new ObjectResult(user);
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex.StackTrace);
            }

            if (_userManager.SupportsUserLockout)
            {
                await _userManager.AccessFailedAsync(user);
                if (await _userManager.IsLockedOutAsync(user))
                {
                    return new BadRequestObjectResult("User account is locked out.");
                }
            }

            return new BadRequestObjectResult("Invalid login attempt.");
        }

        [HttpPost]
        [Route("AddNewUser")]
        public async System.Threading.Tasks.Task<IActionResult> AddNewUser([FromBody]dynamic registerViewModel)
        {
            try
            {
                if (registerViewModel == null)
                {
                    throw new System.ArgumentNullException(nameof(registerViewModel));
                }

                string password = registerViewModel.Password;
                string username = registerViewModel.Email;
                string firstName = registerViewModel.FirstName;
                string lastName = registerViewModel.LastName;

                var user = new ApplicationUser { UserName = username, Email = username };
                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    Core.Domain.Security.User newUser =
                        new Core.Domain.Security.User
                        {
                            EmailAddress = username, 
                            UserName = username,
                            Firstname = firstName,
                            Lastname = lastName
                        };

                    _administrationService.SaveUser(newUser);

                    return new ObjectResult(result);
                }
                return new BadRequestObjectResult(result);
            }
            catch(System.Exception ex)
            {
                var errors = new[] { ex.InnerException != null ? ex.InnerException.Message : ex.Message };
                return new BadRequestObjectResult(errors);
            }
        }
    }
}

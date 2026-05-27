using Dto.Administration;
using Dto.Security;
using Microsoft.AspNetCore.Mvc;

namespace AccountGoWeb.Controllers
{
    //[Microsoft.AspNetCore.Authorization.Authorize]
    public class AdministrationController : BaseController
  {
    public AdministrationController(IConfiguration config)
    {
      _baseConfig = config;
      Models.SelectListItemHelper._config = config;
    }

    public async System.Threading.Tasks.Task<IActionResult> Company()
    {
      ViewBag.PageContentHeader = "Company";
      var model = await GetAsync<Company>("administration/company");
      if (model == null)
        model = new Company();
      return View(model);
    }

    [HttpPost]
    public async System.Threading.Tasks.Task<IActionResult> Company(Company model)
    {
      ViewBag.PageContentHeader = "Company";
      if (ModelState.IsValid)
      {
        var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(model);
        var content = new StringContent(serialize);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        var response = await PostAsync("administration/savecompany", content);

        return View(model);
      }
      return View(model);
    }

    public async System.Threading.Tasks.Task<IActionResult> Settings()
    {
      ViewBag.PageContentHeader = "Setup and Configuration";
      ViewBag.Accounts = Models.SelectListItemHelper.Accounts();
      var model = await GetAsync<GeneralLedgerSetting>("administration/settings");
      if (model == null)
        model = new GeneralLedgerSetting();
      return View(model);
    }

    [HttpPost]
    public async System.Threading.Tasks.Task<IActionResult> SaveSettings(Models.Financial.GeneralLedgerSetting model)
    {
      if (ModelState.IsValid)
      {
        var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(model);
        var content = new StringContent(serialize);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        await PostAsync("administration/savesettings", content);
      }
      ViewBag.Accounts = Models.SelectListItemHelper.Accounts();
      ViewBag.PageContentHeader = "Setup and Configuration";
      return RedirectToAction(nameof(AdministrationController.Settings));
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "SystemAdministrators")]
    public async System.Threading.Tasks.Task<IActionResult> Users()
    {
      var users = await GetAsync<System.Collections.Generic.IEnumerable<User>>("administration/users");
      ViewBag.PageContentHeader = "Users";
      return View(users);
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "SystemAdministrators")]
    public async System.Threading.Tasks.Task<IActionResult> Roles()
    {
      var roles = await GetAsync<System.Collections.Generic.IEnumerable<Role>>("administration/roles");
      ViewBag.PageContentHeader = "Security Roles";
      return View(roles);
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "SystemAdministrators")]
    public async System.Threading.Tasks.Task<IActionResult> Groups()
    {
      var groups = await GetAsync<System.Collections.Generic.IEnumerable<Group>>("administration/groups");
      ViewBag.PageContentHeader = "Security Groups";
      return View(groups);
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "SystemAdministrators")]
    public async System.Threading.Tasks.Task<IActionResult> AuditLogs()
    {
      var auditLogs = await GetAsync<System.Collections.Generic.IEnumerable<AuditLog>>("administration/auditlogs");

      ViewBag.PageContentHeader = "Audit Logs";
      return View(model: auditLogs);
    }

    [HttpGet]
    public IActionResult UserDetail(int id = 0)
    {
      if (id != 0)
      {
        ViewBag.PageContentHeader = "User";
      }
      else
      {
        ViewBag.PageContentHeader = "New User";
      }

      return View(new Models.Account.RegisterViewModel());
    }

    [HttpPost]
    public async System.Threading.Tasks.Task<IActionResult> UserDetail(Models.Account.RegisterViewModel model)
    {
      try
      {
        if (ModelState.IsValid)
        {
          var serialize = Newtonsoft.Json.JsonConvert.SerializeObject(model);
          var content = new StringContent(serialize);
          content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
          HttpResponseMessage responseAddNewUser = await Post("account/addnewuser", content);
          string responseContent = await responseAddNewUser.Content.ReadAsStringAsync();
          Newtonsoft.Json.Linq.JObject resultAddNewUser = Newtonsoft.Json.Linq.JObject.Parse(responseContent);

          if ((bool)resultAddNewUser["succeeded"]!)
          {
            return RedirectToAction(nameof(AdministrationController.Users), "Administration");
          }
          else
          {
            ModelState.AddModelError(string.Empty, resultAddNewUser["errors"]![0]!["description"]!.ToString());
            return View(model);
          }
        }
      }
      catch (Exception ex)
      {
        ModelState.AddModelError(string.Empty, "Please check if your database is ready/published." + ": " + ex.Message);
        return View(model);
      }
      ViewBag.PageContentHeader = "New User";
      return View(model);
    }
  }
}

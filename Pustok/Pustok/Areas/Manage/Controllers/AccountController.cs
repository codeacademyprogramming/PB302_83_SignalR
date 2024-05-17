using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Pustok.Areas.Manage.ViewModels;
using Pustok.Models;

namespace Pustok.Areas.Manage.Controllers
{
    [Area("manage")]
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> CreateAdmin()
        {
            AppUser admin = new AppUser
            {
                UserName = "admin",
                Email = "admin"
            };

            var r = await _userManager.CreateAsync(admin, "Admin123");
            await _userManager.AddToRoleAsync(admin,"super_admin");

            return Json(r);
        }

        public async Task<IActionResult> CreateRoles()
        {
            await _roleManager.CreateAsync(new IdentityRole("super_admin"));
            await _roleManager.CreateAsync(new IdentityRole("admin"));
            await _roleManager.CreateAsync(new IdentityRole("member"));

            return Ok();
        }
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(AdminLoginViewModel loginVM,string returnUrl)
        {
            AppUser admin = await _userManager.FindByNameAsync(loginVM.UserName);

            if (admin == null || (!await _userManager.IsInRoleAsync(admin, "admin") && !await _userManager.IsInRoleAsync(admin, "super_admin")))
            {
                ModelState.AddModelError("", "UserName or Password incorrect");
                return View();
            }


            var result = await _signInManager.PasswordSignInAsync(admin, loginVM.Password,loginVM.RememberMe,false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "UserName or Password incorrect");
                return View();
            }


            return returnUrl!=null?Redirect(returnUrl): RedirectToAction("index", "dashboard");
        }

        public IActionResult GetName()
        {
            var username = User.Identity.Name;

            return Json(User.Identity);
        }
    }
}

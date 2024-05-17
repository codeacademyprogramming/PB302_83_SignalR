using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit.Text;
using MimeKit;
using Pustok.Models;
using Pustok.ViewModels;
using System.Security.Claims;
using MailKit.Security;
using MailKit.Net.Smtp;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using NuGet.Common;
using Pustok.Services;

namespace Pustok.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly PustokDbContext _context;
        private readonly EmailService _emailService;

        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, PustokDbContext context, EmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailService = emailService;
        }
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(MemberRegisterViewModel registerVM)
        {
            if (!ModelState.IsValid) return View();

            if(_userManager.Users.Any(x=>x.NormalizedEmail == registerVM.Email.ToUpper()))
            {
                ModelState.AddModelError("Email", "Email is already taken");
                return View();
            }

            AppUser user = new AppUser
            {
                UserName = registerVM.UserName,
                Email = registerVM.Email,
                FullName = registerVM.FullName,
            };

            var result = await _userManager.CreateAsync(user, registerVM.Password);

            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                {
                    if(err.Code == "DuplicateUserName")
                        ModelState.AddModelError("UserName", "UserName is already taken");
                    else ModelState.AddModelError("", err.Description);
                }
                return View();
            }
            await _userManager.AddToRoleAsync(user, "member");


            return RedirectToAction("index", "home");
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(MemberLoginViewModel loginVM, string? returnUrl)
        {
            if ((!ModelState.IsValid)) return View();

            AppUser? user = await _userManager.FindByEmailAsync(loginVM.Email);

            if (user == null || !await _userManager.IsInRoleAsync(user,"member"))
            {
                ModelState.AddModelError("", "Email or Pasword incorrect!");
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(user, loginVM.Password, false, true);

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "You are locked out for 5 minutes!");
                return View();
            }
            else if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Email or Pasword incorrect!");
                return View();
            }

            return returnUrl != null ? Redirect(returnUrl) : RedirectToAction("index", "home");

        }

        [Authorize(Roles ="member")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("index", "home");
        }

        [Authorize(Roles = "member")]
        public async Task<IActionResult> Profile(string tab="dashboard")
        {
            AppUser? user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("login", "account");

            ProfileViewModel profileVM = new ProfileViewModel
            {
                ProfileEditVM = new ProfileEditViewModel
                {
                    FullName = user.FullName,
                    Email = user.Email,
                    UserName = user.UserName
                },
                Orders = _context.Orders.Include(x => x.OrderItems).ThenInclude(oi => oi.Book)
                        .OrderByDescending(x=>x.CreatedAt).Where(x => x.AppUserId == user.Id).ToList()
                         
            };

			ViewBag.Tab = tab;

			return View(profileVM);
        }

        [Authorize(Roles ="member")]
        [HttpPost]
		public async Task<IActionResult> Profile(ProfileEditViewModel profileEditVM, string tab="profile")
        {
            ViewBag.Tab = tab;
            ProfileViewModel profileVM = new ProfileViewModel();
            profileVM.ProfileEditVM = profileEditVM;

			if (!ModelState.IsValid) return View(profileVM);

            AppUser? user = await _userManager.GetUserAsync(User);

            if (user == null) return RedirectToAction("login", "account");

            user.UserName = profileEditVM.UserName;
            user.Email = profileEditVM.Email;
            user.FullName = profileEditVM.FullName;

            if(_userManager.Users.Any(x=>x.Id!=User.FindFirstValue(ClaimTypes.NameIdentifier) && x.NormalizedEmail == profileEditVM.Email.ToUpper()))
            {
				ModelState.AddModelError("Email", "Email is already taken");
                return View(profileVM);
			}

            if(profileEditVM.NewPassword != null)
            {
               var passwordResult = await _userManager.ChangePasswordAsync(user, profileEditVM.CurrentPassword, profileEditVM.NewPassword);
                
                if(!passwordResult.Succeeded)
                {
                    foreach (var err in passwordResult.Errors)
                        ModelState.AddModelError("", err.Description);

                    return View(profileVM);
                }
            }


			var result = await _userManager.UpdateAsync(user);

			if (!result.Succeeded)
			{
				foreach (var err in result.Errors)
				{
					if (err.Code == "DuplicateUserName")
						ModelState.AddModelError("UserName", "UserName is already taken");
					else ModelState.AddModelError("", err.Description);
				}
				return View(profileVM);
			}

            await _signInManager.SignInAsync(user, false);

            return View(profileVM);
		}

        public IActionResult ForgetPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ForgetPassword(ForgetPasswordViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);   

            AppUser? user = _userManager.FindByEmailAsync(vm.Email).Result;

            if(user == null || !_userManager.IsInRoleAsync(user, "member").Result)
            {
                ModelState.AddModelError("", "Account is not exist");
                return View();
            }

            var token = _userManager.GeneratePasswordResetTokenAsync(user).Result;


            var url = Url.Action("verify", "account", new { email = vm.Email,token = token }, Request.Scheme);
            TempData["EmailSent"] = vm.Email;

            var subject = "Reset Password Link";
            var body = $"<h1>Click <a href=\"{url}\">here</a> to reset your password</h1>";

            _emailService.Send(user.Email, subject, body);


            return View();
        }

        public IActionResult Verify(string email,string token)
        {
            AppUser? user = _userManager.FindByEmailAsync(email).Result;

            if (user == null || !_userManager.IsInRoleAsync(user, "member").Result)
            {
                return RedirectToAction("notfound", "error");
            }

            if (!_userManager.VerifyUserTokenAsync(user, _userManager.Options.Tokens.PasswordResetTokenProvider, "ResetPassword", token).Result)
            {
                return RedirectToAction("notfound", "error");
            }

            TempData["email"] = email;
            TempData["token"] = token;

            return RedirectToAction("resetPassword");
        }

        public IActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordViewModel vm)
        {
            TempData["email"] = vm.Email;
            TempData["token"] = vm.Token;

            if (!ModelState.IsValid) return View(vm);

            AppUser? user = _userManager.FindByEmailAsync(vm.Email).Result;

            if (user == null || !_userManager.IsInRoleAsync(user, "member").Result)
            {
                ModelState.AddModelError("", "Account is not exist");
                return View();
            }

            if (!_userManager.VerifyUserTokenAsync(user, _userManager.Options.Tokens.PasswordResetTokenProvider, "ResetPassword", vm.Token).Result)
            {
                ModelState.AddModelError("", "Account is not exist");
                return View();
            }

            var result = _userManager.ResetPasswordAsync(user, vm.Token, vm.NewPassword).Result;

            if (!result.Succeeded)
            {
                foreach (var item in result.Errors)
                {
                    ModelState.AddModelError("", item.Description);
                }
                return View();
            }

            return RedirectToAction("login");
        }

        public IActionResult Users()
        {
            var users = _userManager.Users.ToList();

            return View(users);
        }

    }
}

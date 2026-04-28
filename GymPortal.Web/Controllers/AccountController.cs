using GymPortal.Application.DTOs;
using GymPortal.Application.Interfaces.Services;
using GymPortal.Domain.Entities;
using GymPortal.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymPortal.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUserService _userService;
        private readonly IMembershipService _membershipService;
        private readonly IBookingService _bookingService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IUserService userService,
            IMembershipService membershipService,
            IBookingService bookingService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userService = userService;
            _membershipService = membershipService;
            _bookingService = bookingService;
        }

        // GET: /Account/SignUp
        [HttpGet]
        public IActionResult SignUp() => View(new SignUpViewModel());

        // POST: /Account/SignUpStep1 (email only, as per design)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SignUpStep1(string email)
        {
            if (!ModelState.IsValid || string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("Email", "Email is required.");
                return View("SignUp", new SignUpViewModel());
            }
            TempData["SignUpEmail"] = email;
            ViewData["Step2"] = true;
            return View("SignUp", new SignUpViewModel { Email = email });
        }

        // POST: /Account/SignUp (full registration)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUp(SignUpViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber,
                    DateOfBirth = null // not collected in signup form currently
                };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Member");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }
            // If we got this far, something failed, redisplay form
            return View(model);
        }

        // GET: /Account/SignIn
        [HttpGet]
        public IActionResult SignIn(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new SignInViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/SignIn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignIn(SignInViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return RedirectToLocal(returnUrl);
                }
                if (result.IsLockedOut)
                {
                    return View("Lockout");
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }

        // POST: /Account/SignOut
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> SignOut()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // External login (Google)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
                return View(nameof(SignIn));
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return RedirectToAction(nameof(SignIn));

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
            if (result.Succeeded)
                return RedirectToLocal(returnUrl);
            if (result.IsLockedOut)
                return View("Lockout");

            // Otherwise prompt user to create an account
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["ProviderDisplayName"] = info.ProviderDisplayName;
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var name = info.Principal.FindFirstValue(ClaimTypes.Name);
            return View("ExternalLoginConfirmation", new ExternalLoginConfirmationDto { Email = email, Name = name });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationDto model, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                    return View("Error");

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.Name ?? "",
                    LastName = ""
                };
                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Member");
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return RedirectToLocal(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        // GET: /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }

                // Generate password reset token
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.Action(nameof(ResetPassword), "Account", new { userId = user.Id, code = code }, protocol: Request.Scheme);

                // TODO: Send email with callbackUrl
                // await _emailSender.SendEmailAsync(model.Email, "Reset Password", 
                //    $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");

                TempData["SuccessMessage"] = "Password reset link has been sent to your email.";
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation() => View();

        // GET: /Account/ResetPassword
        [HttpGet]
        public IActionResult ResetPassword(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return BadRequest("Invalid password reset request.");
            }

            var model = new ResetPasswordViewModel
            {
                UserId = userId,
                Code = code
            };
            return View(model);
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation() => View();

        // GET: /Account/MyAccount
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyAccount()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound();

            var viewModel = new MyAccountViewModel
            {
                User = user,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth
            };
            return View(viewModel);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MyAccount(MyAccountViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                string? profileImageUrl = null;
                // Handle file upload (simplified: you'd save the file and store path)
                if (model.ProfileImage != null && model.ProfileImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                    Directory.CreateDirectory(uploadsFolder);
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ProfileImage.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await model.ProfileImage.CopyToAsync(stream);
                    profileImageUrl = "/uploads/" + uniqueFileName;
                }

                var result = await _userService.UpdateUserProfileAsync(userId, model.FirstName, model.LastName, model.PhoneNumber, model.DateOfBirth, profileImageUrl);
                if (result.IsSuccess)
                {
                    TempData["Success"] = "Profile updated successfully.";
                    return RedirectToAction(nameof(MyAccount));
                }
                ModelState.AddModelError(string.Empty, result.Error!);
            }
            return View(model);
        }

        // GET: /Account/MyMembership
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyMembership()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userService.GetUserByIdAsync(userId);
            return View(user ?? new UserDto());
        }

        // POST: /Account/DeleteAccount
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = _userManager.GetUserId(User);
            var result = await _userService.DeleteUserAsync(userId);
            if (result.IsSuccess)
            {
                await _signInManager.SignOutAsync();
                TempData["Success"] = "Your account has been deleted.";
                return RedirectToAction("Index", "Home");
            }
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(MyAccount));
        }

        // GET: /Account/MyBookings
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyBookings()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToAction("SignIn");
            }

            var bookings = await _bookingService.GetUserBookingsWithClassAsync(userId); // Use the method that loads ClassSession

            var bookingDtos = new List<BookingDto>();
            foreach (var booking in bookings)
            {
                // Add null checks for all navigation properties
                if (booking?.ClassSession != null)
                {
                    bookingDtos.Add(new BookingDto
                    {
                        Id = booking.Id,
                        ClassSessionId = booking.ClassSessionId,
                        ProgramTitle = booking.ClassSession.TrainingProgram.Title ?? "Unknown Class",
                        InstructorName = booking.ClassSession.InstructorName ?? "Unknown Instructor",
                        Location = booking.ClassSession.Location ?? "Unknown Location",
                        StartTime = booking.ClassSession.StartTime,
                        EndTime = booking.ClassSession.EndTime,
                        Status = booking.Status,
                        BookingTime = booking.BookingTime
                    });
                }
            }

            var user = await _userService.GetUserByIdAsync(userId);
            var viewModel = new MyBookingsViewModel
            {
                User = user ?? new UserDto(),
                Bookings = bookingDtos
            };
            return View(viewModel);
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            else
                return RedirectToAction("Index", "Home");
        }
    }
}
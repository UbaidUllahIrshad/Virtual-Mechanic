using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using VirtualMechanic.Core.ViewModels;
using VirtualMechanic.Core.Models;
using VirtualMechanic.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace VirtualMechanic.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Register() => View(new RegisterRoleViewModel());

        [HttpPost]
        public IActionResult Register(RegisterRoleViewModel model)
        {
            if (model.SelectedRole == "Client") return View("RegisterClient", new RegisterBaseViewModel());
            if (model.SelectedRole == "Mechanic") return View("RegisterMechanic", new RegisterMechanicViewModel());
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> RegisterClient(RegisterBaseViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = model.Email, Email = model.Email, PhoneNumber = model.Phone };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    if (!await _roleManager.RoleExistsAsync("Client")) await _roleManager.CreateAsync(new IdentityRole("Client"));
                    await _userManager.AddToRoleAsync(user, "Client");

                    var appUser = new User
                    {
                        Name = model.Name,
                        Email = model.Email,
                        Phone = model.Phone,
                        PasswordHash = "ManagedByIdentity",
                        Role = "Client",
                        ServiceRequests = new List<ServiceRequest>()
                    };
                    _context.AppUsers.Add(appUser); 
                    await _context.SaveChangesAsync();

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("RequestService", "Client");
                }
                foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> RegisterMechanic(RegisterMechanicViewModel model)
        {

            if (model.SelectedSpecialties == null || model.SelectedSpecialties.Count == 0)
            {
                ModelState.AddModelError("SelectedSpecialties", "Please select at least one specialty.");
            }

            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = model.Email, Email = model.Email, PhoneNumber = model.Phone };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    if (!await _roleManager.RoleExistsAsync("Mechanic")) await _roleManager.CreateAsync(new IdentityRole("Mechanic"));
                    await _userManager.AddToRoleAsync(user, "Mechanic");

                    string combinedSpecialties = string.Join(",", model.SelectedSpecialties);
                    var random = new Random();
                    double latOffset = (random.NextDouble() * 0.1) - 0.05;
                    double lngOffset = (random.NextDouble() * 0.1) - 0.05;

                    var mechanic = new Mechanic
                    {
                        Email = model.Email,
                        Name = model.Name,
                        Phone = model.Phone,
                        Specialty = combinedSpecialties,
                        Status = "Offline", 
                        PasswordHash = "ManagedByIdentity",
                        AssignedRequests = new List<ServiceRequest>(),
                        Latitude = 31.5204 + latOffset,
                        Longitude = 74.3587 + lngOffset
                    };
                    _context.Mechanics.Add(mechanic);
                    await _context.SaveChangesAsync();

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Dashboard", "Mechanic");
                }
                foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError(string.Empty, "Required fields missing.");
                return View();
            }
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(email, password, isPersistent: false, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(email);
                    if (user != null)
                    {
                        // Direct based on role
                        if (await _userManager.IsInRoleAsync(user, "Admin")) return RedirectToAction("Dashboard", "Admin");
                        if (await _userManager.IsInRoleAsync(user, "Mechanic")) return RedirectToAction("Dashboard", "Mechanic");
                        if (await _userManager.IsInRoleAsync(user, "Client")) return RedirectToAction("RequestService", "Client");
                    }

                    // Fallback redirect
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null && await _userManager.IsInRoleAsync(user, "Mechanic"))
            {
                var mechanic = await _context.Mechanics
                    .FirstOrDefaultAsync(m => m.Email == user.Email);

                if (mechanic != null)
                {
                    mechanic.Status = "Offline";
                    await _context.SaveChangesAsync();
                }
            }

            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
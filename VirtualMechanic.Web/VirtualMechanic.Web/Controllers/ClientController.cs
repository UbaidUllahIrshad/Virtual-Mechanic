using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VirtualMechanic.Core.Interfaces;
using VirtualMechanic.Core.Models;
using VirtualMechanic.Data;
using VirtualMechanic.Core.ViewModels;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace VirtualMechanic.Web.Controllers
{
    [Authorize(Roles = "Client")]
    public class ClientController : Controller
    {
        private readonly IRequestService _requestService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ClientController(
            IRequestService requestService,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context)
        {
            _requestService = requestService;
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult RequestService()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SubmitRequest(RequestSubmissionViewModel model)
        {
            if (ModelState.IsValid)
            {
                var identityUser = await _userManager.GetUserAsync(User);
                if (identityUser == null) return RedirectToAction("Login", "Account");

                var appUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == identityUser.Email);

                if (appUser == null)
                {
                    ModelState.AddModelError("", "User profile not found. Please contact support.");
                    return View("RequestService", model);
                }

                var request = await _requestService.SubmitNewRequestAsync(model, appUser.UserId);

                ViewBag.Message = $"Your request (ID: {request.Id}) has been submitted!";
                return View("RequestConfirmation");
            }

            return View("RequestService", model);
        }

        [HttpGet]
        public async Task<IActionResult> TrackStatus()
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null) return RedirectToAction("Login", "Account");

            var appUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == identityUser.Email);

            if (appUser == null) return View("Error");

            var myRequests = await _requestService.GetClientRequestsAsync(appUser.UserId);

            return View(myRequests);
        }

        [HttpPost]
        public async Task<IActionResult> CancelRequest(int requestId)
        {
            await _requestService.CancelJobAsync(requestId);
            return RedirectToAction("TrackStatus");
        }

        [HttpGet]
        public async Task<IActionResult> GetMechanicLocation(int requestId)
        {
            var request = await _context.ServiceRequests
                .Include(r => r.Mechanic)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request != null)
            {
               
                if (request.Status != "Assigned")
                {
                    return Json(new { success = false, status = request.Status });
                }

                if (request.Mechanic != null)
                {
                    return Json(new
                    {
                        success = true,
                        status = "Assigned",
                        lat = request.Mechanic.Latitude,
                        lng = request.Mechanic.Longitude,
                        name = request.Mechanic.Name,
                        distance = request.Distance, 
                        eta = request.ETA 
                    });
                }
            }
            return Json(new { success = false });
        }
    }
}
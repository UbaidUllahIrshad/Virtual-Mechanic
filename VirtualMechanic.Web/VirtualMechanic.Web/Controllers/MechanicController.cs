using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VirtualMechanic.Core.Models;
using VirtualMechanic.Data;
using VirtualMechanic.Core.ViewModels;
using VirtualMechanic.Core.Interfaces;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;

namespace VirtualMechanic.Web.Controllers
{
    [Authorize(Roles = "Mechanic")]
    public class MechanicController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IRequestService _requestService;

        public MechanicController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IRequestService requestService)
        {
            _context = context;
            _userManager = userManager;
            _requestService = requestService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var mechanic = await _context.Mechanics.FirstOrDefaultAsync(m => m.Email == user.Email);
            if (mechanic == null) return View("Error");

            var activeJobRequest = await _context.ServiceRequests
                .Include(r => r.User)
                .Where(r => r.MechanicId == mechanic.MechanicId && r.Status == "Assigned")
                .FirstOrDefaultAsync();

            RequestDashboardItemViewModel? activeJobVM = null;
            if (activeJobRequest != null)
            {
                activeJobVM = new RequestDashboardItemViewModel
                {
                    RequestId = activeJobRequest.Id,
                    ClientName = activeJobRequest.User!.Name,
                    ClientPhone = activeJobRequest.User!.Phone,
                    Problem = activeJobRequest.ServiceType,
                    ProblemDescription = activeJobRequest.ProblemDescription,
                    Status = activeJobRequest.Status,
                    Distance = activeJobRequest.Distance,
                    ETA = activeJobRequest.ETA,
                    ClientLatitude = activeJobRequest.Latitude,
                    ClientLongitude = activeJobRequest.Longitude
                };

                ViewBag.ClientLat = activeJobRequest.Latitude;
                ViewBag.ClientLng = activeJobRequest.Longitude;
            }

            ViewBag.ActiveJob = activeJobVM;

            var offers = new List<RequestDashboardItemViewModel>();
            if (activeJobVM == null)
            {
                offers = await _requestService.GetPendingOffersForMechanicAsync(mechanic.MechanicId);
            }

            return View(offers);
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardState()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var mechanic = await _context.Mechanics.FirstOrDefaultAsync(m => m.Email == user.Email);
            if (mechanic == null) return NotFound();

            // 1. Check Active Job
            var activeJobRequest = await _context.ServiceRequests
                .Include(r => r.User)
                .Where(r => r.MechanicId == mechanic.MechanicId && r.Status == "Assigned")
                .FirstOrDefaultAsync();

            if (activeJobRequest != null)
            {
                return Json(new
                {
                    hasActiveJob = true,
                    jobData = new
                    {
                        requestId = activeJobRequest.Id,
                        clientName = activeJobRequest.User!.Name,
                        clientPhone = activeJobRequest.User!.Phone,
                        problem = activeJobRequest.ServiceType,
                        notes = activeJobRequest.ProblemDescription,
                        distance = activeJobRequest.Distance,
                        eta = activeJobRequest.ETA,
                        clientLat = activeJobRequest.Latitude,
                        clientLng = activeJobRequest.Longitude
                    }
                });
            }

            var offers = await _requestService.GetPendingOffersForMechanicAsync(mechanic.MechanicId);
            return Json(new
            {
                hasActiveJob = false,
                offers = offers
            });
        }

        [HttpPost]
        public async Task<IActionResult> AcceptJob(int requestId)
        {
            var user = await _userManager.GetUserAsync(User);
            var mechanic = await _context.Mechanics.FirstOrDefaultAsync(m => m.Email == user.Email);
            if (mechanic != null) await _requestService.AcceptJobAsync(requestId, mechanic.MechanicId);
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> ArrivedAtLocation(int requestId)
        {
            var request = await _context.ServiceRequests.FindAsync(requestId);
            if (request != null)
            {
                request.TravelCost = 100;
                request.TotalCost = request.ServiceCost + request.TravelCost;
                request.Distance = "0.00 km (Arrived)";
                request.ETA = $"ARRIVED | Total: Rs. {request.TotalCost:F0}";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> CompleteJob(int requestId)
        {
            var request = await _context.ServiceRequests.FindAsync(requestId);
            if (request != null)
            {
                request.Status = "Completed";
                var mechanic = await _context.Mechanics.FindAsync(request.MechanicId);
                if (mechanic != null) mechanic.Status = "Available";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> CancelJob(int requestId)
        {
            await _requestService.CancelJobAsync(requestId);
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateLocation(double lat, double lng)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            var mechanic = await _context.Mechanics.FirstOrDefaultAsync(m => m.Email == user.Email);

            if (mechanic != null)
            {
                mechanic.Latitude = lat;
                mechanic.Longitude = lng;

                if (mechanic.Status != "Busy") mechanic.Status = "Available";

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpGet] public async Task<IActionResult> Profile() { return View(); }
        [HttpPost] public async Task<IActionResult> UpdateProfile(MechanicProfileViewModel model) { return RedirectToAction("Profile"); }
    }
}
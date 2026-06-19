using Microsoft.AspNetCore.Mvc;
using VirtualMechanic.Core.ViewModels;
using VirtualMechanic.Core.Interfaces;
using VirtualMechanic.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using VirtualMechanic.Core.Models; 

namespace VirtualMechanic.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IRequestService _requestService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(IRequestService requestService, ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _requestService = requestService;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var requests = await _requestService.GetAllPendingAndAssignedRequestsAsync();

            var activeRequests = requests
                .Where(r => !r.Status.Contains("Cancelled"))
                .ToList();

            var mechanics = await _context.Mechanics.Where(m => m.Status == "Available").ToListAsync();
            ViewBag.AvailableMechanics = mechanics;

            return View(activeRequests);
        }

        [HttpPost]
        public async Task<IActionResult> AssignMechanic(int requestId, int mechanicId)
        {
            if (mechanicId == 0) return RedirectToAction("Dashboard");
            var success = await _requestService.AssignMechanicAsync(requestId, mechanicId);

            if (!success) TempData["Error"] = "Failed to assign mechanic.";
            else TempData["Success"] = "Mechanic assigned successfully!";

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> CancelRequest(int id)
        {
            var request = await _context.ServiceRequests.FindAsync(id);
            if (request != null)
            {
                if (request.MechanicId.HasValue)
                {
                    var mechanic = await _context.Mechanics.FindAsync(request.MechanicId);
                    if (mechanic != null) mechanic.Status = "Available";
                }

                request.Status = "Cancelled (Admin)";
                request.MechanicId = null;

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Request #{id} cancelled.";
            }
            else
            {
                TempData["Error"] = "Request not found.";
            }
            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> ManageMechanics()
        {
            var mechanics = await _context.Mechanics.ToListAsync();
            return View(mechanics);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMechanic(int id)
        {
            var mechanic = await _context.Mechanics.FindAsync(id);
            if (mechanic != null)
            {
                var assignedRequests = await _context.ServiceRequests
                    .Where(r => r.MechanicId == id)
                    .ToListAsync();

                foreach (var request in assignedRequests)
                {
                    request.MechanicId = null;
                    if (request.Status == "Assigned") request.Status = "Pending";
                }
                await _context.SaveChangesAsync();

                var user = await _userManager.FindByEmailAsync(mechanic.Email);
                if (user != null) await _userManager.DeleteAsync(user);

                _context.Mechanics.Remove(mechanic);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Mechanic deleted successfully.";
            }
            return RedirectToAction("ManageMechanics");
        }
    }
}
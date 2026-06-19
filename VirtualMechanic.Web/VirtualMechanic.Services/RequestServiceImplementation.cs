using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using VirtualMechanic.Core.Interfaces;
using VirtualMechanic.Core.Models;
using VirtualMechanic.Data;
using VirtualMechanic.Core.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace VirtualMechanic.Services
{
    public static class Extensions
    {
        public static double DegreesToRadians(this double degrees)
        {
            return degrees * (Math.PI / 180.0);
        }
    }

    public class RequestService : IRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;

        private const string ORS_API_KEY = "eyJvcmciOiI1YjNjZTM1OTc4NTExMTAwMDFjZjYyNDgiLCJpZCI6IjQzMDkyZjBmMjExYjY3YTA5YjBkYTFjMmU1NDgwODMzMWVmMmQzOWNjODJlM2UzMjgyMDc3NjExIiwiaCI6Im11cm11cjY0In0=";

        public RequestService(ApplicationDbContext context, HttpClient httpClient)
        {
            _context = context;
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ORS_API_KEY);
        }
        private class RouteInfo
        {
            public double DistanceKm { get; set; }
            public string DurationText { get; set; }
        }
        private decimal GetServiceBasePrice(string serviceType)
        {
            return serviceType switch
            {
                "Flat Tyre" => 300M,
                "Dead Battery" => 500M,
                "Engine Heat" => 1000M,
                "Towing" => 2000M,
                _ => 500M,
            };
        }

        private decimal GetTravelFee(double distanceKm)
        {
            if (distanceKm < 0.1) return 0M; 
            if (distanceKm <= 2.0) return 100M;
            if (distanceKm <= 5.0) return 200M;
            if (distanceKm <= 10.0) return 350M;
            return 500M;
        }

        private async Task<RouteInfo> GetRouteDataAsync(double startLat, double startLng, double endLat, double endLng)
        {
            double mathDist = CalculateMathDistance(startLat, startLng, endLat, endLng);
            var fallbackResult = new RouteInfo
            {
                DistanceKm = mathDist,
                DurationText = CalculateMathETA(mathDist)
            };

            try
            {
                string start = $"{startLng.ToString(CultureInfo.InvariantCulture)},{startLat.ToString(CultureInfo.InvariantCulture)}";
                string end = $"{endLng.ToString(CultureInfo.InvariantCulture)},{endLat.ToString(CultureInfo.InvariantCulture)}";

                string url = $"https://api.openrouteservice.org/v2/directions/cycling-regular?start={start}&end={end}";


                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return fallbackResult;

                var body = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(body);

                var summary = json["features"]?[0]?["properties"]?["summary"];
                if (summary == null) return fallbackResult;

                double distMeters = (double)summary["distance"];
                double apiKm = distMeters / 1000.0;

                double durationSeconds = (double)summary["duration"];
                double minutes = durationSeconds / 60.0;
                string apiEta = (apiKm < 0.1) ? "Arriving Now" : $"{(int)minutes} mins";

                if (apiKm > (mathDist * 3.5)) return fallbackResult;

                return new RouteInfo
                {
                    DistanceKm = apiKm,
                    DurationText = apiEta
                };
            }
            catch
            {
                return fallbackResult;
            }
        }

        private double CalculateMathDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;
            var dLat = (lat2 - lat1).DegreesToRadians();
            var dLon = (lon2 - lon1).DegreesToRadians();
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1.DegreesToRadians()) * Math.Cos(lat2.DegreesToRadians()) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return (R * c) * 1.25;
        }

        private string CalculateMathETA(double distanceKm)
        {
            if (distanceKm < 0.1) return "Arriving Now";
            double minutes = (distanceKm / 30.0) * 60.0;
            return $"{(int)(minutes + 2)} mins";
        }
        public async Task<ServiceRequest> SubmitNewRequestAsync(RequestSubmissionViewModel model, int userId)
        {
            decimal serviceFee = GetServiceBasePrice(model.ServiceType);
            var newRequest = new ServiceRequest
            {
                UserId = userId,
                ProblemDescription = model.ProblemDescription,
                ServiceType = model.ServiceType,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                Status = "Pending",
                MechanicId = null,
                Distance = "Waiting...",
                ETA = "Calculating...",
                ServiceCost = serviceFee,
                TravelCost = 0,
                TotalCost = serviceFee,
                RequestTime = DateTime.UtcNow
            };
            _context.ServiceRequests.Add(newRequest);
            await _context.SaveChangesAsync();
            return newRequest;
        }

        public async Task<List<RequestDashboardItemViewModel>> GetPendingOffersForMechanicAsync(int mechanicId)
        {
            var mechanic = await _context.Mechanics.FindAsync(mechanicId);
            if (mechanic == null) return new List<RequestDashboardItemViewModel>();

            var pendingJobs = await _context.ServiceRequests
                .Include(r => r.User)
                .Where(r => r.Status == "Pending")
                .ToListAsync();

            var offers = new List<RequestDashboardItemViewModel>();

            foreach (var job in pendingJobs)
            {
                bool isMatch = mechanic.Specialty == "General" || job.ServiceType == "Other" || mechanic.Specialty.Contains(job.ServiceType);
                if (!isMatch) continue;

                RouteInfo route = await GetRouteDataAsync(mechanic.Latitude, mechanic.Longitude, job.Latitude, job.Longitude);

                decimal travelFee = GetTravelFee(route.DistanceKm);
                decimal totalEarnings = job.ServiceCost + travelFee;

                offers.Add(new RequestDashboardItemViewModel
                {
                    RequestId = job.Id,
                    ClientName = job.User!.Name,
                    ClientPhone = job.User!.Phone,
                    Problem = job.ServiceType,
                    ProblemDescription = job.ProblemDescription,
                    Status = "Offer",
                    Distance = $"{route.DistanceKm:F2} km",
                    ETA = route.DurationText,
                    RequestTime = job.RequestTime,
                    ClientLatitude = job.Latitude,
                    ClientLongitude = job.Longitude
                });
            }
            return offers.OrderBy(o => double.Parse(o.Distance.Split(' ')[0])).ToList();
        }

        public async Task<bool> AcceptJobAsync(int requestId, int mechanicId)
        {
            var request = await _context.ServiceRequests.FindAsync(requestId);
            var mechanic = await _context.Mechanics.FindAsync(mechanicId);

            if (request == null || mechanic == null) return false;
            if (request.Status != "Pending") return false;

            RouteInfo route = await GetRouteDataAsync(mechanic.Latitude, mechanic.Longitude, request.Latitude, request.Longitude);

            decimal travelFee = GetTravelFee(route.DistanceKm);

            request.MechanicId = mechanicId;
            request.Status = "Assigned";
            request.TravelCost = travelFee;
            request.TotalCost = request.ServiceCost + travelFee;
            request.Distance = $"{route.DistanceKm:F2} km";
            request.ETA = route.DurationText;

            mechanic.Status = "Busy";
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelJobAsync(int requestId)
        {
            var request = await _context.ServiceRequests.FindAsync(requestId);
            if (request == null) return false;
            if (request.MechanicId.HasValue)
            {
                var mechanic = await _context.Mechanics.FindAsync(request.MechanicId);
                if (mechanic != null) mechanic.Status = "Available";
            }
            request.Status = "Cancelled";
            request.MechanicId = null;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RecalculateBillAsync(int requestId, double finalDistanceKm)
        {
            var request = await _context.ServiceRequests.FindAsync(requestId);
            if (request == null) return false;
            request.TravelCost = GetTravelFee(finalDistanceKm);
            request.TotalCost = request.ServiceCost + request.TravelCost;
            request.Distance = $"{finalDistanceKm:F2} km";
            request.ETA = $"Arrived | Bill: Rs. {request.TotalCost:F0}";
            await _context.SaveChangesAsync();
            return true;
        }

        private RequestDashboardItemViewModel MapToViewModel(ServiceRequest r)
        {
            return new RequestDashboardItemViewModel
            {
                RequestId = r.Id,
                ClientName = r.User?.Name ?? "Unknown",
                ClientPhone = r.User?.Phone ?? "N/A",
                Problem = r.ServiceType,
                ProblemDescription = r.ProblemDescription,
                Status = r.Status,
                Distance = r.Distance,
                ETA = r.ETA,
                MechanicName = r.Mechanic?.Name ?? "Not Assigned",
                MechanicPhone = r.Mechanic?.Phone ?? "N/A",
                RequestTime = r.RequestTime,
                ClientLatitude = r.Latitude,
                ClientLongitude = r.Longitude
            };
        }

        public async Task<List<RequestDashboardItemViewModel>> GetAllPendingAndAssignedRequestsAsync()
        {
            var requests = await _context.ServiceRequests.Include(r => r.User).Include(r => r.Mechanic).Where(r => r.Status != "Completed" && r.Status != "Cancelled").OrderByDescending(r => r.RequestTime).ToListAsync();
            return requests.Select(MapToViewModel).ToList();
        }

        public async Task<List<RequestDashboardItemViewModel>> GetClientRequestsAsync(int userId)
        {
            var requests = await _context.ServiceRequests.Include(r => r.User).Include(r => r.Mechanic).Where(r => r.UserId == userId && r.Status != "Completed" && r.Status != "Cancelled").OrderByDescending(r => r.RequestTime).ToListAsync();
            return requests.Select(MapToViewModel).ToList();
        }

        public async Task<RequestDashboardItemViewModel?> GetRequestStatusAsync(int userId)
        {
            var request = await _context.ServiceRequests.Include(r => r.User).Include(r => r.Mechanic).FirstOrDefaultAsync(r => r.UserId == userId && r.Status != "Completed" && r.Status != "Cancelled");
            return request == null ? null : MapToViewModel(request);
        }

        public async Task<bool> AssignMechanicAsync(int requestId, int mechanicId)
        {
            var req = await _context.ServiceRequests.FindAsync(requestId);
            var mech = await _context.Mechanics.FindAsync(mechanicId);
            if (req == null || mech == null) return false;
            req.MechanicId = mechanicId;
            req.Status = "Assigned";
            mech.Status = "Busy";
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> TryAssignPendingJobToMechanicAsync(int mechanicId)
        {
            return false;
        }
    }
}
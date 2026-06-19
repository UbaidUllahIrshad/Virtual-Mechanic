using System.Collections.Generic;
using System.Threading.Tasks;
using VirtualMechanic.Core.Models;
using VirtualMechanic.Core.ViewModels;

namespace VirtualMechanic.Core.Interfaces
{
    public interface IRequestService
    {

        Task<ServiceRequest> SubmitNewRequestAsync(RequestSubmissionViewModel model, int userId);

        Task<List<RequestDashboardItemViewModel>> GetClientRequestsAsync(int userId);

        Task<RequestDashboardItemViewModel?> GetRequestStatusAsync(int userId);

        Task<List<RequestDashboardItemViewModel>> GetAllPendingAndAssignedRequestsAsync();

        Task<List<RequestDashboardItemViewModel>> GetPendingOffersForMechanicAsync(int mechanicId);

        Task<bool> AssignMechanicAsync(int requestId, int mechanicId);
        Task<bool> AcceptJobAsync(int requestId, int mechanicId);
        Task<bool> CancelJobAsync(int requestId);

        Task<bool> RecalculateBillAsync(int requestId, double finalDistanceKm);

        Task<bool> TryAssignPendingJobToMechanicAsync(int mechanicId);
    }
}
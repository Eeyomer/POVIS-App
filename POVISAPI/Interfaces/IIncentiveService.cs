using POVISAPI.Models.DTO;

namespace POVISAPI.Interfaces
{
    public interface IIncentiveService
    {
        /// <param name="forcedOfficerId">When set (Apprehending Officer portal) the result is limited to that officer, whatever the filter says.</param>
        Task<IncentiveListDTO> GetAsync(IncentiveFilterDTO filter, Guid? forcedOfficerId);
    }
}

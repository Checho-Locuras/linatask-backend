using LinaTask.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Application.Services.Interfaces
{
    public interface IOfferService
    {
        Task<IEnumerable<OfferDto>> GetAllOffersAsync();
        Task<IEnumerable<OfferDto>> GetOffersByTaskAsync(Guid taskId);
        Task<IEnumerable<OfferDto>> GetOffersByTeacherAsync(Guid teacherId);
        Task<OfferDto?> GetOfferByIdAsync(Guid id);
        Task<OfferDto> CreateOfferAsync(CreateOfferDto createOfferDto);
        Task<OfferDto> UpdateOfferAsync(Guid id, UpdateOfferDto updateOfferDto);
        Task<bool> DeleteOfferAsync(Guid id);
    }
}

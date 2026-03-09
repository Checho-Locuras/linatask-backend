namespace LinaTask.Application.Services.Interfaces
{
    public interface IAdminUserService
    {
        Task PromoteToAdminAsync(Guid userId);
        Task RemoveAdminAsync(Guid userId);
    }
}

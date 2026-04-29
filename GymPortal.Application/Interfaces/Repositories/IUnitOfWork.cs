namespace GymPortal.Application.Interfaces.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IBaseRepository<T> Repository<T>() where T : Domain.Common.BaseEntity;
        Task<int> CompleteAsync();
    }
}

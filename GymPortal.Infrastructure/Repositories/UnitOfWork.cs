using GymPortal.Application.Interfaces.Repositories;
using GymPortal.Domain.Common;
using GymPortal.Infrastructure.Data;
using System.Collections;


namespace GymPortal.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private Hashtable _repositories;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }
        public IBaseRepository<T> Repository<T>() where T : BaseEntity
        {
            _repositories ??= new Hashtable();
            var type = typeof(T).Name;
            if (!_repositories.ContainsKey(type))
            {
                var repositoryType = typeof(BaseRepository<>);
                var repositoryInstance = Activator.CreateInstance(repositoryType.MakeGenericType(typeof(T)), _context);
                _repositories.Add(type, repositoryInstance);
            }
            return (IBaseRepository<T>)_repositories[type];
        }
        public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();

        public void Dispose() => _context.Dispose();
    }
}

using Bruktborsen.Api.Models;

namespace Bruktborsen.Api.Repository;

public interface IListingRepository
{
    Task<Listing?> GetByIdAsync(int id);
}

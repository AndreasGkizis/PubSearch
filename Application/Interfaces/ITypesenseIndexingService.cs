using ResearchPublications.Domain.Entities;

namespace ResearchPublications.Application.Interfaces;

public interface ITypesenseIndexingService
{
    Task EnsureCollectionExistsAsync();
    Task IndexAllPublicationsAsync();
    Task IndexPublicationAsync(Publication publication);
    Task RemovePublicationAsync(int id);
}

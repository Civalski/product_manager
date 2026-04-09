using ProductStore.Api.DTOs;
using ProductStore.Api.Exceptions;
using ProductStore.Api.Services;

namespace ProductStore.Api.Tests;

internal sealed class NoOpCosmosGtinValidator : ICosmosGtinValidator
{
    public Task ValidateAsync(string sku, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<CosmosGtinProductDto> FetchProductAsync(string sku, CancellationToken cancellationToken = default) =>
        Task.FromException<CosmosGtinProductDto>(new CosmosNotConfiguredException());
}

using Microsoft.AspNetCore.Diagnostics;

using ProductStore.Api.Exceptions;



namespace ProductStore.Api.Middleware;



public sealed class GlobalExceptionHandler(

    ILogger<GlobalExceptionHandler> logger,

    IHostEnvironment environment) : IExceptionHandler

{

    public async ValueTask<bool> TryHandleAsync(

        HttpContext httpContext,

        Exception exception,

        CancellationToken cancellationToken)

    {

        switch (exception)

        {

            case ProductNotFoundException nf:

                logger.LogWarning("Produto não encontrado: {ProductId}", nf.ProductId);

                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;

                await httpContext.Response.WriteAsJsonAsync(

                    new Microsoft.AspNetCore.Mvc.ProblemDetails

                    {

                        Status = StatusCodes.Status404NotFound,

                        Title = "Não encontrado",

                        Detail = nf.Message,

                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"

                    },

                    cancellationToken);

                return true;



            case CategoryNotFoundException cf:

                logger.LogWarning("Categoria não encontrada: {CategoryId}", cf.CategoryId);

                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;

                await httpContext.Response.WriteAsJsonAsync(

                    new Microsoft.AspNetCore.Mvc.ProblemDetails

                    {

                        Status = StatusCodes.Status404NotFound,

                        Title = "Não encontrado",

                        Detail = cf.Message,

                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"

                    },

                    cancellationToken);

                return true;



            case DuplicateSkuException dup:

                logger.LogWarning("Conflito de SKU: {Sku}", dup.Sku);

                httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

                await httpContext.Response.WriteAsJsonAsync(

                    new Microsoft.AspNetCore.Mvc.ProblemDetails

                    {

                        Status = StatusCodes.Status409Conflict,

                        Title = "Conflito",

                        Detail = dup.Message,

                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8"

                    },

                    cancellationToken);

                return true;



            case DuplicateCategoryNameException dupCat:

                logger.LogWarning("Nome de categoria duplicado: {Name}", dupCat.Name);

                httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

                await httpContext.Response.WriteAsJsonAsync(

                    new Microsoft.AspNetCore.Mvc.ProblemDetails

                    {

                        Status = StatusCodes.Status409Conflict,

                        Title = "Conflito",

                        Detail = dupCat.Message,

                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8"

                    },

                    cancellationToken);

                return true;



            case FormatException fe:

                logger.LogWarning("Formato de dados inválido: {Message}", fe.Message);

                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

                await httpContext.Response.WriteAsJsonAsync(

                    new Microsoft.AspNetCore.Mvc.ProblemDetails

                    {

                        Status = StatusCodes.Status500InternalServerError,

                        Title = "Erro de dados",

                        Detail = environment.IsDevelopment()

                            ? fe.Message

                            : "Inconsistência nos dados armazenados. Contacte o suporte ou restaure o banco.",

                        Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"

                    },

                    cancellationToken);

                return true;



            case NoProductsToExportException noProducts:

                logger.LogInformation("Backup sem produtos na base do utilizador");

                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

                await httpContext.Response.WriteAsJsonAsync(

                    new Microsoft.AspNetCore.Mvc.ProblemDetails

                    {

                        Status = StatusCodes.Status400BadRequest,

                        Title = "Sem dados",

                        Detail = noProducts.Message,

                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"

                    },

                    cancellationToken);

                return true;



            case ElectronicsMinPriceException price:

                logger.LogWarning("Preço abaixo do mínimo para eletrônico: mínimo {Min}", price.MinPrice);

                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

                await httpContext.Response.WriteAsJsonAsync(

                    new Microsoft.AspNetCore.Mvc.ProblemDetails

                    {

                        Status = StatusCodes.Status400BadRequest,

                        Title = "Validação",

                        Detail = price.Message,

                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"

                    },

                    cancellationToken);

                return true;



            case InvalidGtinSkuException badGtin:

                logger.LogWarning("SKU com formato de GTIN inválido: {Sku}", badGtin.Sku);

                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

                await httpContext.Response.WriteAsJsonAsync(

                    new Microsoft.AspNetCore.Mvc.ProblemDetails

                    {

                        Status = StatusCodes.Status400BadRequest,

                        Title = "Validação",

                        Detail = badGtin.Message,

                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"

                    },

                    cancellationToken);

                return true;



            case CosmosProductNotFoundException notInCosmos:

                logger.LogWarning("GTIN não encontrado na Cosmos: {Gtin}", notInCosmos.Gtin);

                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

                await httpContext.Response.WriteAsJsonAsync(

                    new Microsoft.AspNetCore.Mvc.ProblemDetails

                    {

                        Status = StatusCodes.Status400BadRequest,

                        Title = "SKU inválido",

                        Detail = notInCosmos.Message,

                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"

                    },

                    cancellationToken);

                return true;



            case CosmosNotConfiguredException notCfg:

                logger.LogWarning("Cosmos não configurado: {Message}", notCfg.Message);

                httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

                await httpContext.Response.WriteAsJsonAsync(

                    new Microsoft.AspNetCore.Mvc.ProblemDetails

                    {

                        Status = StatusCodes.Status503ServiceUnavailable,

                        Title = "Integração Cosmos",

                        Detail = notCfg.Message,

                        Type = "https://tools.ietf.org/html/rfc7231#section-6.6.4"

                    },

                    cancellationToken);

                return true;



            case CosmosApiException cosmosErr:

                var cosmosStatus = cosmosErr.SuggestedStatusCode is >= 400 and < 600

                    ? cosmosErr.SuggestedStatusCode

                    : StatusCodes.Status502BadGateway;

                logger.LogWarning("Erro na API Cosmos: {Message} (status sugerido {Status})", cosmosErr.Message, cosmosStatus);

                httpContext.Response.StatusCode = cosmosStatus;

                await httpContext.Response.WriteAsJsonAsync(

                    new Microsoft.AspNetCore.Mvc.ProblemDetails

                    {

                        Status = cosmosStatus,

                        Title = cosmosStatus == StatusCodes.Status429TooManyRequests ? "Limite excedido" : "Integração Cosmos",

                        Detail = cosmosErr.Message,

                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"

                    },

                    cancellationToken);

                return true;



            default:

                if (environment.IsDevelopment())

                    logger.LogError(exception, "Exceção não tratada");

                else

                    logger.LogError("Exceção não tratada: {Type}: {Message}", exception.GetType().Name, exception.Message);

                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

                await httpContext.Response.WriteAsJsonAsync(

                    new Microsoft.AspNetCore.Mvc.ProblemDetails

                    {

                        Status = StatusCodes.Status500InternalServerError,

                        Title = "Erro interno",

                        Detail = environment.IsDevelopment()

                            ? exception.ToString()

                            : "Ocorreu um erro inesperado.",

                        Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"

                    },

                    cancellationToken);

                return true;

        }

    }

}


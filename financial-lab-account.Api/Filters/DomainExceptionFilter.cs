using financial_lab_account.Business.Exceptions;

namespace financial_lab_account.Api.Filters;

public class DomainExceptionFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (DomainException ex)
        {
            var (status, title) = ex switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado."),
                ConflictException => (StatusCodes.Status409Conflict, "Conflito."),
                BusinessRuleException => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada."),
                _ => (StatusCodes.Status400BadRequest, "Requisição inválida.")
            };
            return Results.Problem(title: title, detail: ex.Message, statusCode: status);
        }
    }
}

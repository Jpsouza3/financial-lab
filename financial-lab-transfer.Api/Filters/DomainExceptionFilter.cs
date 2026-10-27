using financial_lab_transfer.Business.Exceptions;

namespace financial_lab_transfer.Api.Filters;

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
            return Results.Problem(title: "Requisição inválida.", detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }
}

using Microsoft.AspNetCore.Diagnostics;
using PdfBatch.Api.Models;

namespace PdfBatch.Api.Middleware;

internal static class ApiExceptionHandler
{
    public static async Task HandleAsync(HttpContext context)
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var status = exception is BadHttpRequestException badRequest
            ? badRequest.StatusCode : StatusCodes.Status500InternalServerError;
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ResponseDto
        {
            Status = status,
            Message = status == StatusCodes.Status500InternalServerError
                ? "PDF generation failed. Please try again." : "The request could not be processed.",
            Result = null
        }, context.RequestAborted);
    }
}

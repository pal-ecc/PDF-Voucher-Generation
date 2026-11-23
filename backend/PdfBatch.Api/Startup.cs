using Microsoft.AspNetCore.Mvc;
using PdfBatch.Api.Core.Interfaces;
using PdfBatch.Api.DBContext;
using PdfBatch.Api.Fonts;
using PdfBatch.Api.Helpers.Pdf;
using PdfBatch.Api.Middleware;
using PdfBatch.Api.Models;
using PdfBatch.Api.Services.Print;
using PdfBatch.Api.Utility;
using PdfSharpCore.Fonts;
using PdfSharpCore.Utils;

namespace PdfBatch.Api;

public sealed class Startup(IConfiguration configuration, IWebHostEnvironment environment)
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers().ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(entry => entry.Value?.Errors.Count > 0)
                    .ToDictionary(entry => entry.Key, entry => entry.Value!.Errors
                        .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "The supplied value is invalid." : error.ErrorMessage).ToArray());
                return new BadRequestObjectResult(new ResponseDto
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = string.Join(" ", errors.Values.SelectMany(messages => messages).Distinct()),
                    Result = new ResultDto { Data = errors }
                });
            };
        });
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<PdfGeneratorService>();
        services.AddScoped<IPdfGeneratorService>(services => services.GetRequiredService<PdfGeneratorService>());
        services.AddScoped<IPdfBatchService>(services => services.GetRequiredService<PdfGeneratorService>());
        services.AddSingleton<IAmountInWordsFormatter, InternationalAmountInWordsFormatter>();
        services.AddScoped<VoucherDocument>();

        // Development samples only. Replace with the application's authorized data access before deployment.
        if (!environment.IsDevelopment())
            throw new InvalidOperationException("Configure a production IInvoiceRepository before deploying this sample.");
        services.AddScoped<IInvoiceRepository, DevelopmentInvoiceRepository>();

        // Resolve fonts once before rendering. Explicit font files are recommended for deployed hosts.
        var regularFont = configuration["PdfFonts:RegularPath"];
        var boldFont = configuration["PdfFonts:BoldPath"];
        if (string.IsNullOrWhiteSpace(regularFont) != string.IsNullOrWhiteSpace(boldFont))
            throw new InvalidOperationException("Configure both PdfFonts:RegularPath and PdfFonts:BoldPath together.");
        if (!string.IsNullOrWhiteSpace(regularFont) && !string.IsNullOrWhiteSpace(boldFont))
            GlobalFontSettings.FontResolver = new PdfFontResolver(regularFont, boldFont);
        else
            GlobalFontSettings.FontResolver = new FontResolver();
    }

    public void Configure(WebApplication app)
    {
        app.UseExceptionHandler(handler => handler.Run(ApiExceptionHandler.HandleAsync));
        app.MapControllers();
    }
}

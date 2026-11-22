using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PdfBatch.Api.Contracts;
using PdfBatch.Api.Features.Print;
using PdfSharpCore.Fonts;
using PdfSharpCore.Utils;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
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
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<PdfGeneratorService>();
builder.Services.AddScoped<IPdfGeneratorService>(services => services.GetRequiredService<PdfGeneratorService>());
builder.Services.AddScoped<IPdfBatchService>(services => services.GetRequiredService<PdfGeneratorService>());
builder.Services.AddSingleton<IAmountInWordsFormatter, InternationalAmountInWordsFormatter>();
builder.Services.AddScoped<VoucherDocument>();

// Development samples only. Replace with the application's authorized data access before deployment.
if (!builder.Environment.IsDevelopment())
    throw new InvalidOperationException("Configure a production IInvoiceRepository before deploying this sample.");
builder.Services.AddScoped<IInvoiceRepository, DevelopmentInvoiceRepository>();

// Resolve fonts once before rendering. Explicit font files are recommended for deployed hosts.
var regularFont = builder.Configuration["PdfFonts:RegularPath"];
var boldFont = builder.Configuration["PdfFonts:BoldPath"];
if (string.IsNullOrWhiteSpace(regularFont) != string.IsNullOrWhiteSpace(boldFont))
    throw new InvalidOperationException("Configure both PdfFonts:RegularPath and PdfFonts:BoldPath together.");
if (!string.IsNullOrWhiteSpace(regularFont) && !string.IsNullOrWhiteSpace(boldFont))
    GlobalFontSettings.FontResolver = new PdfFontResolver(regularFont, boldFont);
else
    GlobalFontSettings.FontResolver = new FontResolver();

var app = builder.Build();
app.UseExceptionHandler(handler => handler.Run(async context =>
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
}));
app.MapControllers();
app.Run();

public partial class Program;

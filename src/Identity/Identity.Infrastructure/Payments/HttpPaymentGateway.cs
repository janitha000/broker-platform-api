using System.Net;
using System.Net.Http.Json;
using Identity.Application.Abstractions;

namespace Identity.Infrastructure.Payments;

public sealed class HttpPaymentGateway : IPaymentGateway
{
    private readonly HttpClient _http;

    public HttpPaymentGateway(HttpClient http)
    {
        _http = http;
    }

    public async Task<PaymentChargeStatus> Charge(
        string email,
        PaymentCard card,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(
                "payments/charges",
                new
                {
                    email,
                    card = new
                    {
                        number = card.Number,
                        expMonth = card.ExpMonth,
                        expYear = card.ExpYear,
                        cvc = card.Cvc,
                    },
                    idempotencyKey,
                },
                cancellationToken);

            return response.StatusCode switch
            {
                HttpStatusCode.OK => PaymentChargeStatus.Succeeded,
                HttpStatusCode.PaymentRequired => PaymentChargeStatus.Declined,
                HttpStatusCode.Conflict => PaymentChargeStatus.Conflict,
                _ => PaymentChargeStatus.Unavailable,
            };
        }
        catch (HttpRequestException)
        {
            return PaymentChargeStatus.Unavailable;
        }
        catch (TaskCanceledException)
        {
            return PaymentChargeStatus.Unavailable;
        }
    }
}
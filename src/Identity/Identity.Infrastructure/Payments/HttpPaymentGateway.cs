using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Identity.Application.Abstractions;
using Identity.Infrastructure.Auth;

namespace Identity.Infrastructure.Payments;

public sealed class HttpPaymentGateway : IPaymentGateway
{
    private readonly HttpClient _http;
    private readonly Auth0PaymentTokenProvider _paymentTokens;

    public HttpPaymentGateway(HttpClient http, Auth0PaymentTokenProvider paymentTokens)
    {
        _http = http;
        _paymentTokens = paymentTokens;
    }

    public async Task<PaymentChargeStatus> Charge(
        string email,
        PaymentCard card,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var accessToken = await _paymentTokens.GetAccessToken(cancellationToken);
            if (string.IsNullOrWhiteSpace(accessToken))
                return PaymentChargeStatus.Unavailable;

            using var request = new HttpRequestMessage(HttpMethod.Post, "payments/charges");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = JsonContent.Create(new
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
            });

            var response = await _http.SendAsync(request, cancellationToken);

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

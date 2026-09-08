using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Identity.Application.Abstractions;
using Identity.Infrastructure.Auth;

namespace Identity.Infrastructure.Payments;

public sealed class HttpPaymentGateway : IPaymentGateway
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly Auth0PaymentTokenProvider _paymentTokens;

    public HttpPaymentGateway(HttpClient http, Auth0PaymentTokenProvider paymentTokens)
    {
        _http = http;
        _paymentTokens = paymentTokens;
    }

    public async Task<PaymentChargeResult> Charge(
        string email,
        PaymentCard card,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var accessToken = await _paymentTokens.GetAccessToken(cancellationToken);
            if (string.IsNullOrWhiteSpace(accessToken))
                return new PaymentChargeResult(PaymentChargeStatus.Unavailable, null);

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

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var body = await response.Content.ReadFromJsonAsync<ChargeResponse>(Json, cancellationToken);
                if (body is null || body.ChargeId == Guid.Empty)
                    return new PaymentChargeResult(PaymentChargeStatus.Unavailable, null);

                return new PaymentChargeResult(PaymentChargeStatus.Succeeded, body.ChargeId);
            }

            var status = response.StatusCode switch
            {
                HttpStatusCode.PaymentRequired => PaymentChargeStatus.Declined,
                HttpStatusCode.Conflict => PaymentChargeStatus.Conflict,
                _ => PaymentChargeStatus.Unavailable,
            };
            return new PaymentChargeResult(status, null);
        }
        catch (HttpRequestException)
        {
            return new PaymentChargeResult(PaymentChargeStatus.Unavailable, null);
        }
        catch (TaskCanceledException)
        {
            return new PaymentChargeResult(PaymentChargeStatus.Unavailable, null);
        }
    }

    public async Task<PaymentRefundStatus> Refund(
        Guid chargeId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var accessToken = await _paymentTokens.GetAccessToken(cancellationToken);
            if (string.IsNullOrWhiteSpace(accessToken))
                return PaymentRefundStatus.Unavailable;

            using var request = new HttpRequestMessage(HttpMethod.Post, "payments/refunds");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = JsonContent.Create(new { chargeId, idempotencyKey });

            var response = await _http.SendAsync(request, cancellationToken);
            return response.StatusCode switch
            {
                HttpStatusCode.OK => PaymentRefundStatus.Succeeded,
                HttpStatusCode.NotFound => PaymentRefundStatus.NotFound,
                HttpStatusCode.Conflict => PaymentRefundStatus.NotRefundable,
                _ => PaymentRefundStatus.Unavailable,
            };
        }
        catch (HttpRequestException)
        {
            return PaymentRefundStatus.Unavailable;
        }
        catch (TaskCanceledException)
        {
            return PaymentRefundStatus.Unavailable;
        }
    }

    private sealed record ChargeResponse(Guid ChargeId, string Status);
}

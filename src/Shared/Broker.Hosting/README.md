# Broker.Hosting

Shared HTTP host kit for the Broker Platform APIs.

`AddBrokerWebHost` / `UseBrokerWebHost` register Swagger (Development), JSON string enums, CORS from `Cors:Origins` (no-op when empty), then `UseAuthentication` / `UseAuthorization` / `MapControllers`. Pass `BrokerWebHostOptions { AllowCredentials = true }` for Identity and Notification when CORS is enabled.

Shared `broker.access` cookie authentication for Identity, Origination, Document, Audit, and Notification.

`AddBrokerSessionAuthentication` picks a validator from `Auth:Mode`:

- `IdentityJwt` (learning reference): HMAC JWT issued by Identity (`AddBrokerJwtAuthentication`).
- `Auth0Organizations` (default in Development): Auth0 access token validated via JWKS (`AddAuth0AccessTokenAuthentication`).

Identity still runs the OIDC BFF. See [AUTH.md](../../../../AUTH.md).

`Broker.Hosting.Audit` is the shared audit envelope (`AuditEvent`). Services record via `IAuditRecorder`; they do not HTTP-call the Audit service.

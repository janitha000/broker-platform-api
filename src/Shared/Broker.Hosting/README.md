# Broker.Hosting

Shared HTTP host kit for the Broker Platform APIs.

`AddBrokerWebHost` / `UseBrokerWebHost` register Swagger (Development), JSON string enums, CORS from `Cors:Origins` (no-op when empty), rate limiting, then `UseAuthentication` / `UseRateLimiter` / `UseAuthorization` / `MapControllers`. Swagger marks non-nullable C# properties as required (`SupportNonNullableReferenceTypes` plus `RequireNonNullablePropertiesSchemaFilter`, because positional records omit `required` otherwise). Pass `BrokerWebHostOptions { AllowCredentials = true }` for Identity and Notification when CORS is enabled.

## Rate limiting

`RateLimiting` configures an in-memory sliding-window limiter for every API using this host kit. Authenticated requests are partitioned by `tenant_id`, then authenticated `sub`, and anonymous requests by remote IP. `/auth*` uses a separate, stricter partition and `/health*` is not limited. Rejections return HTTP 429 Problem Details with a `Retry-After` header.

Defaults are 120 general requests and 20 auth requests per 60-second window. All permit limits and windows must be greater than zero. Counters are local to each process; use a distributed limiter when multiple ECS tasks must share one tenant-wide budget.

Shared `broker.access` cookie authentication for Identity, Origination, Document, Audit, and Notification.

`AddBrokerSessionAuthentication` picks a validator from `Auth:Mode`:

- `IdentityJwt` (learning reference): HMAC JWT issued by Identity (`AddBrokerJwtAuthentication`).
- `Auth0Organizations` (default in Development): Auth0 access token validated via JWKS (`AddAuth0AccessTokenAuthentication`).

Identity still runs the OIDC BFF. See [AUTH.md](../../../../AUTH.md).

`Broker.Hosting.Audit` is the shared audit envelope (`AuditEvent`). Services record via `IAuditRecorder`; they do not HTTP-call the Audit service.

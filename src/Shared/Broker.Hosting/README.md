# Broker.Hosting

Shared `broker.access` cookie authentication for Identity, Origination, and Notification.

`AddBrokerSessionAuthentication` picks a validator from `Auth:Mode`:

- `IdentityJwt` (learning reference): HMAC JWT issued by Identity (`AddBrokerJwtAuthentication`).
- `Auth0Organizations` (default in Development): Auth0 access token validated via JWKS (`AddAuth0AccessTokenAuthentication`).

Identity still runs the OIDC BFF. See [AUTH.md](../../../../AUTH.md).

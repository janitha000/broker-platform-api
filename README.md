# Broker platform API

Greenfield **Australian mortgage broking** backend. Brokers originate **cases** (not loans until settlement). This repo is the API monorepo. **.NET 8**. One database per service. No shared domain entities across services.

## What exists today

Only **Origination** is implemented. Later services (Identity, BrokerDirectory, Document, ProductCatalog, Notification) are not in this repo yet.

| Piece | Status |
|---|---|
| Origination API (cases + fact-find) | Running locally, Docker Compose, and on AWS |
| EF migrations | Applied to local SQL and to RDS (manual from a laptop) |
| AWS (`ap-southeast-2`) | VPC, RDS SQL Server, ECR, ECS Fargate, ALB, Secrets Manager |
| GitHub Actions | Test → Docker → ECR → force new ECS deployment |

## Domain (MVP)

- **Case** — inquiry from a broker; core object until settlement.
- **Fact-find** — income / expenses / assets / debts attached to a case.
- Status path so far: `Inquiry` → `FactFindCompleted`. Recommendation and lodgement come later.

IDs only across future services (no shared SQL). Sync HTTP where the caller needs an answer now; events later.

## Origination layout (onion)

```
src/Origination/
  Origination.Domain
  Origination.Application
  Origination.Infrastructure   # EF + SQL Server
  Origination.Api
  Dockerfile
  docker-compose.yml
tests/Origination/Origination.Application.Tests
infra/origination/             # Terraform for origination-dev
.github/workflows/origination-ecr.yml
```

Rules: Domain has no Infrastructure references. Api wires Application + Infrastructure. Connection string name is **`Origination`**.

Auth: Identity issues a JWT (`POST /auth/register` or `/auth/login`). Origination `/cases` requires `Authorization: Bearer`. `/health` is anonymous. Same `Jwt` Key/Issuer/Audience in both APIs. JSON uses string enums.

## API

Base URL:

- Local / Compose: `http://localhost:8080`
- AWS: `http://<alb_dns>` from `terraform output alb_dns_name` (HTTP only)

| Method | Path | Notes |
|---|---|---|
| GET | `/health` | ALB health check; must return 200 |
| POST | `/cases` | Body: `{ "inquiryNotes": "..." }` |
| GET | `/cases/{caseId}` | |
| PUT | `/cases/{caseId}/fact-find` | Route supplies `caseId`; body is money fields |

HTTPS redirection is **off** in `Program.cs` so the ALB HTTP health check is not a 307.

Do **not** put the RDS password in `appsettings.json`. That file keeps LocalDB for a laptop. Runtime overrides:

`ConnectionStrings__Origination`

On ECS this is injected from Secrets Manager `origination/dev/sql`.

## Local: .NET + LocalDB

```powershell
cd src\Origination
dotnet test ..\..\tests\Origination\Origination.Application.Tests\Origination.Application.Tests.csproj
dotnet ef database update --project Origination.Infrastructure --startup-project Origination.Api
dotnet run --project Origination.Api
```

`InvariantGlobalization` must stay **false** (SqlClient).

## Local: Docker Compose

```powershell
cd src\Origination
docker build -t origination-api:local .
docker compose up
```

SQL Server SA password in compose is for **dev only**. API is on port **8080**.

## AWS (origination-dev)

Region **`ap-southeast-2`**. Terraform lives in [`infra/origination`](infra/origination). State and `terraform.tfvars` are gitignored (password + `my_ip`).

```
Laptop / GitHub
    → ECR (origination-api, identity-api)
    → ECS Fargate (private subnets)
    → ALB :80
         /auth*     → identity-api  (register / login)
         everything else → origination-api  (/health, /cases)
    → RDS SQL Server: databases Origination and Identity
    → Jwt__Key from Secrets Manager origination/dev/jwt (same for both tasks)
```

Public Identity URLs use the **same ALB host**: `http://<alb>/auth/register`, `http://<alb>/auth/login`. Origination `POST /cases` still needs `Authorization: Bearer`.

Apply from a machine with AWS credentials:

```powershell
cd infra\origination
terraform init
terraform apply
terraform output alb_dns_name
terraform output identity_ecr_repository_url
```

Push an `identity-api:latest` image (Actions or local `docker push`) **before** the Identity service will go healthy. Then migrate Identity on RDS (create database `Identity` if needed):

```powershell
# ConnectionStrings__Identity = same host as Origination, Database=Identity
dotnet ef database update --project src\Identity\Identity.Infrastructure --startup-project src\Identity\Identity.Api
```

`terraform.tfvars` (not committed) needs `db_password` and optionally `my_ip` as `x.x.x.x/32` for laptop EF against RDS.

**RDS is currently publicly accessible** so a laptop can migrate. Do not open 1433 to `0.0.0.0/0`. Moving the instance back to private subnets requires destroying the DB instance (subnet groups cannot drop in-use subnets); that **wipes data** unless you snapshot first. Re-run `dotnet ef database update` after a recreate.

EF from GitHub cannot reach RDS. Migrate from a host allowed on the RDS security group.

NAT + RDS **bill until `terraform destroy`**.

## GitHub Actions

Workflows:

- [`.github/workflows/origination-ecr.yml`](.github/workflows/origination-ecr.yml) — Origination test / ECR / ECS `origination-api`
- [`.github/workflows/identity-ecr.yml`](.github/workflows/identity-ecr.yml) — Identity test / ECR `identity-api` / ECS `identity-api`

Trigger: push to **`release`** (path filters) or `workflow_dispatch`.

OIDC (no long-lived AWS keys). Repository **variables** (not GitHub Environments, not access-key secrets):

| Variable | Example |
|---|---|
| `AWS_ROLE_ARN` | `arn:aws:iam::ACCOUNT:role/origination-dev-github-actions` |
| `AWS_REGION` | `ap-southeast-2` |
| `ECR_REPOSITORY` | `origination-api` |

Trust policy must match the **real** token `sub`. GitHub currently signs subjects with numeric ids, for example:

`repo:janitha000@5737103/broker-platform-api@1344936057:ref:refs/heads/master`

Classic `repo:janitha000/broker-platform-api:*` alone is not enough. See [`infra/origination/github_oidc.tf`](infra/origination/github_oidc.tf). Job needs `permissions: id-token: write`.

## Useful commands

```powershell
# After a new image, if Actions deploy did not run:
aws ecs update-service --cluster origination-dev --service origination-api --force-new-deployment --region ap-southeast-2

# Task logs
# CloudWatch groups: /ecs/origination-api  /ecs/identity-api
```

## Not done yet

CORS for a browser UI, ALB HTTPS, private RDS lock-down without data loss, S3 Terraform state, auto-migrate from Actions, production IAM tightening. Do not use the default Jwt key outside this spike.

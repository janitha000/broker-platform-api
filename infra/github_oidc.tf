variable "github_org" {
  type    = string
  default = "janitha000"
}

variable "github_repo" {
  type    = string
  default = "broker-platform-api"
}

variable "github_branch" {
  type    = string
  default = "master"
}

# GitHub embeds immutable numeric ids in the OIDC sub claim:
# repo:<org>@<owner_id>/<repo>@<repo_id>:ref:refs/heads/<branch>
variable "github_owner_id" {
  type    = string
  default = "5737103"
}

variable "github_repo_id" {
  type    = string
  default = "1344936057"
}

resource "aws_iam_openid_connect_provider" "github" {
  url            = "https://token.actions.githubusercontent.com"
  client_id_list = ["sts.amazonaws.com"]
  # GitHub Actions CA thumbprints (not the leaf from tls_certificate).
  # See https://github.blog/changelog/2023-06-27-github-actions-update-on-oidc-integration-with-aws/
  thumbprint_list = [
    "6938fd4d98bab03faadb97b34396831e3780aea1",
    "1c58a3a8518e8759bf075b76b750d4c743ad7336",
  ]
}

data "aws_iam_policy_document" "github_assume" {
  statement {
    actions = ["sts:AssumeRoleWithWebIdentity"]
    principals {
      type        = "Federated"
      identifiers = [aws_iam_openid_connect_provider.github.arn]
    }
    condition {
      test     = "StringEquals"
      variable = "token.actions.githubusercontent.com:aud"
      values   = ["sts.amazonaws.com"]
    }
    condition {
      test     = "StringLike"
      variable = "token.actions.githubusercontent.com:sub"
      values = [
        "repo:${var.github_org}@${var.github_owner_id}/${var.github_repo}@${var.github_repo_id}:*",
        "repo:${var.github_org}/${var.github_repo}:*",
      ]
    }
  }
}

resource "aws_iam_role" "github_actions" {
  name               = "origination-dev-github-actions"
  assume_role_policy = data.aws_iam_policy_document.github_assume.json
}

data "aws_iam_policy_document" "github_ecr" {
  statement {
    sid       = "EcrAuth"
    actions   = ["ecr:GetAuthorizationToken"]
    resources = ["*"]
  }

  statement {
    sid = "EcrPush"
    actions = [
      "ecr:BatchCheckLayerAvailability",
      "ecr:GetDownloadUrlForLayer",
      "ecr:BatchGetImage",
      "ecr:PutImage",
      "ecr:InitiateLayerUpload",
      "ecr:UploadLayerPart",
      "ecr:CompleteLayerUpload",
    ]
    resources = [
      aws_ecr_repository.api.arn,
      aws_ecr_repository.identity.arn,
      aws_ecr_repository.payment.arn,
      aws_ecr_repository.notification.arn,
    ]
  }
}

resource "aws_iam_role_policy" "github_ecr" {
  name   = "origination-dev-github-ecr"
  role   = aws_iam_role.github_actions.id
  policy = data.aws_iam_policy_document.github_ecr.json
}

variable "github_client_repo" {
  type    = string
  default = "broker-platform-client"
}

variable "github_client_repo_id" {
  type        = string
  default     = "1349209006"
  description = "Numeric GitHub repo id for OIDC sub (broker-platform-client)."
}

data "aws_iam_policy_document" "github_client_assume" {
  statement {
    actions = ["sts:AssumeRoleWithWebIdentity"]
    principals {
      type        = "Federated"
      identifiers = [aws_iam_openid_connect_provider.github.arn]
    }
    condition {
      test     = "StringEquals"
      variable = "token.actions.githubusercontent.com:aud"
      values   = ["sts.amazonaws.com"]
    }
    condition {
      test     = "StringLike"
      variable = "token.actions.githubusercontent.com:sub"
      values = [
        "repo:${var.github_org}@${var.github_owner_id}/${var.github_client_repo}@${var.github_client_repo_id}:*",
        "repo:${var.github_org}/${var.github_client_repo}:*",
      ]
    }
  }
}

resource "aws_iam_role" "github_actions_client" {
  name               = "origination-dev-github-client"
  assume_role_policy = data.aws_iam_policy_document.github_client_assume.json
}

data "aws_iam_policy_document" "github_client_deploy" {
  statement {
    sid = "S3Sync"
    actions = [
      "s3:PutObject",
      "s3:GetObject",
      "s3:DeleteObject",
      "s3:ListBucket",
    ]
    resources = [
      aws_s3_bucket.ui.arn,
      "${aws_s3_bucket.ui.arn}/*",
    ]
  }

  statement {
    sid       = "CloudFrontInvalidate"
    actions   = ["cloudfront:CreateInvalidation"]
    resources = [aws_cloudfront_distribution.ui.arn]
  }
}

resource "aws_iam_role_policy" "github_client_deploy" {
  name   = "origination-dev-github-client-deploy"
  role   = aws_iam_role.github_actions_client.id
  policy = data.aws_iam_policy_document.github_client_deploy.json
}

output "github_client_actions_role_arn" {
  value = aws_iam_role.github_actions_client.arn
}

output "github_actions_role_arn" {
  value = aws_iam_role.github_actions.arn
}

data "aws_caller_identity" "current" {}

data "aws_iam_policy_document" "github_ecs" {
  statement {
    sid = "EcsRoll"
    actions = [
      "ecs:UpdateService",
      "ecs:DescribeServices",
    ]
    resources = [
      "arn:aws:ecs:${var.aws_region}:${data.aws_caller_identity.current.account_id}:service/origination-dev/origination-api",
      "arn:aws:ecs:${var.aws_region}:${data.aws_caller_identity.current.account_id}:service/origination-dev/identity-api",
      "arn:aws:ecs:${var.aws_region}:${data.aws_caller_identity.current.account_id}:service/origination-dev/payment-api",
      "arn:aws:ecs:${var.aws_region}:${data.aws_caller_identity.current.account_id}:service/origination-dev/notification-api",
    ]
  }
}

resource "aws_iam_role_policy" "github_ecs" {
  name   = "origination-dev-github-ecs"
  role   = aws_iam_role.github_actions.id
  policy = data.aws_iam_policy_document.github_ecs.json
}
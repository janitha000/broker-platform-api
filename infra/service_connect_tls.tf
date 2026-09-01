# Service Connect TLS template (proxy-to-proxy mTLS via ACM PCA).
# Not created on apply unless enable_service_connect_tls = true.
# Apps still use http://payment-api:8080; sidecars encrypt the hop.
# PCA is billed monthly.

data "aws_partition" "current" {}

resource "aws_kms_key" "service_connect_tls" {
  count                   = var.enable_service_connect_tls ? 1 : 0
  description             = "ECS Service Connect TLS private keys"
  deletion_window_in_days = 7
  enable_key_rotation     = true
}

resource "aws_kms_alias" "service_connect_tls" {
  count         = var.enable_service_connect_tls ? 1 : 0
  name          = "alias/origination-dev-service-connect-tls"
  target_key_id = aws_kms_key.service_connect_tls[0].id
}

resource "aws_acmpca_certificate_authority" "service_connect" {
  count      = var.enable_service_connect_tls ? 1 : 0
  type       = "ROOT"
  usage_mode = "SHORT_LIVED_CERTIFICATE"

  certificate_authority_configuration {
    key_algorithm     = "RSA_2048"
    signing_algorithm = "SHA256WITHRSA"

    subject {
      common_name = "origination-dev.service-connect"
    }
  }
}

resource "aws_acmpca_certificate" "service_connect_root" {
  count                       = var.enable_service_connect_tls ? 1 : 0
  certificate_authority_arn   = aws_acmpca_certificate_authority.service_connect[0].arn
  certificate_signing_request = aws_acmpca_certificate_authority.service_connect[0].certificate_signing_request
  signing_algorithm           = "SHA256WITHRSA"
  template_arn                = "arn:${data.aws_partition.current.partition}:acm-pca:::template/RootCACertificate/V1"

  validity {
    type  = "YEARS"
    value = 10
  }
}

resource "aws_acmpca_certificate_authority_certificate" "service_connect" {
  count                     = var.enable_service_connect_tls ? 1 : 0
  certificate_authority_arn = aws_acmpca_certificate_authority.service_connect[0].arn
  certificate               = aws_acmpca_certificate.service_connect_root[0].certificate
  certificate_chain         = aws_acmpca_certificate.service_connect_root[0].certificate_chain
}

data "aws_iam_policy_document" "service_connect_tls_assume" {
  statement {
    actions = ["sts:AssumeRole"]
    principals {
      type        = "Service"
      identifiers = ["ecs-tasks.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "service_connect_tls" {
  count              = var.enable_service_connect_tls ? 1 : 0
  name               = "origination-dev-service-connect-tls"
  assume_role_policy = data.aws_iam_policy_document.service_connect_tls_assume.json
}

data "aws_iam_policy_document" "service_connect_tls" {
  count = var.enable_service_connect_tls ? 1 : 0

  statement {
    sid = "IssueShortLivedCerts"
    actions = [
      "acm-pca:IssueCertificate",
      "acm-pca:GetCertificate",
      "acm-pca:GetCertificateAuthorityCertificate",
    ]
    resources = [aws_acmpca_certificate_authority.service_connect[0].arn]
  }

  statement {
    sid = "UseTlsKey"
    actions = [
      "kms:Decrypt",
      "kms:GenerateDataKey",
      "kms:DescribeKey",
    ]
    resources = [aws_kms_key.service_connect_tls[0].arn]
  }
}

resource "aws_iam_role_policy" "service_connect_tls" {
  count  = var.enable_service_connect_tls ? 1 : 0
  name   = "origination-dev-service-connect-tls"
  role   = aws_iam_role.service_connect_tls[0].id
  policy = data.aws_iam_policy_document.service_connect_tls[0].json
}

resource "aws_kms_key_policy" "service_connect_tls" {
  count  = var.enable_service_connect_tls ? 1 : 0
  key_id = aws_kms_key.service_connect_tls[0].id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "AccountRoot"
        Effect = "Allow"
        Principal = {
          AWS = "arn:${data.aws_partition.current.partition}:iam::${data.aws_caller_identity.current.account_id}:root"
        }
        Action   = "kms:*"
        Resource = "*"
      },
      {
        Sid    = "ServiceConnectTlsRole"
        Effect = "Allow"
        Principal = {
          AWS = aws_iam_role.service_connect_tls[0].arn
        }
        Action = [
          "kms:Decrypt",
          "kms:GenerateDataKey",
          "kms:DescribeKey",
        ]
        Resource = "*"
      }
    ]
  })
}

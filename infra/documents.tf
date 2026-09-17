resource "aws_s3_bucket" "document_landing" {
  bucket = "origination-dev-docs-landing-${data.aws_caller_identity.current.account_id}"
}

resource "aws_s3_bucket" "document_clean" {
  bucket = "origination-dev-docs-clean-${data.aws_caller_identity.current.account_id}"
}

resource "aws_s3_bucket" "document_quarantine" {
  bucket = "origination-dev-docs-quarantine-${data.aws_caller_identity.current.account_id}"
}

resource "aws_s3_bucket_public_access_block" "document_landing" {
  bucket                  = aws_s3_bucket.document_landing.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_public_access_block" "document_clean" {
  bucket                  = aws_s3_bucket.document_clean.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_public_access_block" "document_quarantine" {
  bucket                  = aws_s3_bucket.document_quarantine.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_server_side_encryption_configuration" "document_landing" {
  bucket = aws_s3_bucket.document_landing.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "document_clean" {
  bucket = aws_s3_bucket.document_clean.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "document_quarantine" {
  bucket = aws_s3_bucket.document_quarantine.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

resource "aws_s3_bucket_versioning" "document_clean" {
  bucket = aws_s3_bucket.document_clean.id

  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_lifecycle_configuration" "document_landing" {
  bucket = aws_s3_bucket.document_landing.id

  rule {
    id     = "expire-unpromoted-landing"
    status = "Enabled"

    filter {
      prefix = ""
    }

    expiration {
      days = 7
    }
  }
}

resource "aws_s3_bucket_cors_configuration" "document_landing" {
  bucket = aws_s3_bucket.document_landing.id

  cors_rule {
    allowed_headers = ["*"]
    allowed_methods = ["PUT", "HEAD"]
    allowed_origins = ["*"]
    expose_headers  = ["ETag"]
    max_age_seconds = 300
  }
}

resource "aws_s3_bucket_cors_configuration" "document_clean" {
  bucket = aws_s3_bucket.document_clean.id

  cors_rule {
    allowed_headers = ["*"]
    allowed_methods = ["GET", "HEAD"]
    allowed_origins = ["*"]
    expose_headers  = ["ETag"]
    max_age_seconds = 300
  }
}

resource "aws_s3_bucket_notification" "document_landing" {
  bucket      = aws_s3_bucket.document_landing.id
  eventbridge = true
}

resource "aws_sqs_queue" "document_landing_dlq" {
  name                      = "document-landing-dlq"
  message_retention_seconds = 1209600
}

resource "aws_sqs_queue" "document_landing" {
  name                       = "document-landing"
  visibility_timeout_seconds = 60
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.document_landing_dlq.arn
    maxReceiveCount     = 5
  })
}

resource "aws_cloudwatch_event_rule" "document_landing_created" {
  name = "document-landing-object-created"
  event_pattern = jsonencode({
    source        = ["aws.s3"]
    "detail-type" = ["Object Created"]
    detail = {
      bucket = {
        name = [aws_s3_bucket.document_landing.id]
      }
    }
  })
}

resource "aws_sqs_queue_policy" "document_landing" {
  queue_url = aws_sqs_queue.document_landing.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Sid       = "AllowEventBridge"
      Effect    = "Allow"
      Principal = { Service = "events.amazonaws.com" }
      Action    = "sqs:SendMessage"
      Resource  = aws_sqs_queue.document_landing.arn
      Condition = {
        ArnEquals = {
          "aws:SourceArn" = aws_cloudwatch_event_rule.document_landing_created.arn
        }
      }
    }]
  })
}

resource "aws_cloudwatch_event_target" "document_landing_sqs" {
  rule       = aws_cloudwatch_event_rule.document_landing_created.name
  arn        = aws_sqs_queue.document_landing.arn
  depends_on = [aws_sqs_queue_policy.document_landing]
}

resource "aws_iam_role_policy" "task_documents" {
  name = "origination-dev-task-documents"
  role = aws_iam_role.task.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "s3:GetObject",
          "s3:PutObject",
          "s3:DeleteObject",
        ]
        Resource = [
          "${aws_s3_bucket.document_landing.arn}/*",
          "${aws_s3_bucket.document_clean.arn}/*",
          "${aws_s3_bucket.document_quarantine.arn}/*",
        ]
      },
      {
        Effect = "Allow"
        Action = ["s3:ListBucket", "s3:GetBucketLocation"]
        Resource = [
          aws_s3_bucket.document_landing.arn,
          aws_s3_bucket.document_clean.arn,
          aws_s3_bucket.document_quarantine.arn,
        ]
      },
      {
        Effect = "Allow"
        Action = [
          "sqs:ReceiveMessage",
          "sqs:DeleteMessage",
          "sqs:GetQueueUrl",
          "sqs:GetQueueAttributes",
          "sqs:ChangeMessageVisibility",
        ]
        Resource = aws_sqs_queue.document_landing.arn
      }
    ]
  })
}

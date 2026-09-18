resource "aws_s3_bucket" "audit_archive" {
  bucket              = "origination-dev-audit-${data.aws_caller_identity.current.account_id}"
  object_lock_enabled = true
}

resource "aws_s3_bucket_public_access_block" "audit_archive" {
  bucket                  = aws_s3_bucket.audit_archive.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_server_side_encryption_configuration" "audit_archive" {
  bucket = aws_s3_bucket.audit_archive.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

resource "aws_s3_bucket_versioning" "audit_archive" {
  bucket = aws_s3_bucket.audit_archive.id

  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_object_lock_configuration" "audit_archive" {
  bucket = aws_s3_bucket.audit_archive.id

  rule {
    default_retention {
      mode  = "GOVERNANCE"
      years = 7
    }
  }

  depends_on = [aws_s3_bucket_versioning.audit_archive]
}

resource "aws_sqs_queue" "audit_events_dlq" {
  name                      = "audit-events-dlq"
  message_retention_seconds = 1209600
}

resource "aws_sqs_queue" "audit_events" {
  name                       = "audit-events"
  visibility_timeout_seconds = 60
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.audit_events_dlq.arn
    maxReceiveCount     = 5
  })
}

resource "aws_cloudwatch_event_rule" "audit_event" {
  name           = "audit-event"
  event_bus_name = aws_cloudwatch_event_bus.broker.name
  event_pattern = jsonencode({
    "detail-type" = ["AuditEvent"]
  })
}

resource "aws_sqs_queue_policy" "audit_events" {
  queue_url = aws_sqs_queue.audit_events.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Sid       = "AllowEventBridge"
      Effect    = "Allow"
      Principal = { Service = "events.amazonaws.com" }
      Action    = "sqs:SendMessage"
      Resource  = aws_sqs_queue.audit_events.arn
      Condition = {
        ArnEquals = {
          "aws:SourceArn" = aws_cloudwatch_event_rule.audit_event.arn
        }
      }
    }]
  })
}

resource "aws_cloudwatch_event_target" "audit_sqs" {
  rule           = aws_cloudwatch_event_rule.audit_event.name
  event_bus_name = aws_cloudwatch_event_bus.broker.name
  arn            = aws_sqs_queue.audit_events.arn
  depends_on     = [aws_sqs_queue_policy.audit_events]
}

resource "aws_iam_role_policy" "task_audit" {
  name = "origination-dev-task-audit"
  role = aws_iam_role.task.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect   = "Allow"
        Action   = ["s3:PutObject", "s3:GetObject"]
        Resource = "${aws_s3_bucket.audit_archive.arn}/*"
      },
      {
        Effect   = "Allow"
        Action   = ["s3:ListBucket", "s3:GetBucketLocation"]
        Resource = aws_s3_bucket.audit_archive.arn
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
        Resource = aws_sqs_queue.audit_events.arn
      }
    ]
  })
}

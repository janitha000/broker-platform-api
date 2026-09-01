resource "aws_cloudwatch_event_bus" "broker" {
  name = "broker-platform"
}

resource "aws_sqs_queue" "notification_dlq" {
  name                      = "notification-commands-dlq"
  message_retention_seconds = 1209600
}

resource "aws_sqs_queue" "notification_commands" {
  name                       = "notification-commands"
  visibility_timeout_seconds = 60
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.notification_dlq.arn
    maxReceiveCount     = 5
  })
}

resource "aws_sqs_queue_policy" "notification_commands" {
  queue_url = aws_sqs_queue.notification_commands.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Sid       = "AllowEventBridge"
      Effect    = "Allow"
      Principal = { Service = "events.amazonaws.com" }
      Action    = "sqs:SendMessage"
      Resource  = aws_sqs_queue.notification_commands.arn
      Condition = {
        ArnEquals = {
          "aws:SourceArn" = aws_cloudwatch_event_rule.case_fact_find_completed.arn
        }
      }
    }]
  })
}

resource "aws_cloudwatch_event_rule" "case_fact_find_completed" {
  name           = "origination-case-fact-find-completed"
  event_bus_name = aws_cloudwatch_event_bus.broker.name
  event_pattern = jsonencode({
    source        = ["origination.broker-platform"]
    "detail-type" = ["CaseFactFindCompleted"]
  })
}

resource "aws_cloudwatch_event_target" "notification_sqs" {
  rule           = aws_cloudwatch_event_rule.case_fact_find_completed.name
  event_bus_name = aws_cloudwatch_event_bus.broker.name
  arn            = aws_sqs_queue.notification_commands.arn
  depends_on     = [aws_sqs_queue_policy.notification_commands]
}

resource "aws_iam_role_policy" "task_messaging" {
  name = "origination-dev-task-messaging"
  role = aws_iam_role.task.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect   = "Allow"
        Action   = ["events:PutEvents"]
        Resource = aws_cloudwatch_event_bus.broker.arn
      },
      {
        Effect = "Allow"
        Action = [
          "sqs:ReceiveMessage",
          "sqs:DeleteMessage",
          "sqs:GetQueueUrl",
          "sqs:GetQueueAttributes",
          "sqs:ChangeMessageVisibility"
        ]
        Resource = aws_sqs_queue.notification_commands.arn
      }
    ]
  })
}
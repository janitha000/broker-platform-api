resource "aws_cloudwatch_log_group" "notification" {
  name              = "/ecs/notification-api"
  retention_in_days = 7
}

resource "aws_ecs_task_definition" "notification" {
  family                   = "notification-api"
  requires_compatibilities = ["FARGATE"]
  network_mode             = "awsvpc"
  cpu                      = "512"
  memory                   = "1024"
  execution_role_arn       = aws_iam_role.execution.arn
  task_role_arn            = aws_iam_role.task.arn

  runtime_platform {
    operating_system_family = "LINUX"
    cpu_architecture        = "X86_64"
  }

  container_definitions = jsonencode([{
    name      = "api"
    image     = "${aws_ecr_repository.notification.repository_url}:latest"
    essential = true
    portMappings = [{
      name          = "http"
      containerPort = 8080
      hostPort      = 8080
      protocol      = "tcp"
      appProtocol   = "http"
    }]
    healthCheck = {
      command     = ["CMD-SHELL", "timeout 2 bash -c ':> /dev/tcp/127.0.0.1/8080' || exit 1"]
      interval    = 30
      timeout     = 5
      retries     = 3
      startPeriod = 60
    }
    environment = [
      {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      },
      {
        name  = "Email__Provider"
        value = "Mock"
      },
      {
        name  = "Messaging__QueueUrl"
        value = aws_sqs_queue.notification_commands.url
      },
      {
        name  = "Messaging__AwsRegion"
        value = var.aws_region
      }
    ]
    secrets = [
      {
        name      = "ConnectionStrings__Notification"
        valueFrom = aws_secretsmanager_secret.notification_sql.arn
      }
    ]
    logConfiguration = {
      logDriver = "awslogs"
      options = {
        awslogs-group         = aws_cloudwatch_log_group.notification.name
        awslogs-region        = var.aws_region
        awslogs-stream-prefix = "ecs"
      }
    }
  }])

  depends_on = [aws_secretsmanager_secret_version.notification_sql]
}

resource "aws_ecs_service" "notification" {
  name            = "notification-api"
  cluster         = aws_ecs_cluster.this.id
  task_definition = aws_ecs_task_definition.notification.arn
  desired_count   = 1
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = module.vpc.public_subnets
    security_groups  = [aws_security_group.notification.id]
    assign_public_ip = true
  }
}

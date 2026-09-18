resource "aws_cloudwatch_log_group" "audit" {
  name              = "/ecs/audit-api"
  retention_in_days = 7
}

resource "aws_ecs_task_definition" "audit" {
  family                   = "audit-api"
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
    image     = "${aws_ecr_repository.audit.repository_url}:latest"
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
        name  = "Auth__Mode"
        value = "Auth0Organizations"
      },
      {
        name  = "Auth0__Domain"
        value = "dev-ggsd0s-z.us.auth0.com"
      },
      {
        name  = "Auth0__Audience"
        value = "https://api.broker-platform.com"
      },
      {
        name  = "Messaging__QueueUrl"
        value = aws_sqs_queue.audit_events.url
      },
      {
        name  = "Messaging__AwsRegion"
        value = var.aws_region
      },
      {
        name  = "Archive__AwsRegion"
        value = var.aws_region
      },
      {
        name  = "Archive__Bucket"
        value = aws_s3_bucket.audit_archive.id
      }
    ]
    secrets = [
      {
        name      = "ConnectionStrings__Audit"
        valueFrom = aws_secretsmanager_secret.audit_sql.arn
      }
    ]
    logConfiguration = {
      logDriver = "awslogs"
      options = {
        awslogs-group         = aws_cloudwatch_log_group.audit.name
        awslogs-region        = var.aws_region
        awslogs-stream-prefix = "ecs"
      }
    }
  }])

  depends_on = [aws_secretsmanager_secret_version.audit_sql]
}

resource "aws_ecs_service" "audit" {
  name            = "audit-api"
  cluster         = aws_ecs_cluster.this.id
  task_definition = aws_ecs_task_definition.audit.arn
  desired_count   = var.ecs_desired_count
  launch_type     = "FARGATE"

  health_check_grace_period_seconds = 120

  network_configuration {
    subnets          = module.vpc.public_subnets
    security_groups  = [aws_security_group.audit.id]
    assign_public_ip = true
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.audit.arn
    container_name   = "api"
    container_port   = 8080
  }

  depends_on = [aws_lb_listener_rule.audit_api]
}

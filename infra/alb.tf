resource "aws_lb" "this" {
  name               = "origination-dev"
  load_balancer_type = "application"
  subnets            = module.vpc.public_subnets
  security_groups    = [aws_security_group.alb.id]
  idle_timeout       = 400
}

resource "aws_lb_target_group" "api" {
  name        = "origination-api"
  port        = 8080
  protocol    = "HTTP"
  vpc_id      = module.vpc.vpc_id
  target_type = "ip"

  health_check {
    path                = "/health"
    matcher             = "200"
    healthy_threshold   = 2
    unhealthy_threshold = 3
  }
}

resource "aws_lb_listener" "http" {
  load_balancer_arn = aws_lb.this.arn
  port              = 80
  protocol          = "HTTP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.api.arn
  }
}

resource "aws_lb_target_group" "identity" {
  name        = "identity-api"
  port        = 8080
  protocol    = "HTTP"
  vpc_id      = module.vpc.vpc_id
  target_type = "ip"

  health_check {
    path                = "/health"
    matcher             = "200"
    healthy_threshold   = 2
    unhealthy_threshold = 3
  }
}

resource "aws_lb_listener_rule" "identity_auth" {
  listener_arn = aws_lb_listener.http.arn
  priority     = 10

  action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.identity.arn
  }

  condition {
    path_pattern {
      values = ["/auth", "/auth/*"]
    }
  }
}

resource "aws_lb_target_group" "document" {
  name        = "document-api"
  port        = 8080
  protocol    = "HTTP"
  vpc_id      = module.vpc.vpc_id
  target_type = "ip"

  health_check {
    path                = "/health"
    matcher             = "200"
    healthy_threshold   = 2
    unhealthy_threshold = 3
  }
}

resource "aws_lb_listener_rule" "document_api" {
  listener_arn = aws_lb_listener.http.arn
  priority     = 20

  action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.document.arn
  }

  condition {
    path_pattern {
      values = ["/documents", "/documents/*"]
    }
  }
}

resource "aws_lb_target_group" "audit" {
  name        = "audit-api"
  port        = 8080
  protocol    = "HTTP"
  vpc_id      = module.vpc.vpc_id
  target_type = "ip"

  health_check {
    path                = "/health"
    matcher             = "200"
    healthy_threshold   = 2
    unhealthy_threshold = 3
  }
}

resource "aws_lb_listener_rule" "audit_api" {
  listener_arn = aws_lb_listener.http.arn
  priority     = 25

  action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.audit.arn
  }

  condition {
    path_pattern {
      values = ["/audit", "/audit/*"]
    }
  }
}

resource "aws_lb_target_group" "notification" {
  name        = "notification-api"
  port        = 8080
  protocol    = "HTTP"
  vpc_id      = module.vpc.vpc_id
  target_type = "ip"

  health_check {
    path                = "/health"
    matcher             = "200"
    healthy_threshold   = 2
    unhealthy_threshold = 3
  }

  # SignalR negotiate + WebSocket should stick to one task when count > 1.
  stickiness {
    type            = "lb_cookie"
    enabled         = true
    cookie_duration = 3600
  }
}

resource "aws_lb_listener_rule" "notification_hubs" {
  listener_arn = aws_lb_listener.http.arn
  priority     = 30

  action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.notification.arn
  }

  condition {
    path_pattern {
      values = ["/hubs", "/hubs/*"]
    }
  }
}



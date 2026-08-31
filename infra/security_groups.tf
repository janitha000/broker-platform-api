resource "aws_security_group" "alb" {
  name   = "origination-alb-sg"
  vpc_id = module.vpc.vpc_id

  ingress {
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_security_group" "identity" {
  name   = "identity-api-sg"
  vpc_id = module.vpc.vpc_id

  ingress {
    description     = "ALB to Identity"
    from_port       = 8080
    to_port         = 8080
    protocol        = "tcp"
    security_groups = [aws_security_group.alb.id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_security_group" "origination" {
  name   = "origination-api-sg"
  vpc_id = module.vpc.vpc_id

  ingress {
    description     = "ALB to Origination"
    from_port       = 8080
    to_port         = 8080
    protocol        = "tcp"
    security_groups = [aws_security_group.alb.id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

# Payment is not on the ALB. Only Identity may open its port, so Origination
# cannot reach Payment even though both are in the Service Connect namespace.
resource "aws_security_group" "payment" {
  name   = "payment-api-sg"
  vpc_id = module.vpc.vpc_id

  ingress {
    description     = "Identity to Payment"
    from_port       = 8080
    to_port         = 8080
    protocol        = "tcp"
    security_groups = [aws_security_group.identity.id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_security_group" "rds" {
  name   = "origination-rds-sg"
  vpc_id = module.vpc.vpc_id

  ingress {
    from_port       = 1433
    to_port         = 1433
    protocol        = "tcp"
    security_groups = [aws_security_group.identity.id, aws_security_group.origination.id]
  }

  dynamic "ingress" {
    for_each = var.my_ip == "" ? [] : [var.my_ip]
    content {
      from_port   = 1433
      to_port     = 1433
      protocol    = "tcp"
      cidr_blocks = [ingress.value]
    }
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

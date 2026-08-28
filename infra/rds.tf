resource "aws_db_subnet_group" "this" {
  name       = "origination-dev"
  subnet_ids = module.vpc.public_subnets
}

resource "aws_db_instance" "this" {
  identifier     = "origination-dev"
  engine         = "sqlserver-ex"
  engine_version = "16.00"
  instance_class = "db.t3.small"
  license_model  = "license-included"

  allocated_storage   = 20
  storage_encrypted   = true
  publicly_accessible = true

  username = var.db_username
  password = var.db_password
  port     = 1433

  db_subnet_group_name   = aws_db_subnet_group.this.name
  vpc_security_group_ids = [aws_security_group.rds.id]

  skip_final_snapshot = true
  deletion_protection = false
  apply_immediately   = true
}
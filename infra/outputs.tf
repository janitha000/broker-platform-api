output "ecr_repository_url" {
  value = aws_ecr_repository.api.repository_url
}

output "rds_endpoint" {
  value = aws_db_instance.this.address
}

output "alb_dns_name" {
  value = aws_lb.this.dns_name
}

output "sql_secret_arn" {
  value = aws_secretsmanager_secret.sql.arn
}

output "ecs_cluster_name" {
  value = aws_ecs_cluster.this.name
}

output "execution_role_arn" {
  value = aws_iam_role.execution.arn
}

output "task_role_arn" {
  value = aws_iam_role.task.arn
}

output "private_subnet_ids" {
  value = module.vpc.private_subnets
}

output "ecs_security_group_id" {
  value = aws_security_group.ecs.id
}

output "identity_ecr_repository_url" {
  value = aws_ecr_repository.identity.repository_url
}

output "payment_ecr_repository_url" {
  value = aws_ecr_repository.payment.repository_url
}

output "identity_sql_secret_arn" {
  value = aws_secretsmanager_secret.identity_sql.arn
}

output "ui_bucket_name" {
  value = aws_s3_bucket.ui.id
}

output "ui_cloudfront_domain" {
  value = aws_cloudfront_distribution.ui.domain_name
}

output "ui_cloudfront_distribution_id" {
  value = aws_cloudfront_distribution.ui.id
}

output "ui_url" {
  value = "https://${aws_cloudfront_distribution.ui.domain_name}"
}
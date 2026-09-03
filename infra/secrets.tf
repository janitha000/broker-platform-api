resource "aws_secretsmanager_secret" "sql" {
  name = "origination/dev/sql"
}

resource "aws_secretsmanager_secret_version" "sql" {
  secret_id = aws_secretsmanager_secret.sql.id
  secret_string = join("", [
    "Server=", aws_db_instance.this.address, ",1433;",
    "Database=Origination;",
    "User Id=", var.db_username, ";",
    "Password=", var.db_password, ";",
    "TrustServerCertificate=True;Encrypt=True"
  ])
}

resource "aws_secretsmanager_secret" "identity_sql" {
  name = "identity/dev/sql"
}

resource "aws_secretsmanager_secret_version" "identity_sql" {
  secret_id = aws_secretsmanager_secret.identity_sql.id
  secret_string = join("", [
    "Server=", aws_db_instance.this.address, ",1433;",
    "Database=Identity;",
    "User Id=", var.db_username, ";",
    "Password=", var.db_password, ";",
    "TrustServerCertificate=True;Encrypt=True"
  ])
}

resource "aws_secretsmanager_secret" "notification_sql" {
  name = "notification/dev/sql"
}

resource "aws_secretsmanager_secret_version" "notification_sql" {
  secret_id = aws_secretsmanager_secret.notification_sql.id
  secret_string = join("", [
    "Server=", aws_db_instance.this.address, ",1433;",
    "Database=Notification;",
    "User Id=", var.db_username, ";",
    "Password=", var.db_password, ";",
    "TrustServerCertificate=True;Encrypt=True"
  ])
}

resource "aws_secretsmanager_secret" "jwt" {
  name = "origination/dev/jwt"
}

resource "aws_secretsmanager_secret_version" "jwt" {
  secret_id     = aws_secretsmanager_secret.jwt.id
  secret_string = var.jwt_signing_key
}

resource "aws_secretsmanager_secret" "auth0_client" {
  name                    = "identity/dev/auth0-bff"
  recovery_window_in_days = 0
}

resource "aws_secretsmanager_secret_version" "auth0_client" {
  secret_id     = aws_secretsmanager_secret.auth0_client.id
  secret_string = var.auth0_client_secret
}

resource "aws_secretsmanager_secret" "auth0_management" {
  name                    = "identity/dev/auth0-mgmt"
  recovery_window_in_days = 0
}

resource "aws_secretsmanager_secret_version" "auth0_management" {
  secret_id     = aws_secretsmanager_secret.auth0_management.id
  secret_string = var.auth0_management_client_secret
}
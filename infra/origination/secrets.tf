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

resource "aws_secretsmanager_secret" "jwt" {
  name = "origination/dev/jwt"
}

resource "aws_secretsmanager_secret_version" "jwt" {
  secret_id     = aws_secretsmanager_secret.jwt.id
  secret_string = var.jwt_signing_key
}
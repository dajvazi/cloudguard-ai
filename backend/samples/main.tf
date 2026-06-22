resource "aws_api_gateway_rest_api" "api_gateway" {
  name = "cloudguard-api"
}

resource "aws_db_instance" "database" {
  engine         = "postgres"
  instance_class = "db.t3.micro"
}

resource "aws_lambda_function" "ai_engine" {
  function_name = "cloudguard-ai-engine"
  runtime       = "python3.11"
}

resource "aws_ecs_service" "notification_service" {
  name = "notification-service"
}

module "authentication" {
  source = "./modules/auth"
}

module "database" {
  source = "./modules/database"
}

data "aws_caller_identity" "current" {}

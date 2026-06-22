terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = var.region
}

variable "region" {
  default = "eu-central-1"
}

variable "environment" {
  default = "production"
}

# API Gateway - entry point for all client requests
resource "aws_api_gateway_rest_api" "cloudguard_api" {
  name        = "cloudguard-api"
  description = "Main API gateway for CloudGuard platform"
}

# AI Engine - anomaly detection and ML inference
resource "aws_lambda_function" "ai_engine" {
  function_name = "cloudguard-ai-engine"
  runtime       = "python3.11"
  handler       = "main.handler"
  memory_size   = 512
  timeout       = 30
  filename      = "ai_engine.zip"

  environment {
    variables = {
      ENV        = var.environment
      MODEL_PATH = "/opt/ml/model"
    }
  }
}

# PostgreSQL Database
resource "aws_db_instance" "main_database" {
  identifier     = "cloudguard-db"
  engine         = "postgres"
  engine_version = "16.2"
  instance_class = "db.t3.medium"
  allocated_storage = 50

  db_name  = "cloudguard"
  username = "admin"
  password = "managed-by-secrets-manager"

  backup_retention_period = 7
  multi_az               = true
  skip_final_snapshot    = false
}

# Notification Service - runs as ECS container
resource "aws_ecs_service" "notification_worker" {
  name            = "cloudguard-notifications"
  cluster         = "cloudguard-cluster"
  task_definition = "notification-task:latest"
  desired_count   = 2
  launch_type     = "FARGATE"
}

# Log Storage
resource "aws_s3_bucket" "logs" {
  bucket = "cloudguard-logs-${var.environment}"
}

# Monitoring Alarm - triggers self-healing
resource "aws_cloudwatch_metric_alarm" "high_cpu" {
  alarm_name          = "cloudguard-high-cpu"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 2
  metric_name         = "CPUUtilization"
  namespace           = "AWS/ECS"
  period              = 60
  statistic           = "Average"
  threshold           = 80
  alarm_description   = "Triggers recovery when CPU exceeds 80%"
  alarm_actions       = [aws_sns_topic.alerts.arn]
}

# Alert Topic - incident notifications
resource "aws_sns_topic" "alerts" {
  name = "cloudguard-incident-alerts"
}

resource "aws_cognito_user_pool" "auth_pool" {
  name = "cloudguard-auth-pool"
}

resource "aws_cognito_user_pool_client" "auth_client" {
  name         = "cloudguard-auth-client"
  user_pool_id = aws_cognito_user_pool.auth_pool.id
}

resource "aws_lambda_function" "token_validator" {
  function_name = "cloudguard-token-validator"
  runtime       = "nodejs20.x"
}

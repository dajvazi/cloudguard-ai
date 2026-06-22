resource "aws_db_instance" "auth_database" {
  engine         = "postgres"
  instance_class = "db.t3.micro"
  db_name        = "cloudguard_auth"
}

resource "aws_db_subnet_group" "auth_subnet" {
  name       = "cloudguard-auth-subnet"
  subnet_ids = []
}

module "backup" {
  source = "../backup"
}

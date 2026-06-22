resource "aws_s3_bucket" "db_backups" {
  bucket = "cloudguard-db-backups"
}

resource "aws_s3_bucket_versioning" "db_backups_versioning" {
  bucket = aws_s3_bucket.db_backups.id
}

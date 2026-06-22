using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudGuard.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTerraformUploadRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "cloud_services",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<int>(
                name: "terraform_upload_id",
                table: "cloud_services",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_cloud_services_terraform_upload_id",
                table: "cloud_services",
                column: "terraform_upload_id");

            migrationBuilder.AddForeignKey(
                name: "FK_cloud_services_terraform_uploads_terraform_upload_id",
                table: "cloud_services",
                column: "terraform_upload_id",
                principalTable: "terraform_uploads",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cloud_services_terraform_uploads_terraform_upload_id",
                table: "cloud_services");

            migrationBuilder.DropIndex(
                name: "IX_cloud_services_terraform_upload_id",
                table: "cloud_services");

            migrationBuilder.DropColumn(
                name: "terraform_upload_id",
                table: "cloud_services");

            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "cloud_services",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
        }
    }
}

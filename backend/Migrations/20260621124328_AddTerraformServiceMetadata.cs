using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudGuard.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTerraformServiceMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "module_source",
                table: "cloud_services",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "parent_module",
                table: "cloud_services",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "raw_resource_type",
                table: "cloud_services",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_file",
                table: "cloud_services",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_kind",
                table: "cloud_services",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "resource");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "module_source",
                table: "cloud_services");

            migrationBuilder.DropColumn(
                name: "parent_module",
                table: "cloud_services");

            migrationBuilder.DropColumn(
                name: "raw_resource_type",
                table: "cloud_services");

            migrationBuilder.DropColumn(
                name: "source_file",
                table: "cloud_services");

            migrationBuilder.DropColumn(
                name: "source_kind",
                table: "cloud_services");
        }
    }
}

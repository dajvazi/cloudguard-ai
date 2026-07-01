using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudGuard.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExtendedMetricColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "description",
                table: "recovery_actions",
                newName: "details");

            migrationBuilder.AlterColumn<decimal>(
                name: "memory_usage",
                table: "metrics",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "cpu_usage",
                table: "metrics",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "disk_read_bytes",
                table: "metrics",
                type: "numeric(15,2)",
                precision: 15,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "disk_write_bytes",
                table: "metrics",
                type: "numeric(15,2)",
                precision: 15,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "maximum",
                table: "metrics",
                type: "numeric(15,2)",
                precision: 15,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "metric_name",
                table: "metrics",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "minimum",
                table: "metrics",
                type: "numeric(15,2)",
                precision: 15,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "network_in",
                table: "metrics",
                type: "numeric(15,2)",
                precision: 15,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "network_out",
                table: "metrics",
                type: "numeric(15,2)",
                precision: 15,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "unit",
                table: "metrics",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "value",
                table: "metrics",
                type: "numeric(15,2)",
                precision: 15,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "disk_read_bytes",
                table: "metrics");

            migrationBuilder.DropColumn(
                name: "disk_write_bytes",
                table: "metrics");

            migrationBuilder.DropColumn(
                name: "maximum",
                table: "metrics");

            migrationBuilder.DropColumn(
                name: "metric_name",
                table: "metrics");

            migrationBuilder.DropColumn(
                name: "minimum",
                table: "metrics");

            migrationBuilder.DropColumn(
                name: "network_in",
                table: "metrics");

            migrationBuilder.DropColumn(
                name: "network_out",
                table: "metrics");

            migrationBuilder.DropColumn(
                name: "unit",
                table: "metrics");

            migrationBuilder.DropColumn(
                name: "value",
                table: "metrics");

            migrationBuilder.RenameColumn(
                name: "details",
                table: "recovery_actions",
                newName: "description");

            migrationBuilder.AlterColumn<decimal>(
                name: "memory_usage",
                table: "metrics",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldPrecision: 10,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "cpu_usage",
                table: "metrics",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldPrecision: 10,
                oldScale: 2,
                oldNullable: true);
        }
    }
}

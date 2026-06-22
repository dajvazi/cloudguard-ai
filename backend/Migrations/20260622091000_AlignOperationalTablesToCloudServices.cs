using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudGuard.Api.Migrations;

/// <inheritdoc />
public partial class AlignOperationalTablesToCloudServices : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE metrics DROP CONSTRAINT IF EXISTS fk_metrics_resource;
            ALTER TABLE anomalies DROP CONSTRAINT IF EXISTS fk_anomaly_resource;
            ALTER TABLE incidents DROP CONSTRAINT IF EXISTS fk_incident_resource;

            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public' AND table_name = 'metrics' AND column_name = 'resource_id'
                ) AND NOT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public' AND table_name = 'metrics' AND column_name = 'cloud_service_id'
                ) THEN
                    ALTER TABLE metrics RENAME COLUMN resource_id TO cloud_service_id;
                END IF;

                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public' AND table_name = 'anomalies' AND column_name = 'resource_id'
                ) AND NOT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public' AND table_name = 'anomalies' AND column_name = 'cloud_service_id'
                ) THEN
                    ALTER TABLE anomalies RENAME COLUMN resource_id TO cloud_service_id;
                END IF;

                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public' AND table_name = 'incidents' AND column_name = 'resource_id'
                ) AND NOT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public' AND table_name = 'incidents' AND column_name = 'cloud_service_id'
                ) THEN
                    ALTER TABLE incidents RENAME COLUMN resource_id TO cloud_service_id;
                END IF;
            END $$;

            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'FK_incidents_cloud_services_cloud_service_id'
                ) THEN
                    ALTER TABLE incidents
                        ADD CONSTRAINT "FK_incidents_cloud_services_cloud_service_id"
                        FOREIGN KEY (cloud_service_id) REFERENCES cloud_services(id) ON DELETE CASCADE;
                END IF;

                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'FK_metrics_cloud_services_cloud_service_id'
                ) THEN
                    ALTER TABLE metrics
                        ADD CONSTRAINT "FK_metrics_cloud_services_cloud_service_id"
                        FOREIGN KEY (cloud_service_id) REFERENCES cloud_services(id) ON DELETE CASCADE;
                END IF;

                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'FK_anomalies_cloud_services_cloud_service_id'
                ) THEN
                    ALTER TABLE anomalies
                        ADD CONSTRAINT "FK_anomalies_cloud_services_cloud_service_id"
                        FOREIGN KEY (cloud_service_id) REFERENCES cloud_services(id) ON DELETE CASCADE;
                END IF;
            END $$;

            UPDATE incidents
            SET cloud_service_id = (
                SELECT id FROM cloud_services WHERE name = 'Database Service' ORDER BY id DESC LIMIT 1
            )
            WHERE cloud_service_id IS NULL AND title ILIKE '%database%';

            UPDATE incidents
            SET cloud_service_id = (
                SELECT id FROM cloud_services WHERE name = 'Notification Service' ORDER BY id DESC LIMIT 1
            )
            WHERE cloud_service_id IS NULL AND title ILIKE '%notification%';

            UPDATE incidents
            SET cloud_service_id = (
                SELECT id FROM cloud_services WHERE name = 'AI Module' ORDER BY id DESC LIMIT 1
            )
            WHERE cloud_service_id IS NULL AND title ILIKE '%ai%';
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE metrics DROP CONSTRAINT IF EXISTS "FK_metrics_cloud_services_cloud_service_id";
            ALTER TABLE anomalies DROP CONSTRAINT IF EXISTS "FK_anomalies_cloud_services_cloud_service_id";
            ALTER TABLE incidents DROP CONSTRAINT IF EXISTS "FK_incidents_cloud_services_cloud_service_id";

            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public' AND table_name = 'metrics' AND column_name = 'cloud_service_id'
                ) AND NOT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public' AND table_name = 'metrics' AND column_name = 'resource_id'
                ) THEN
                    ALTER TABLE metrics RENAME COLUMN cloud_service_id TO resource_id;
                END IF;

                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public' AND table_name = 'anomalies' AND column_name = 'cloud_service_id'
                ) AND NOT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public' AND table_name = 'anomalies' AND column_name = 'resource_id'
                ) THEN
                    ALTER TABLE anomalies RENAME COLUMN cloud_service_id TO resource_id;
                END IF;

                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public' AND table_name = 'incidents' AND column_name = 'cloud_service_id'
                ) AND NOT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public' AND table_name = 'incidents' AND column_name = 'resource_id'
                ) THEN
                    ALTER TABLE incidents RENAME COLUMN cloud_service_id TO resource_id;
                END IF;
            END $$;
            """);
    }
}

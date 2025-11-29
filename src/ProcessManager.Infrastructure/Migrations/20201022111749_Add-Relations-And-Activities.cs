using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ProcessManager.Infrastructure.Migrations
{
    public partial class AddRelationsAndActivities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkflowRuns",
                table: "WorkflowRuns");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowRuns_OperationId",
                table: "WorkflowRuns");

            migrationBuilder.DropColumn(
                name: "WorkflowRunId",
                table: "WorkflowRuns");

            migrationBuilder.RenameColumn(
                name: "WorkFlowRunName",
                table: "WorkflowRuns",
                newName: "WorkflowRunName");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkflowRuns",
                table: "WorkflowRuns",
                column: "OperationId");

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    ActivityId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Status = table.Column<string>(nullable: false),
                    URI = table.Column<string>(nullable: true),
                    StartDate = table.Column<DateTime>(nullable: false),
                    EndDate = table.Column<DateTime>(nullable: false),
                    OperationId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.ActivityId);
                    table.ForeignKey(
                        name: "FK_Activities_WorkflowRuns_OperationId",
                        column: x => x.OperationId,
                        principalTable: "WorkflowRuns",
                        principalColumn: "OperationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Relations",
                columns: table => new
                {
                    EntityId = table.Column<Guid>(nullable: false),
                    EntityType = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relations", x => x.EntityId);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowRelations",
                columns: table => new
                {
                    OperationId = table.Column<Guid>(nullable: false),
                    EntityId = table.Column<Guid>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ChangedBy = table.Column<string>(nullable: true),
                    ChangedDate = table.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowRelations", x => new { x.OperationId, x.EntityId });
                    table.ForeignKey(
                        name: "FK_WorkflowRelations_Relations_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Relations",
                        principalColumn: "EntityId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowRelations_WorkflowRuns_OperationId",
                        column: x => x.OperationId,
                        principalTable: "WorkflowRuns",
                        principalColumn: "OperationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_OperationId",
                table: "Activities",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRelations_EntityId",
                table: "WorkflowRelations",
                column: "EntityId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "WorkflowRelations");

            migrationBuilder.DropTable(
                name: "Relations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkflowRuns",
                table: "WorkflowRuns");

            migrationBuilder.RenameColumn(
                name: "WorkflowRunName",
                table: "WorkflowRuns",
                newName: "WorkFlowRunName");

            migrationBuilder.AddColumn<Guid>(
                name: "WorkflowRunId",
                table: "WorkflowRuns",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.NewGuid());

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkflowRuns",
                table: "WorkflowRuns",
                column: "WorkflowRunId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRuns_OperationId",
                table: "WorkflowRuns",
                column: "OperationId",
                unique: true);
        }
    }
}

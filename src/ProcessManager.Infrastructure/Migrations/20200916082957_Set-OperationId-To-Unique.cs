using Microsoft.EntityFrameworkCore.Migrations;

namespace ProcessManager.Infrastructure.Migrations
{
    public partial class SetOperationIdToUnique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRuns_OperationId",
                table: "WorkflowRuns",
                column: "OperationId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowRuns_OperationId",
                table: "WorkflowRuns");
        }
    }
}

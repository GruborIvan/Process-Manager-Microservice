using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ProcessManager.Infrastructure.Migrations
{
    public partial class AddEndDateToWorflow : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "WorkflowRuns",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "WorkflowRuns");
        }
    }
}

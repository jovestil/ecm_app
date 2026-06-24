using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddViewpointSyncDateToTerminationReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EmployeeID",
                table: "NewHireRequestDetails",
                newName: "EmployeeId");

            migrationBuilder.AddColumn<DateTime>(
                name: "ViewpointSyncDate",
                table: "TerminationReasons",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 1,
                column: "ViewpointSyncDate",
                value: null);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 2,
                column: "ViewpointSyncDate",
                value: null);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 3,
                column: "ViewpointSyncDate",
                value: null);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 4,
                column: "ViewpointSyncDate",
                value: null);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 5,
                column: "ViewpointSyncDate",
                value: null);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 6,
                column: "ViewpointSyncDate",
                value: null);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 7,
                column: "ViewpointSyncDate",
                value: null);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 8,
                column: "ViewpointSyncDate",
                value: null);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 9,
                column: "ViewpointSyncDate",
                value: null);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 10,
                column: "ViewpointSyncDate",
                value: null);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 11,
                column: "ViewpointSyncDate",
                value: null);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 12,
                column: "ViewpointSyncDate",
                value: null);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 13,
                column: "ViewpointSyncDate",
                value: null);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 14,
                column: "ViewpointSyncDate",
                value: null);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 15,
                column: "ViewpointSyncDate",
                value: null);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 16,
                column: "ViewpointSyncDate",
                value: null);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 17,
                column: "ViewpointSyncDate",
                value: null);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 18,
                column: "ViewpointSyncDate",
                value: null);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 19,
                column: "ViewpointSyncDate",
                value: null);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 20,
                column: "ViewpointSyncDate",
                value: null);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 21,
                column: "ViewpointSyncDate",
                value: null);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 22,
                column: "ViewpointSyncDate",
                value: null);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 23,
                column: "ViewpointSyncDate",
                value: null);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 24,
                column: "ViewpointSyncDate",
                value: null);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 25,
                column: "ViewpointSyncDate",
                value: null);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 26,
                column: "ViewpointSyncDate",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ViewpointSyncDate",
                table: "TerminationReasons");

            migrationBuilder.RenameColumn(
                name: "EmployeeId",
                table: "NewHireRequestDetails",
                newName: "EmployeeID");
        }
    }
}

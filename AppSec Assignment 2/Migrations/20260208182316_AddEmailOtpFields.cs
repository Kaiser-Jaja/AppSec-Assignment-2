using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppSec_Assignment_2.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailOtpFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TwoFactorOtpCode",
                table: "Members",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TwoFactorOtpExpiry",
                table: "Members",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TwoFactorOtpCode",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "TwoFactorOtpExpiry",
                table: "Members");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppSec_Assignment_2.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedSecurityFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastPasswordChangeAt",
                table: "Members",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                table: "Members",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiry",
                table: "Members",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousPasswordHash1",
                table: "Members",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousPasswordHash2",
                table: "Members",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TwoFactorEnabled",
                table: "Members",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TwoFactorSecretKey",
                table: "Members",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastPasswordChangeAt",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiry",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "PreviousPasswordHash1",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "PreviousPasswordHash2",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "TwoFactorEnabled",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "TwoFactorSecretKey",
                table: "Members");
        }
    }
}

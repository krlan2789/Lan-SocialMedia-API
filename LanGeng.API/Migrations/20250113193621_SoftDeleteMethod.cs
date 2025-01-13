using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LanGeng.API.Migrations
{
    /// <inheritdoc />
    public partial class SoftDeleteMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "UserPosts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "PostComments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Groups",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "UserPosts");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PostComments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Groups");
        }
    }
}

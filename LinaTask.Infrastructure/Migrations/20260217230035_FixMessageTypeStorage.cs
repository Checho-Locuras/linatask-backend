using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinaTask.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixMessageTypeStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AlterColumn<string>(
                name: "module",
                table: "permissions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "permissions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "permissions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "idx_conversation_unique",
                table: "conversations",
                columns: new[] { "user_one_id", "user_two_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_conversations_user_two_id",
                table: "conversations",
                column: "user_two_id");

            migrationBuilder.CreateIndex(
                name: "idx_menus_is_visible",
                table: "menus",
                column: "is_visible");

            migrationBuilder.CreateIndex(
                name: "idx_menus_order",
                table: "menus",
                column: "order");

            migrationBuilder.CreateIndex(
                name: "idx_menus_parent_id",
                table: "menus",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "idx_messages_conversation",
                table: "messages",
                columns: new[] { "conversation_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_messages_sender",
                table: "messages",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "idx_messages_unread",
                table: "messages",
                columns: new[] { "conversation_id", "is_read" },
                filter: "is_read = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "module",
                table: "permissions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "permissions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "permissions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EquipmentSolver.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerId = table.Column<string>(type: "text", nullable: false),
                    IgdbGameId = table.Column<int>(type: "integer", nullable: false),
                    GameName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GameCoverUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    VoteScore = table.Column<int>(type: "integer", nullable: false),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameProfiles_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Equipment_GameProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "GameProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentSlots_GameProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "GameProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfilePatchNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileId = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Content = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfilePatchNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfilePatchNotes_GameProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "GameProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfileUsages",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ProfileId = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileUsages", x => new { x.UserId, x.ProfileId });
                    table.ForeignKey(
                        name: "FK_ProfileUsages_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProfileUsages_GameProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "GameProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProfileVotes",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ProfileId = table.Column<int>(type: "integer", nullable: false),
                    Vote = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileVotes", x => new { x.UserId, x.ProfileId });
                    table.ForeignKey(
                        name: "FK_ProfileVotes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProfileVotes_GameProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "GameProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SolverPresets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolverPresets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolverPresets_GameProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "GameProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StatTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatTypes_GameProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "GameProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserEquipmentStates",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    EquipmentId = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEquipmentStates", x => new { x.UserId, x.EquipmentId });
                    table.ForeignKey(
                        name: "FK_UserEquipmentStates_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserEquipmentStates_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EquipmentSlotCompatibilities",
                columns: table => new
                {
                    EquipmentId = table.Column<int>(type: "integer", nullable: false),
                    SlotId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentSlotCompatibilities", x => new { x.EquipmentId, x.SlotId });
                    table.ForeignKey(
                        name: "FK_EquipmentSlotCompatibilities_EquipmentSlots_SlotId",
                        column: x => x.SlotId,
                        principalTable: "EquipmentSlots",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EquipmentSlotCompatibilities_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSlotStates",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    SlotId = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSlotStates", x => new { x.UserId, x.SlotId });
                    table.ForeignKey(
                        name: "FK_UserSlotStates_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSlotStates_EquipmentSlots_SlotId",
                        column: x => x.SlotId,
                        principalTable: "EquipmentSlots",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EquipmentStats",
                columns: table => new
                {
                    EquipmentId = table.Column<int>(type: "integer", nullable: false),
                    StatTypeId = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentStats", x => new { x.EquipmentId, x.StatTypeId });
                    table.ForeignKey(
                        name: "FK_EquipmentStats_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EquipmentStats_StatTypes_StatTypeId",
                        column: x => x.StatTypeId,
                        principalTable: "StatTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SolverConstraints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PresetId = table.Column<int>(type: "integer", nullable: false),
                    StatTypeId = table.Column<int>(type: "integer", nullable: false),
                    Operator = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolverConstraints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolverConstraints_SolverPresets_PresetId",
                        column: x => x.PresetId,
                        principalTable: "SolverPresets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SolverConstraints_StatTypes_StatTypeId",
                        column: x => x.StatTypeId,
                        principalTable: "StatTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SolverPriorities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PresetId = table.Column<int>(type: "integer", nullable: false),
                    StatTypeId = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolverPriorities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolverPriorities_SolverPresets_PresetId",
                        column: x => x.PresetId,
                        principalTable: "SolverPresets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SolverPriorities_StatTypes_StatTypeId",
                        column: x => x.StatTypeId,
                        principalTable: "StatTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_ProfileId",
                table: "Equipment",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentSlotCompatibilities_SlotId",
                table: "EquipmentSlotCompatibilities",
                column: "SlotId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentSlots_ProfileId_Name",
                table: "EquipmentSlots",
                columns: new[] { "ProfileId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentStats_StatTypeId",
                table: "EquipmentStats",
                column: "StatTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_GameProfiles_IgdbGameId",
                table: "GameProfiles",
                column: "IgdbGameId");

            migrationBuilder.CreateIndex(
                name: "IX_GameProfiles_IsPublic",
                table: "GameProfiles",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_GameProfiles_OwnerId",
                table: "GameProfiles",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfilePatchNotes_ProfileId",
                table: "ProfilePatchNotes",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileUsages_ProfileId",
                table: "ProfileUsages",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileVotes_ProfileId",
                table: "ProfileVotes",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_SolverConstraints_PresetId",
                table: "SolverConstraints",
                column: "PresetId");

            migrationBuilder.CreateIndex(
                name: "IX_SolverConstraints_StatTypeId",
                table: "SolverConstraints",
                column: "StatTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SolverPresets_ProfileId",
                table: "SolverPresets",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_SolverPriorities_PresetId",
                table: "SolverPriorities",
                column: "PresetId");

            migrationBuilder.CreateIndex(
                name: "IX_SolverPriorities_StatTypeId",
                table: "SolverPriorities",
                column: "StatTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_StatTypes_ProfileId_Name",
                table: "StatTypes",
                columns: new[] { "ProfileId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserEquipmentStates_EquipmentId",
                table: "UserEquipmentStates",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSlotStates_SlotId",
                table: "UserSlotStates",
                column: "SlotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "EquipmentSlotCompatibilities");

            migrationBuilder.DropTable(
                name: "EquipmentStats");

            migrationBuilder.DropTable(
                name: "ProfilePatchNotes");

            migrationBuilder.DropTable(
                name: "ProfileUsages");

            migrationBuilder.DropTable(
                name: "ProfileVotes");

            migrationBuilder.DropTable(
                name: "SolverConstraints");

            migrationBuilder.DropTable(
                name: "SolverPriorities");

            migrationBuilder.DropTable(
                name: "UserEquipmentStates");

            migrationBuilder.DropTable(
                name: "UserSlotStates");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "SolverPresets");

            migrationBuilder.DropTable(
                name: "StatTypes");

            migrationBuilder.DropTable(
                name: "Equipment");

            migrationBuilder.DropTable(
                name: "EquipmentSlots");

            migrationBuilder.DropTable(
                name: "GameProfiles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}

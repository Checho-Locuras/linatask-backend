using LinaTask.Domain.Enums;
using LinaTask.Domain.Models;
using LinaTask.Domain.Models.Chat;
using Microsoft.EntityFrameworkCore;

namespace LinaTask.Infrastructure.DataBaseContext
{
    public class LinaTaskDbContext : DbContext
    {
        public LinaTaskDbContext(DbContextOptions<LinaTaskDbContext> options)
            : base(options) { }

        // =========================
        // CORE
        // =========================
        public DbSet<User> Users { get; set; }
        public DbSet<TeacherProfile> TeacherProfiles { get; set; }
        public DbSet<TutoringSession> TutoringSessions { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<TeacherSubject> TeacherSubjects { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<UserAcademicProfile> UserAcademicProfiles { get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<SystemParameter> SystemParameters { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<TeacherAvailability> TeacherAvailabilities { get; set; }
        public DbSet<SessionRating> SessionRatings { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();

        // =========================
        // LOCATION / UTILITIES
        // =========================
        public DbSet<Country> Countries { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Institution> Institutions { get; set; }

        // =========================
        // MENU & PERMISSIONS
        // =========================
        public DbSet<Menu> Menus { get; set; }

        // =========================
        // CHAT
        // =========================
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }

        public DbSet<MenuPermission> MenuPermissions { get; set; }

        // =========================
        // MARKET-PLACE
        // =========================
        public DbSet<MarketplaceTask> MarketplaceTasks { get; set; }
        public DbSet<TaskOffer> TaskOffers { get; set; }
        public DbSet<TaskAttachment> TaskAttachments { get; set; }
        public DbSet<MarketplacePayment> MarketplacePayments { get; set; }
        public DbSet<TaskCorrectionRequest> TaskCorrectionRequests { get; set; }
        public DbSet<MarketplaceRating> MarketplaceRatings { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================
            // USER
            // =========================
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");

                entity.HasKey(u => u.Id);

                entity.HasIndex(u => u.Email)
                    .IsUnique();

                entity.Property(u => u.Rating)
                    .HasPrecision(2, 1);

                entity.Property(u => u.BirthDate)
                    .HasColumnType("date");

                entity.Property(u => u.ProfilePhoto)
                    .HasColumnType("text")
                    .IsRequired(false);

                
                entity.HasMany(u => u.UserRoles)
                    .WithOne(ur => ur.User)
                    .HasForeignKey(ur => ur.UserId);
            });


            // =========================
            // TEACHER PROFILE
            // =========================
            modelBuilder.Entity<TeacherProfile>(entity =>
            {
                entity.HasIndex(tp => tp.UserId).IsUnique();
                entity.HasOne(tp => tp.User)
                      .WithOne(u => u.TeacherProfile)
                      .HasForeignKey<TeacherProfile>(tp => tp.UserId);
            });

            // =========================
            // TUTORING SESSION
            // =========================
            modelBuilder.Entity<TutoringSession>(e =>
            {
                e.HasKey(s => s.Id);

                // ─────────────────────────────────────
                // Status (Enum → string)
                // ─────────────────────────────────────
                e.Property(s => s.Status)
                 .HasConversion<string>()   // Guarda "Scheduled", "Ready", etc.
                 .HasMaxLength(20)
                 .IsRequired();

                // ─────────────────────────────────────
                // Precio
                // ─────────────────────────────────────
                e.Property(s => s.TotalPrice)
                 .HasColumnType("decimal(10,2)");

                // ─────────────────────────────────────
                // Relaciones Student / Teacher
                // ─────────────────────────────────────
                e.HasOne(s => s.Student)
                 .WithMany(u => u.TutoringSessionsAsStudent) 
                 .HasForeignKey(s => s.StudentId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(s => s.Teacher)
                 .WithMany(u => u.TutoringSessionsAsTeacher)
                 .HasForeignKey(s => s.TeacherId)
                 .OnDelete(DeleteBehavior.Restrict);

                // ─────────────────────────────────────
                // Subject (opcional)
                // ─────────────────────────────────────
                e.HasOne(s => s.Subject)
                 .WithMany()
                 .HasForeignKey(s => s.SubjectId)
                 .IsRequired(false)
                 .OnDelete(DeleteBehavior.SetNull);

                // ─────────────────────────────────────
                // Rating (One to One)
                // ─────────────────────────────────────
                e.HasOne(s => s.Rating)
                 .WithOne(r => r.Session)
                 .HasForeignKey<TutoringSession>(s => s.RatingId)
                 .IsRequired(false)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // ── SessionRating ────────────────────────────────────────
            modelBuilder.Entity<SessionRating>(e =>
            {
                e.HasKey(r => r.Id);

                // SessionRating → TutoringSession
                // DeleteBehavior.Cascade: si se borra la sesión, se borra la calificación
                e.HasOne(r => r.Session)
                 .WithOne(s => s.Rating)
                 .HasForeignKey<SessionRating>(r => r.SessionId)
                 .OnDelete(DeleteBehavior.Cascade);

                // SessionRating → User (quien calificó)
                // Restrict para no borrar calificaciones si se borra el usuario
                e.HasOne(r => r.RatedByUser)
                 .WithMany()
                 .HasForeignKey(r => r.RatedByUserId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.Property(r => r.Score)
                 .IsRequired();

                e.Property(r => r.Comment)
                 .HasMaxLength(1000);
            });

            // ── Notificaciones ────────────────────────────────────────
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("notifications");

                entity.Property(e => e.Id)
                   .HasColumnName("id")
                   .HasDefaultValueSql("gen_random_uuid()");

                entity.Property(e => e.Id)
                    .HasColumnName("id");

                entity.Property(e => e.UserId)
                    .HasColumnName("user_id")
                    .IsRequired();

                entity.Property(e => e.Title)
                    .HasColumnName("title")
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(e => e.Message)
                    .HasColumnName("message")
                    .IsRequired();

                entity.Property(e => e.Type)
                    .HasColumnName("type")
                    .HasMaxLength(50)
                    .IsRequired()
                    .HasDefaultValue(NotificationType.Info);

                entity.Property(e => e.Category)
                    .HasColumnName("category")
                    .HasMaxLength(100)
                    .IsRequired()
                    .HasDefaultValue(NotificationCategory.System.General);

                entity.Property(e => e.IsRead)
                    .HasColumnName("is_read")
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(e => e.ReadAt)
                    .HasColumnName("read_at");

                entity.Property(e => e.ReferenceId)
                    .HasColumnName("reference_id");

                entity.Property(e => e.ReferenceType)
                    .HasColumnName("reference_type")
                    .HasMaxLength(100);

                entity.Property(e => e.ActionUrl)
                    .HasColumnName("action_url")
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .IsRequired()
                    .HasDefaultValueSql("NOW()");
            });

            // =========================
            // SUBJECT
            // =========================
            modelBuilder.Entity<Subject>(entity =>
            {
                entity.HasIndex(s => s.Name).IsUnique();
                entity.Property(s => s.Name).HasMaxLength(100).IsRequired();
                entity.Property(s => s.Category).HasMaxLength(50);
            });

            // =========================
            // TEACHER SUBJECT
            // =========================
            modelBuilder.Entity<TeacherSubject>(entity =>
            {
                entity.HasIndex(ts => new { ts.TeacherId, ts.SubjectId }).IsUnique();

                entity.HasOne(ts => ts.Teacher)
                      .WithMany(u => u.TeacherSubjects)
                      .HasForeignKey(ts => ts.TeacherId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ts => ts.Subject)
                      .WithMany(s => s.TeacherSubjects)
                      .HasForeignKey(ts => ts.SubjectId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(ts => ts.ExperienceYears).HasColumnType("integer");

                // Nueva configuración para PricePerHour
                entity.Property(ts => ts.PricePerHour)
                      .HasColumnType("decimal(10,2)")
                      .HasDefaultValue(0m)
                      .IsRequired();
            });

            // =========================
            // PASSWORD RESET TOKEN
            // =========================
            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.ToTable("password_reset_tokens");
                entity.HasKey(prt => prt.Id);

                entity.Property(prt => prt.Id)
                      .HasColumnName("id")
                      .HasDefaultValueSql("gen_random_uuid()");
                entity.Property(prt => prt.UserId).HasColumnName("user_id").IsRequired();
                entity.Property(prt => prt.Token).HasColumnName("token").HasMaxLength(6).IsRequired();
                entity.Property(prt => prt.DeliveryMethod).HasColumnName("delivery_method").HasMaxLength(20).IsRequired();
                entity.Property(prt => prt.DeliveryDestination).HasColumnName("delivery_destination").HasMaxLength(255).IsRequired();
                entity.Property(prt => prt.ExpiresAt).HasColumnName("expires_at").IsRequired();
                entity.Property(prt => prt.IsUsed).HasColumnName("is_used").HasDefaultValue(false);
                entity.Property(prt => prt.UsedAt).HasColumnName("used_at");
                entity.Property(prt => prt.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd(); ;
                entity.Property(prt => prt.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
                entity.Property(prt => prt.UserAgent).HasColumnName("user_agent").HasColumnType("text");

                entity.HasOne(prt => prt.User)
                      .WithMany(u => u.PasswordResetTokens)
                      .HasForeignKey(prt => prt.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(prt => prt.UserId).HasDatabaseName("idx_password_reset_tokens_user_id");
                entity.HasIndex(prt => prt.Token).HasDatabaseName("idx_password_reset_tokens_token");
                entity.HasIndex(prt => prt.ExpiresAt).HasDatabaseName("idx_password_reset_tokens_expires_at");
                entity.HasCheckConstraint("check_delivery_method", "delivery_method IN ('email', 'sms')");
            });

            // =========================
            // USER ACADEMIC PROFILE
            // =========================
            modelBuilder.Entity<UserAcademicProfile>(entity =>
            {
                entity.ToTable("user_academic_profiles");
                entity.HasKey(uap => uap.Id);

                entity.Property(uap => uap.EducationLevel).HasMaxLength(50).IsRequired();
                entity.Property(uap => uap.AcademicStatus).HasMaxLength(30).IsRequired();
                entity.Property(uap => uap.StudyArea).HasMaxLength(100);
                entity.Property(uap => uap.CreatedAt)
                  .HasDefaultValueSql("CURRENT_TIMESTAMP")
                  .ValueGeneratedOnAdd();

                entity.Property(uap => uap.RoleId)
                      .HasColumnName("role_id")
                      .IsRequired();

                entity.Property(uap => uap.ProfessionalDescription)
                      .HasColumnName("professional_description")
                      .HasColumnType("text");

                entity.HasOne(uap => uap.Role)
                      .WithMany()
                      .HasForeignKey(uap => uap.RoleId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(uap => uap.User)
                      .WithMany(u => u.AcademicProfiles)
                      .HasForeignKey(uap => uap.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(uap => uap.Institution)
                      .WithMany()
                      .HasForeignKey(uap => uap.InstitutionId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // =========================
            // USER ADDRESS
            // =========================
            modelBuilder.Entity<UserAddress>(entity =>
            {
                entity.ToTable("user_addresses");
                entity.HasKey(ua => ua.Id);

                entity.Property(ua => ua.Id).HasColumnName("id");
                entity.Property(ua => ua.UserId).HasColumnName("user_id").IsRequired();
                entity.Property(ua => ua.CityId).HasColumnName("city_id").IsRequired();
                entity.Property(ua => ua.Address).HasColumnName("address").HasMaxLength(255).IsRequired();
                entity.Property(ua => ua.IsPrimary).HasColumnName("is_primary").HasDefaultValue(false);
                entity.Property(ua => ua.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd(); ;

                entity.HasOne(ua => ua.User)
                      .WithMany(u => u.Addresses)
                      .HasForeignKey(ua => ua.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ua => ua.City)
                      .WithMany()
                      .HasForeignKey(ua => ua.CityId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(ua => ua.UserId).HasDatabaseName("idx_user_addresses_user_id");
                entity.HasIndex(ua => new { ua.UserId, ua.IsPrimary }).HasDatabaseName("idx_user_addresses_user_primary");
            });

            // =========================
            // LOCATION HIERARCHY
            // =========================
            modelBuilder.Entity<Department>()
                .HasOne(d => d.Country)
                .WithMany(c => c.Departments)
                .HasForeignKey(d => d.CountryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<City>()
                .HasOne(c => c.Department)
                .WithMany(d => d.Cities)
                .HasForeignKey(c => c.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Institution>()
                .HasOne(i => i.City)
                .WithMany(c => c.Institutions)
                .HasForeignKey(i => i.CityId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================
            // ROLE
            // =========================
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("roles");
                entity.HasKey(r => r.Id);

                entity.Property(r => r.Id).HasColumnName("id");
                entity.Property(r => r.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
                entity.Property(r => r.Description).HasColumnName("description").HasMaxLength(500);

                entity.HasIndex(r => r.Name).IsUnique().HasDatabaseName("idx_roles_name");
            });

            // =========================
            // USER ROLE (muchos a muchos User-Role)
            // =========================
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("user_roles");

                entity.HasKey(ur => new { ur.UserId, ur.RoleId });

                entity.Property(ur => ur.UserId)
                    .HasColumnName("user_id");

                entity.Property(ur => ur.RoleId)
                    .HasColumnName("role_id");

                entity.HasOne(ur => ur.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .HasPrincipalKey(u => u.Id);

                entity.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .HasPrincipalKey(r => r.Id);
            });


            // =========================
            // PERMISSION
            // =========================
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.ToTable("permissions");
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Id).HasColumnName("id");
                entity.Property(p => p.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
                entity.Property(p => p.Description).HasColumnName("description").HasMaxLength(500);
                entity.Property(p => p.Module).HasColumnName("module").HasMaxLength(100);

                entity.HasIndex(p => p.Code).IsUnique().HasDatabaseName("idx_permissions_code");
            });

            // =========================
            // ROLE PERMISSION (muchos a muchos Role-Permission)
            // =========================
            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.ToTable("rolepermissions");
                entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });

                entity.Property(rp => rp.RoleId).HasColumnName("roleid");
                entity.Property(rp => rp.PermissionId).HasColumnName("permissionid");

                entity.HasOne(rp => rp.Role)
                      .WithMany(r => r.RolePermissions)
                      .HasForeignKey(rp => rp.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rp => rp.Permission)
                      .WithMany(p => p.RolePermissions)
                      .HasForeignKey(rp => rp.PermissionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // =========================
            // SYSTEM PARAMETER
            // =========================
            modelBuilder.Entity<SystemParameter>(entity =>
            {
                entity.ToTable("system_parameters");
                entity.HasKey(sp => sp.Id);

                entity.Property(sp => sp.Id).HasColumnName("id");
                entity.Property(sp => sp.ParamKey).HasColumnName("param_key").HasMaxLength(100).IsRequired();
                entity.Property(sp => sp.ParamValue).HasColumnName("param_value").HasMaxLength(500).IsRequired();
                entity.Property(sp => sp.Description).HasColumnName("description").HasMaxLength(500);
                entity.Property(sp => sp.DataType).HasColumnName("data_type").HasMaxLength(50).HasDefaultValue("string");
                entity.Property(sp => sp.IsActive).HasColumnName("is_active").HasDefaultValue(true);
                entity.Property(sp => sp.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd(); ;
                entity.Property(sp => sp.UpdatedAt).HasColumnName("updated_at");

                entity.HasIndex(sp => sp.ParamKey).IsUnique().HasDatabaseName("idx_system_parameters_key");
                entity.HasIndex(sp => sp.IsActive).HasDatabaseName("idx_system_parameters_active");
                entity.HasIndex(sp => new { sp.ParamKey, sp.DataType }).HasDatabaseName("idx_system_parameters_key_type");
            });

            // =========================
            // MENU
            // =========================
            modelBuilder.Entity<Menu>(entity =>
            {
                entity.ToTable("menus");

                entity.HasKey(m => m.Id);

                entity.Property(m => m.Id)
                    .HasColumnName("id")
                    .HasDefaultValueSql("gen_random_uuid()");

                entity.Property(m => m.Name)
                    .HasColumnName("name")
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(m => m.Icon)
                    .HasColumnName("icon")
                    .HasMaxLength(50);

                entity.Property(m => m.Route)
                    .HasColumnName("route")
                    .HasMaxLength(200);

                entity.Property(m => m.ParentId)
                    .HasColumnName("parent_id");

                entity.Property(m => m.Order)
                    .HasColumnName("order")
                    .IsRequired()
                    .HasDefaultValue(0);

                entity.Property(m => m.IsVisible)
                    .HasColumnName("is_visible")
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.Property(m => m.CreatedAt)
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(m => m.UpdatedAt)
                    .HasColumnName("updated_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Relación jerárquica (auto-referenciada)
                entity.HasOne(m => m.Parent)
                    .WithMany(m => m.Children)
                    .HasForeignKey(m => m.ParentId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Índices
                entity.HasIndex(m => m.ParentId)
                    .HasDatabaseName("idx_menus_parent_id");

                entity.HasIndex(m => m.Order)
                    .HasDatabaseName("idx_menus_order");

                entity.HasIndex(m => m.IsVisible)
                    .HasDatabaseName("idx_menus_is_visible");
            });

            // =========================
            // MENU PERMISSION
            // =========================
            modelBuilder.Entity<MenuPermission>(entity =>
            {
                entity.ToTable("menu_permissions");

                entity.HasKey(mp => new { mp.MenuId, mp.PermissionId });

                entity.Property(mp => mp.MenuId)
                    .HasColumnName("menu_id");

                entity.Property(mp => mp.PermissionId)
                    .HasColumnName("permission_id");

                entity.Property(mp => mp.CreatedAt)
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Relación con Menu
                entity.HasOne(mp => mp.Menu)
                    .WithMany(m => m.MenuPermissions)
                    .HasForeignKey(mp => mp.MenuId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relación con Permission (existente)
                entity.HasOne(mp => mp.Permission)
                    .WithMany(p => p.MenuPermissions)
                    .HasForeignKey(mp => mp.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Índices
                entity.HasIndex(mp => mp.PermissionId)
                    .HasDatabaseName("idx_menu_permissions_permission_id");
            });

            ConfigureChatEntities(modelBuilder);
        }

        private static void ConfigureChatEntities(ModelBuilder modelBuilder)
        {
            // ─────────────────────────────────────────────────
            // CONVERSATION
            // ─────────────────────────────────────────────────
            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.ToTable("conversations");

                entity.HasKey(c => c.Id);

                entity.Property(c => c.Id)
                    .HasColumnName("id")
                    .HasDefaultValueSql("gen_random_uuid()");

                entity.Property(c => c.UserOneId)
                    .HasColumnName("user_one_id")
                    .IsRequired();

                entity.Property(c => c.UserTwoId)
                    .HasColumnName("user_two_id")
                    .IsRequired();

                entity.Property(c => c.CreatedAt)
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("NOW()");

                entity.Property(c => c.UpdatedAt)
                    .HasColumnName("updated_at")
                    .HasDefaultValueSql("NOW()");

                // Relaciones
                entity.HasOne(c => c.UserOne)
                    .WithMany()
                    .HasForeignKey(c => c.UserOneId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.UserTwo)
                    .WithMany()
                    .HasForeignKey(c => c.UserTwoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(c => c.Messages)
                    .WithOne(m => m.Conversation)
                    .HasForeignKey(m => m.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Constraint único: no permitir conversaciones duplicadas
                // (user_one, user_two) o (user_two, user_one) son la misma conversación
                entity.HasIndex(c => new { c.UserOneId, c.UserTwoId })
                    .IsUnique()
                    .HasDatabaseName("idx_conversation_unique");
            });

            // ─────────────────────────────────────────────────
            // MESSAGE
            // ─────────────────────────────────────────────────
            modelBuilder.Entity<Message>(entity =>
            {
                entity.ToTable("messages");

                entity.HasKey(m => m.Id);

                entity.Property(m => m.Id)
                    .HasColumnName("id")
                    .HasDefaultValueSql("gen_random_uuid()");

                entity.Property(m => m.ConversationId)
                    .HasColumnName("conversation_id")
                    .IsRequired();

                entity.Property(m => m.SenderId)
                    .HasColumnName("sender_id")
                    .IsRequired();

                entity.Property(m => m.Content)
                    .HasColumnName("content")
                    .HasColumnType("text");

                entity.Property(m => m.MessageType)
                    .HasConversion(
                        v => v.ToString().ToLower(),
                        v => Enum.Parse<MessageType>(v, true)
                    )
                    .HasColumnName("message_type")
                    .HasMaxLength(20)
                    .IsRequired();


                entity.Property(m => m.FileUrl)
                    .HasColumnName("file_url")
                    .HasMaxLength(500);

                entity.Property(m => m.FileName)
                    .HasColumnName("file_name")
                    .HasMaxLength(255);

                entity.Property(m => m.FileSize)
                    .HasColumnName("file_size");

                entity.Property(m => m.IsRead)
                    .HasColumnName("is_read")
                    .HasDefaultValue(false);

                entity.Property(m => m.ReadAt)
                    .HasColumnName("read_at");

                entity.Property(m => m.CreatedAt)
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("NOW()");

                // Relaciones
                entity.HasOne(m => m.Conversation)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(m => m.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Sender)
                    .WithMany()
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Índices para performance
                entity.HasIndex(m => new { m.ConversationId, m.CreatedAt })
                    .HasDatabaseName("idx_messages_conversation");

                entity.HasIndex(m => m.SenderId)
                    .HasDatabaseName("idx_messages_sender");

                entity.HasIndex(m => new { m.ConversationId, m.IsRead })
                    .HasFilter("is_read = false")
                    .HasDatabaseName("idx_messages_unread");
            });

            //Sección para disponibilidad de un docente
            modelBuilder.Entity<TeacherAvailability>(entity =>
            {
                entity.ToTable("teacher_availabilities");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TeacherId).HasColumnName("teacher_id");
                entity.Property(e => e.DayOfWeek).HasColumnName("day_of_week");
                entity.Property(e => e.StartTime).HasColumnName("start_time");
                entity.Property(e => e.EndTime).HasColumnName("end_time");
                entity.Property(e => e.SlotDurationMinutes).HasColumnName("slot_duration_minutes");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.Notes).HasColumnName("notes");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.HasOne(e => e.Teacher)
                    .WithMany()
                    .HasForeignKey(e => e.TeacherId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            //Marketplace
            modelBuilder.Entity<MarketplaceTask>(e =>
            {
                e.ToTable("legacy_tasks");
                e.HasKey(t => t.Id);

                e.Property(t => t.Status)
                    .HasConversion<string>()
                    .HasColumnName("status");

                e.Property(t => t.WorkType)
                    .HasConversion<string>()
                    .HasColumnName("work_type");

                e.Property(t => t.AcademicLevel)
                    .HasConversion<string>()
                    .HasColumnName("academic_level");

                e.Property(t => t.RequiredFormat)
                    .HasConversion<string>()
                    .HasColumnName("required_format");

                e.HasOne(t => t.Student)
                    .WithMany()
                    .HasForeignKey(t => t.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(t => t.AssignedTeacher)
                    .WithMany()
                    .HasForeignKey(t => t.AssignedTeacherId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(t => t.SelectedOffer)
                    .WithMany()
                    .HasForeignKey(t => t.SelectedOfferId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasMany(t => t.Offers)
                    .WithOne(o => o.Task)
                    .HasForeignKey(o => o.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(t => t.Attachments)
                    .WithOne(a => a.Task)
                    .HasForeignKey(a => a.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(t => t.CorrectionRequests)
                    .WithOne(c => c.Task)
                    .HasForeignKey(c => c.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ── TaskOffer ────────────────────────────────────────────────
            modelBuilder.Entity<TaskOffer>(e =>
            {
                e.ToTable("legacy_offers");
                e.HasKey(o => o.Id);

                e.Property(o => o.Status)
                    .HasConversion<string>()
                    .HasColumnName("status");

                e.HasIndex(o => new { o.TaskId, o.TeacherId })
                    .IsUnique();

                e.HasOne(o => o.Teacher)
                    .WithMany()
                    .HasForeignKey(o => o.TeacherId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ── TaskAttachment ───────────────────────────────────────────
            modelBuilder.Entity<TaskAttachment>(e =>
            {
                e.ToTable("task_attachments");
                e.HasKey(a => a.Id);

                e.HasOne(a => a.Uploader)
                    .WithMany()
                    .HasForeignKey(a => a.UploadedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ── MarketplacePayment ───────────────────────────────────────
            modelBuilder.Entity<MarketplacePayment>(e =>
            {
                e.ToTable("legacy_payments");
                e.HasKey(p => p.Id);

                e.Property(p => p.Status)
                    .HasConversion<string>()
                    .HasColumnName("status");

                e.HasOne(p => p.Student)
                    .WithMany()
                    .HasForeignKey(p => p.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(p => p.Teacher)
                    .WithMany()
                    .HasForeignKey(p => p.TeacherId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ── TaskCorrectionRequest ────────────────────────────────────
            modelBuilder.Entity<TaskCorrectionRequest>(e =>
            {
                e.ToTable("task_correction_requests");
                e.HasKey(c => c.Id);

                e.Property(c => c.Status)
                    .HasConversion<string>()
                    .HasColumnName("status");

                e.HasOne(c => c.Student)
                    .WithMany()
                    .HasForeignKey(c => c.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ── MarketplaceRating ────────────────────────────────────────
            modelBuilder.Entity<MarketplaceRating>(e =>
            {
                e.ToTable("marketplace_ratings");
                e.HasKey(r => r.Id);

                e.HasIndex(r => new { r.TaskId, r.RatedBy, r.RatedUser })
                    .IsUnique();

                e.HasOne(r => r.Rater)
                    .WithMany()
                    .HasForeignKey(r => r.RatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(r => r.RatedUserNavigation)
                    .WithMany()
                    .HasForeignKey(r => r.RatedUser)
                    .OnDelete(DeleteBehavior.Restrict);
            });

        }
    }
}
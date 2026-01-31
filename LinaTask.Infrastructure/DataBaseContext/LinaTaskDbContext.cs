using LinaTask.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace LinaTask.Infrastructure.DataBaseContext
{
    public class LinaTaskDbContext : DbContext
    {
        public LinaTaskDbContext(DbContextOptions<LinaTaskDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<TeacherProfile> TeacherProfiles { get; set; }
        public DbSet<TaskU> Tasks { get; set; }
        public DbSet<Offer> Offers { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<TutoringSession> TutoringSessions { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<TeacherSubject> TeacherSubjects { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.Rating).HasPrecision(2, 1);

                // Configurar el nuevo campo
                entity.Property(u => u.ProfilePhoto)
                      .HasColumnType("text")
                      .IsRequired(false); // Opcional, por si el usuario no tiene foto
            });

            // Configuración de TeacherProfile
            modelBuilder.Entity<TeacherProfile>(entity =>
            {
                entity.HasIndex(tp => tp.UserId).IsUnique();
                entity.HasOne(tp => tp.User)
                      .WithOne(u => u.TeacherProfile)
                      .HasForeignKey<TeacherProfile>(tp => tp.UserId);
            });

            // Configuración de TaskU
            modelBuilder.Entity<TaskU>(entity =>
            {
                entity.HasOne(t => t.Student)
                      .WithMany(u => u.TasksAsStudent)
                      .HasForeignKey(t => t.StudentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(t => t.Title).HasMaxLength(150);
                entity.Property(t => t.Subject).HasMaxLength(100);
                entity.Property(t => t.Status).HasMaxLength(30);
                entity.Property(t => t.Budget).HasColumnType("decimal(10,2)");
            });

            // Configuración de Offer
            modelBuilder.Entity<Offer>(entity =>
            {
                entity.HasOne(o => o.Task)
                      .WithMany(t => t.Offers)
                      .HasForeignKey(o => o.TaskId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.Teacher)
                      .WithMany(u => u.Offers)
                      .HasForeignKey(o => o.TeacherId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(o => o.Price).HasColumnType("decimal(10,2)");
                entity.Property(o => o.Status).HasMaxLength(30);
            });

            // Configuración de Payment
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasOne(p => p.TaskU)  // Cambiado de Task a TaskU
                      .WithMany(t => t.Payments)
                      .HasForeignKey(p => p.TaskId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Student)
                      .WithMany(u => u.PaymentsAsStudent)
                      .HasForeignKey(p => p.StudentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(p => p.Amount).HasColumnType("decimal(10,2)");
                entity.Property(p => p.PlatformFee).HasColumnType("decimal(10,2)");
                entity.Property(p => p.TeacherAmount).HasColumnType("decimal(10,2)");
                entity.Property(p => p.Status).HasMaxLength(30);
            });

            // Configuración de TutoringSession
            modelBuilder.Entity<TutoringSession>(entity =>
            {
                entity.HasOne(ts => ts.Student)
                      .WithMany(u => u.TutoringSessionsAsStudent)
                      .HasForeignKey(ts => ts.StudentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ts => ts.Teacher)
                      .WithMany(u => u.TutoringSessionsAsTeacher)
                      .HasForeignKey(ts => ts.TeacherId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración de Subject
            modelBuilder.Entity<Subject>(entity =>
            {
                entity.HasIndex(s => s.Name).IsUnique();
                entity.Property(s => s.Name).HasMaxLength(100).IsRequired();
                entity.Property(s => s.Category).HasMaxLength(50);
            });

            // Configuración de TeacherSubject
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

                entity.Property(ts => ts.ExperienceYears)
                      .HasColumnType("integer");
            });

            // Configuración de PasswordResetToken
            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.ToTable("password_reset_tokens");

                entity.HasKey(prt => prt.Id);

                entity.Property(prt => prt.Id)
                      .HasColumnName("id")
                      .HasDefaultValueSql("gen_random_uuid()");

                entity.Property(prt => prt.UserId)
                      .HasColumnName("user_id")
                      .IsRequired();

                entity.Property(prt => prt.Token)
                      .HasColumnName("token")
                      .HasMaxLength(6)
                      .IsRequired();

                entity.Property(prt => prt.DeliveryMethod)
                      .HasColumnName("delivery_method")
                      .HasMaxLength(20)
                      .IsRequired();

                entity.Property(prt => prt.DeliveryDestination)
                      .HasColumnName("delivery_destination")
                      .HasMaxLength(255)
                      .IsRequired();

                entity.Property(prt => prt.ExpiresAt)
                      .HasColumnName("expires_at")
                      .IsRequired();

                entity.Property(prt => prt.IsUsed)
                      .HasColumnName("is_used")
                      .HasDefaultValue(false);

                entity.Property(prt => prt.UsedAt)
                      .HasColumnName("used_at");

                entity.Property(prt => prt.CreatedAt)
                      .HasColumnName("created_at")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(prt => prt.IpAddress)
                      .HasColumnName("ip_address")
                      .HasMaxLength(45);

                entity.Property(prt => prt.UserAgent)
                      .HasColumnName("user_agent")
                      .HasColumnType("text");

                // Relación con User
                entity.HasOne(prt => prt.User)
                      .WithMany(u => u.PasswordResetTokens)
                      .HasForeignKey(prt => prt.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Índices
                entity.HasIndex(prt => prt.UserId)
                      .HasDatabaseName("idx_password_reset_tokens_user_id");

                entity.HasIndex(prt => prt.Token)
                      .HasDatabaseName("idx_password_reset_tokens_token");

                entity.HasIndex(prt => prt.ExpiresAt)
                      .HasDatabaseName("idx_password_reset_tokens_expires_at");

                // CHECK constraint
                entity.HasCheckConstraint(
                    "check_delivery_method",
                    "delivery_method IN ('email', 'sms')"
                );
            });

        }
    }
}

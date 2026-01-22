using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

namespace reviews.Models;

public partial class ApplicationDbContext : IdentityDbContext<ApplicationUser, 
                                                              IdentityRole, 
                                                              string>, 
                                          IDataProtectionKeyContext 
                                                              /*,
                                                              -- these items not needed as we're only using user and role 
                                                              IdentityUserClaim<string>,
                                                              IdentityUserRole<string>, 
                                                              IdentityUserLogin<string>,
                                                              IdentityRoleClaim<string>, 
                                                              IdentityUserToken<string>> 
                                                              */
{
    private readonly IConfiguration? _configuration;

    public ApplicationDbContext() : base(new DbContextOptions<ApplicationDbContext>())
    {
        // This constructor is only for design-time support.
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration;
    }

    public DbSet<Client> Clients { get; set; }
    
    public DbSet<EmailQueue> EmailQueue { get; set; }

    public DbSet<EmailTemplate> EmailTemplates { get; set; }

    public DbSet<Evaluation> Evaluations { get; set; }

    public DbSet<EvaluationParticipant> EvaluationParticipants { get; set; }

    public DbSet<EvaluationParticipantStatus> EvaluationParticipantStatuses { get; set; }

    public DbSet<EvaluationRatee> EvaluationRatees { get; set; }

    public DbSet<EvaluationRateeStatus> EvaluationRateeStatuses { get; set; }

    public DbSet<Participant> Participants { get; set; }

    public DbSet<ParticipantRole> ParticipantRoles { get; set; }

    public DbSet<vwAdminParticipants> ViewAdminParticipants { get; set; }

    public DbSet<vwAdminRatees> ViewAdminRatees { get; set; }

    public DbSet<vwDefaultEvaluationByParticipant> ViewDefaultEvaluationByParticipant { get; set; }

    public DbSet<vwEvaluationsByRatee> ViewEvaluationsByRatees { get; set; }

    public DbSet<vwEvaluationsToComplete> ViewEvaluationsToComplete { get; set; }

    public DbSet<vwParticipant> ViewParticipant { get; set; }

    public DbSet<vwRateeInvitations> ViewRateeInvitations { get; set; }

    public DbSet<vwRaterInvitations> ViewRaterInvitations { get; set; }

    public DbSet<vwRatersByRatee> ViewRatersByRatees { get; set; }

    public DbSet<vwReminders> ViewReminders { get; set; }

    // Data Protection keys table
    public DbSet<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (_configuration != null)
        {
            optionsBuilder.UseNpgsql(_configuration.GetConnectionString("ApplicationDBConnection"));
        }
        //optionsBuilder.UseLazyLoadingProxies();
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        // Configure IdentityPasskeyData (new in .NET 10)
        modelBuilder.Entity<IdentityPasskeyData>(entity =>
        {
            entity.Property<byte[]>("Id")
                .HasColumnType("bytea");

            entity.HasKey("Id");

            entity.Property<string>("UserId")
                .IsRequired()
                .HasColumnType("text");

            entity.Property<string>("Name")
                .IsRequired()
                .HasColumnType("text");

            entity.Property<byte[]>("PublicKey")
                .IsRequired()
                .HasColumnType("bytea");

            entity.Property<byte[]>("CredentialId")
                .IsRequired()
                .HasColumnType("bytea");

            entity.ToTable("AspNetPasskeys");
        });

        // Configure IdentityUserPasskey (new in .NET 10)
        modelBuilder.Entity<IdentityUserPasskey<string>>(entity =>
        {
            entity.Property<byte[]>("Id")
                .HasColumnType("bytea");

            entity.HasKey("Id");

            entity.Property<string>("UserId")
                .IsRequired()
                .HasColumnType("text");

            entity.Property<string>("Name")
                .IsRequired()
                .HasColumnType("text");

            entity.Property<byte[]>("PublicKey")
                .IsRequired()
                .HasColumnType("bytea");

            entity.Property<byte[]>("CredentialId")
                .IsRequired()
                .HasColumnType("bytea");

            entity.ToTable("AspNetUserPasskeys");
        });

        modelBuilder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.Property<string>("LoginProvider")
                .HasColumnType("text");

            entity.Property<string>("ProviderKey")
                .HasColumnType("text");

            entity.HasKey("LoginProvider", "ProviderKey");

            entity.ToTable("AspNetUserLogins");
        });

        modelBuilder.Entity<IdentityUserRole<string>>(entity =>
        {
            entity.Property<string>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("text")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property<string>("UserId")
                .HasColumnType("text");

            entity.Property<string>("RoleId")
                .HasColumnType("text");

            entity.Property<string>("LoginProvider")
                .HasColumnType("text");

            entity.Property<string>("Name")
                .HasColumnType("text");

            entity.HasKey("UserId", "RoleId");

            entity.ToTable("AspNetUserRole");
        });

        modelBuilder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.Property<string>("UserId")
                .HasColumnType("text");

            entity.Property<string>("LoginProvider")
                .HasColumnType("text");

            entity.Property<string>("Name")
                .HasColumnType("text");

            entity.HasKey("UserId", "LoginProvider", "Name");

            entity.ToTable("AspNetUserTokens");
        });

        modelBuilder.Entity<IdentityRoleClaim<string>>(entity =>
        {
            entity.Property<int>("Id")
                .HasColumnType("integer");

            entity.HasKey("Id");

            entity.ToTable("AspNetRoleClaims");
        });

        modelBuilder.Entity<IdentityUserClaim<string>>(entity =>
        {
            entity.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            entity.Property<string>("ClaimType")
                .HasColumnType("TEXT");

            entity.Property<string>("ClaimValue")
                .HasColumnType("TEXT");

            entity.Property<string>("UserId")
                .IsRequired()
                .HasColumnType("TEXT");

            entity.HasKey("Id");

            entity.HasIndex("UserId");

            entity.ToTable("AspNetUserClaims");
        });

        modelBuilder.Entity<IdentityRole<string>>(entity =>
        {
            entity.Property<string>("Id")
                .HasColumnType("text");

            entity.HasKey("Id");

            entity.ToTable("AspNetRoles");
        });

        modelBuilder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.Property<string>("UserId")
                .HasColumnType("text");

            entity.Property<string>("LoginProvider")
                .HasColumnType("text");

            entity.Property<string>("Name")
                .HasColumnType("text");

            entity.Property<string>("Value")
                .HasColumnType("text");

            entity.HasKey("UserId", "LoginProvider", "Name");

            entity.ToTable("AspNetUserTokens");
        });

        modelBuilder.Entity<ApplicationUser>( entity =>
        {
            entity.Property<string>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("text")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property<int>("AccessFailedCount")
                .HasColumnType("integer");

            entity.Property<string>("ConcurrencyStamp")
                .HasColumnType("text");

            entity.Property<string>("Email")
                .HasColumnType("text");

            entity.Property<bool>("EmailConfirmed")
                .HasColumnType("boolean");

            entity.Property<bool>("LockoutEnabled")
                .HasColumnType("boolean");

            entity.Property<DateTimeOffset?>("LockoutEnd")
                .HasColumnType("timestamp with time zone");

            entity.Property<string>("NormalizedEmail")
                .HasColumnType("text");

            entity.Property<string>("NormalizedUserName")
                .HasColumnType("text");

            entity.Property<string>("PasswordHash")
                .HasColumnType("text");

            entity.Property<string>("PhoneNumber")
                .HasColumnType("text");

            entity.Property<bool>("PhoneNumberConfirmed")
                .HasColumnType("boolean");

            entity.Property<string>("SecurityStamp")
                .HasColumnType("text");

            entity.Property<bool>("TwoFactorEnabled")
                .HasColumnType("boolean");

            entity.Property<string>("UserName")
                .HasColumnType("text");
            
            entity.Property<Guid>("ParticipantId")
                .HasColumnType("uuid");

            entity.HasMany<IdentityUserRole<string>>(u => u.UserRoles)
                .WithOne()
                .HasForeignKey(ur => ur.UserId)
                .IsRequired();

            entity.HasKey("Id");

            entity.ToTable("AspNetUsers");
        });

        modelBuilder.Entity<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey>(b =>
            {
                b.Property<int>("Id")
                    .HasColumnType("integer")
                    .IsRequired();

                b.Property<string>("FriendlyName")
                    .HasColumnType("text");

                b.Property<string>("Xml")
                    .HasColumnType("text");

                b.HasKey("Id");

                b.ToTable("DataProtectionKeys", "public");
            });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("Client");

            entity.Property(e => e.ClientId).HasDefaultValueSql("gen_random_uuid()");

            entity.HasMany(c => c.EmailTemplates)
                  .WithOne(e => e.Client)
                  .HasForeignKey(e => e.ClientId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmailTemplate>(entity =>
        {
            entity.ToTable("EmailTemplate");

            entity.Property(e => e.EmailTemplateId).HasDefaultValueSql("gen_random_uuid()");

            entity.HasOne(e => e.Client)
                  .WithMany(c => c.EmailTemplates)
                  .HasForeignKey(e => e.ClientId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .IsRequired();
        });

        modelBuilder.Entity<EmailQueue>(entity =>
        {
            entity.ToTable("EmailQueue");

            entity.Property(e => e.EmailQueueId)
                .UseIdentityByDefaultColumn()
                .HasIdentityOptions(startValue: 1000);

            entity.HasKey(e => new { e.EmailQueueId });    

            entity.Property(e => e.EmailQueuedDateTime).HasColumnType("timestamp with time zone");
            entity.Property(e => e.EmailSentDateTime).HasColumnType("timestamp with time zone");
            entity.Property(e => e.EmailTest).HasDefaultValue(true);
            entity.Property(e => e.EmailSent).HasDefaultValue(false);

        });

        modelBuilder.Entity<Evaluation>(entity =>
        {
            entity.ToTable("Evaluation");

            entity.Property(e => e.EvaluationId).HasColumnType("uuid").ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.EvaluationActive).HasDefaultValue(false);
            entity.Property(e => e.EvaluationOpenDate).HasColumnType("timestamp with time zone");
            entity.Property(e => e.EvaluationRateeDueDate).HasColumnType("timestamp with time zone");
            entity.Property(e => e.EvaluationRaterDueDate).HasColumnType("timestamp with time zone");

            entity.HasOne(d => d.Client).WithMany(p => p.Evaluations)
                .HasForeignKey(d => d.ClientId)
                .HasConstraintName("FK_Client_Evaluation_ClientId");
        });

        modelBuilder.Entity<EvaluationParticipant>(entity =>
        {
            entity.HasKey(e => new { e.EvaluationRateeId, e.ParticipantId });

            entity.ToTable("EvaluationParticipant");

            entity.Property(e => e.EvaluationParticipantDateLastEmailed).HasColumnType("timestamp with time zone");

            entity.Property(e => e.EvaluationParticipantDateCompleted).HasColumnType("timestamp with time zone");

            entity.HasOne(e => e.EvaluationRatee)
                .WithMany(r => r.EvaluationParticipants)
                .HasForeignKey(e => e.EvaluationRateeId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("FK_EvaluationParticipant_EvaluationRatee");

            entity.HasOne(d => d.Participants)
                .WithMany(p => p.EvaluationParticipants)
                .HasForeignKey(d => d.ParticipantId)
                .HasConstraintName("FK_EvaluationParticipant_Participant_ParticipantId");

            entity.HasOne(d => d.EvaluationParticipantStatus)
                .WithMany(p => p.EvaluationParticipants)
                .HasForeignKey(d => d.EvaluationParticipantStatusId)
                .HasConstraintName("FK_EvaluationParticipant_EvaluationParticipantStatus");

            entity.HasOne(d => d.ParticipantRoles)
                .WithMany(p => p.EvaluationParticipants)
                .HasForeignKey(d => d.ParticipantRoleId)
                .HasConstraintName("FK_EvaluationParticipant_ParticipantRole_ParticipantRoleId");
        });

        modelBuilder.Entity<EvaluationParticipantStatus>(entity =>
        {
            entity.ToTable("EvaluationParticipantStatus");

            entity.Property(e => e.EvaluationParticipantStatusId).ValueGeneratedNever();
        });

        modelBuilder.Entity<EvaluationRatee>(entity =>
        {
            entity.ToTable("EvaluationRatee");

            entity.HasIndex(e => new { e.EvaluationId, e.ParticipantId }, "UK_EvaluationRatee_EvaluationId_ParticipantId").IsUnique();

            entity.Property(e => e.EvaluationRateeId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.EvaluationRateeDateAdded).HasColumnType("timestamp with time zone");
            entity.Property(e => e.EvaluationRateeDueDateOverride).HasColumnType("timestamp with time zone");
            entity.Property(e => e.EvaluationRaterDueDateOverride).HasColumnType("timestamp with time zone");

            // EvaluationRateeStatus
            entity.HasOne(d => d.EvaluationRateeStatus).WithMany(p => p.EvaluationRatees)
                .HasForeignKey(d => d.EvaluationRateeStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EvaluationRatee_EvaluationRateeStatus");

            // Evaluation
            entity.HasOne(d => d.Evaluations)
                .WithMany(p => p.EvaluationRatee)
                .HasForeignKey(d => d.EvaluationId)
                .HasConstraintName("FK_EvaluationRatee_Evaluation_EvaluationId");

            // Participant
            entity.HasOne(d => d.Participants)
                .WithMany(p => p.EvaluationRatees)
                .HasForeignKey(d => d.ParticipantId);
        });

        modelBuilder.Entity<EvaluationRateeStatus>(entity =>
        {
            entity.ToTable("EvaluationRateeStatus");

            entity.Property(e => e.EvaluationRateeStatusId).ValueGeneratedNever();
        });

        modelBuilder.Entity<Participant>(entity =>
        {
            entity.ToTable("Participant");

            entity.HasIndex(e => e.ParticipantEmail, "UK_Participant_ParticipantEmail").IsUnique();

            entity.Property(e => e.ParticipantId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.ParticipantHasRegistered).HasDefaultValue(false);
        });

        modelBuilder.Entity<ParticipantRole>(entity =>
        {
            entity.ToTable("ParticipantRole");

            entity.Property(e => e.ParticipantRoleId).ValueGeneratedNever();
        });

        modelBuilder.Entity<vwAdminParticipants>().HasNoKey().ToView("vwAdminParticipants");
        modelBuilder.Entity<vwAdminRatees>().HasNoKey().ToView("vwAdminRatees");
        modelBuilder.Entity<vwDefaultEvaluationByParticipant>().HasNoKey().ToView("vwDefaultEvaluationByParticipant");
        modelBuilder.Entity<vwEvaluationsByRatee>().HasNoKey().ToView("vwEvaluationsByRatee");
        modelBuilder.Entity<vwEvaluationsToComplete>().HasNoKey().ToView("vwEvaluationsToComplete");
        modelBuilder.Entity<vwParticipant>().HasNoKey().ToView("vwParticipant");
        modelBuilder.Entity<vwRaterInvitations>().HasNoKey().ToView("vwRaterInvitations");
        modelBuilder.Entity<vwRateeInvitations>().HasNoKey().ToView("vwRateeInvitations");
        modelBuilder.Entity<vwRatersByRatee>().HasNoKey().ToView("vwRatersByRatee");
        modelBuilder.Entity<vwReminders>().HasNoKey().ToView("vwReminders");

        // DateTime conversion to ensure DateTimeKind is handled automatically

        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : v, // Store as UTC
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc).ToLocalTime() // Read as Local
        );

        // Evaluation
        modelBuilder.Entity<Evaluation>()
            .Property(e => e.EvaluationOpenDate)
            .HasConversion(dateTimeConverter);

        modelBuilder.Entity<Evaluation>()
            .Property(e => e.EvaluationRateeDueDate)
            .HasConversion(dateTimeConverter);

        modelBuilder.Entity<Evaluation>()
            .Property(e => e.EvaluationRaterDueDate)
            .HasConversion(dateTimeConverter);

        // EvaluationParticipant
        modelBuilder.Entity<EvaluationParticipant>()
            .Property(e => e.EvaluationParticipantDateLastEmailed)
            .HasConversion(dateTimeConverter);

        modelBuilder.Entity<EvaluationParticipant>()
            .Property(e => e.EvaluationParticipantDateCompleted)
            .HasConversion(dateTimeConverter);

        // EvaluationRatee
        modelBuilder.Entity<EvaluationRatee>()
            .Property(e => e.EvaluationRateeDateAdded)
            .HasConversion(dateTimeConverter);

        modelBuilder.Entity<EvaluationRatee>()
            .Property(e => e.EvaluationRateeDueDateOverride)
            .HasConversion(dateTimeConverter);

        modelBuilder.Entity<EvaluationRatee>()
            .Property(e => e.EvaluationRaterDueDateOverride)
            .HasConversion(dateTimeConverter);

        // EmailQueue
        modelBuilder.Entity<EmailQueue>()
            .Property(e => e.EmailQueuedDateTime)
            .HasConversion(dateTimeConverter);

        modelBuilder.Entity<EmailQueue>()
            .Property(e => e.EmailSentDateTime)
            .HasConversion(dateTimeConverter);

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

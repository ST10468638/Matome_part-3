
using Microsoft.EntityFrameworkCore;
using RaceDay_.Api.Models;

namespace RaceDay_.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<EventRoute> EventRoutes => Set<EventRoute>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // USERS
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", table =>
            {
                table.HasCheckConstraint(
                    "CK_Users_Role",
                    "[Role] IN ('Organiser', 'Participant')");
            });

            entity.HasKey(u => u.UserId)
                .HasName("PK_Users");

            entity.Property(u => u.UserId)
                .UseIdentityColumn();

            entity.Property(u => u.FirstName)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(u => u.LastName)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(u => u.Email)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(u => u.PasswordHash)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(u => u.PhoneNumber)
                .HasMaxLength(20);

            entity.Property(u => u.Role)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(u => u.ProfilePictureBlobName)
                .HasMaxLength(512);

            entity.Property(u => u.CreatedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("UQ_Users_Email");
        });


        // EVENTS
        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events", table =>
            {
                table.HasCheckConstraint(
                    "CK_Events_EventType",
                    "[EventType] IN ('Running', 'Walking', 'Cycling')");

                table.HasCheckConstraint(
                    "CK_Events_MaxParticipants",
                    "[MaxParticipants] IS NULL OR [MaxParticipants] > 0");

                table.HasCheckConstraint(
                    "CK_Events_RegistrationDates",
                    "[RegistrationOpenDate] < [RegistrationCloseDate] " +
                    "AND [RegistrationCloseDate] <= [EventDate]");

                table.HasCheckConstraint(
                    "CK_Events_DistanceKm",
                    "[DistanceKm] > 0");
            });

            entity.HasKey(e => e.EventId)
                .HasName("PK_Events");

            entity.Property(e => e.EventId)
                .UseIdentityColumn();

            entity.Property(e => e.EventName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.EventType)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.DistanceKm)
                .HasColumnType("decimal(6,2)")
                .IsRequired();

            entity.Property(e => e.EventDate)
                .HasColumnType("datetime2");

            entity.Property(e => e.Venue)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(e => e.City)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Province)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.RegistrationOpenDate)
                .HasColumnType("datetime2");

            entity.Property(e => e.RegistrationCloseDate)
                .HasColumnType("datetime2");

            entity.Property(e => e.BannerBlobName)
                .HasMaxLength(512);

            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasIndex(e => e.OrganiserId)
                .HasDatabaseName("IX_Events_OrganiserId");

            entity.HasOne(e => e.Organiser)
                .WithMany(u => u.OrganisedEvents)
                .HasForeignKey(e => e.OrganiserId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Events_Users");
        });


        // CATEGORIES
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories", table =>
            {
                table.HasCheckConstraint(
                    "CK_Categories_CategoryType",
                    "[CategoryType] IN ('Age', 'Distance')");

                table.HasCheckConstraint(
                    "CK_Categories_MinimumAge",
                    "[MinimumAge] IS NULL OR [MinimumAge] >= 0");

                table.HasCheckConstraint(
                    "CK_Categories_MaximumAge",
                    "[MaximumAge] IS NULL OR [MaximumAge] >= 0");

                table.HasCheckConstraint(
                    "CK_Categories_AgeRange",
                    "[MinimumAge] IS NULL OR [MaximumAge] IS NULL " +
                    "OR [MinimumAge] <= [MaximumAge]");

                table.HasCheckConstraint(
                    "CK_Categories_EntryFee",
                    "[EntryFee] >= 0");

                table.HasCheckConstraint(
                    "CK_Categories_Capacity",
                    "[Capacity] IS NULL OR [Capacity] > 0");

                table.HasCheckConstraint(
                    "CK_Categories_TypeDistance",
                    "([CategoryType] = 'Age' AND [DistanceKm] IS NULL) " +
                    "OR ([CategoryType] = 'Distance' " +
                    "AND [DistanceKm] IS NOT NULL " +
                    "AND [DistanceKm] > 0 " +
                    "AND [MinimumAge] IS NULL " +
                    "AND [MaximumAge] IS NULL)");
            });

            entity.HasKey(c => c.CategoryId)
                .HasName("PK_Categories");

            entity.Property(c => c.CategoryId)
                .UseIdentityColumn();

            entity.Property(c => c.CategoryName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(c => c.Description)
                .HasMaxLength(500);

            entity.Property(c => c.CategoryType)
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue("Age");

            entity.Property(c => c.DistanceKm)
                .HasColumnType("decimal(6,2)");

            entity.Property(c => c.EntryFee)
                .HasColumnType("decimal(10,2)")
                .HasDefaultValue(0m);

            entity.HasIndex(c => new
            {
                c.EventId,
                c.CategoryName
            })
            .IsUnique()
            .HasDatabaseName("UQ_Categories_Event_CategoryName");

            entity.HasIndex(c => c.EventId)
                .HasDatabaseName("IX_Categories_EventId");

            entity.HasOne(c => c.Event)
                .WithMany(e => e.Categories)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Categories_Events");
        });

        // EVENT ROUTES
        modelBuilder.Entity<EventRoute>(entity =>
        {
            entity.ToTable("EventRoutes", table =>
            {
                table.HasCheckConstraint(
                    "CK_EventRoutes_Distance",
                    "[DistanceKm] > 0");
            });

            entity.HasKey(r => r.EventRouteId)
                .HasName("PK_EventRoutes");

            entity.Property(r => r.EventRouteId)
                .UseIdentityColumn();

            entity.Property(r => r.RouteName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(r => r.DistanceKm)
                .HasColumnType("decimal(6,2)");

            entity.Property(r => r.StartLocation)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(r => r.FinishLocation)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(r => r.RouteUrl)
                .HasMaxLength(500);

            entity.Property(r => r.CreatedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            // One event can have zero or one stored route.
            entity.HasAlternateKey(r => r.EventId)
                .HasName("UQ_EventRoutes_EventId");

            entity.HasOne(r => r.Event)
                .WithOne(e => e.EventRoute)
                .HasForeignKey<EventRoute>(r => r.EventId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_EventRoutes_Events");
        });

        // ENROLMENTS
        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.ToTable("Enrolments", table =>
            {
                table.HasCheckConstraint(
                    "CK_Enrolments_Status",
                    "[Status] IN ('Pending', 'Confirmed', 'Cancelled')");
            });

            entity.HasKey(e => e.EnrolmentId)
                .HasName("PK_Enrolments");

            entity.Property(e => e.EnrolmentId)
                .UseIdentityColumn();

            entity.Property(e => e.EnrolmentDate)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue("Confirmed");

            entity.Property(e => e.BibNumber)
                .HasMaxLength(20);

            // A participant cannot enrol in the same category twice.
            entity.HasAlternateKey(e => new
            {
                e.ParticipantId,
                e.CategoryId
            }).HasName("UQ_Enrolments_Participant_Category");

            entity.HasIndex(e => e.ParticipantId)
                .HasDatabaseName("IX_Enrolments_ParticipantId");

            entity.HasIndex(e => e.CategoryId)
                .HasDatabaseName("IX_Enrolments_CategoryId");

            // Preserve uniqueness for the optional bib number.
            entity.HasIndex(e => e.BibNumber)
                .IsUnique()
                .HasDatabaseName("UQ_Enrolments_BibNumber")
                .HasFilter(null);

            entity.HasOne(e => e.Participant)
                .WithMany(u => u.Enrolments)
                .HasForeignKey(e => e.ParticipantId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Enrolments_Users");

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Enrolments_Categories");
        });

        // RESULTS
        modelBuilder.Entity<Result>(entity =>
        {
            entity.ToTable("Results", table =>
            {
                table.HasCheckConstraint(
                    "CK_Results_OverallPosition",
                    "[OverallPosition] IS NULL OR [OverallPosition] > 0");

                table.HasCheckConstraint(
                    "CK_Results_CategoryPosition",
                    "[CategoryPosition] IS NULL OR [CategoryPosition] > 0");

                table.HasCheckConstraint(
                    "CK_Results_Status",
                    "[ResultStatus] IN " +
                    "('Finished', 'DNF', 'DNS', 'Disqualified')");

                table.HasCheckConstraint(
                    "CK_Results_FinishTime",
                    "[ResultStatus] <> 'Finished' OR [FinishTime] IS NOT NULL");
            });

            entity.HasKey(r => r.ResultId)
                .HasName("PK_Results");

            entity.Property(r => r.ResultId)
                .UseIdentityColumn();

            entity.Property(r => r.FinishTime)
                .HasColumnType("time(0)");

            entity.Property(r => r.ResultStatus)
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue("Finished");

            entity.Property(r => r.RecordedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            // An enrolment can have zero or one result.
            entity.HasAlternateKey(r => r.EnrolmentId)
                .HasName("UQ_Results_EnrolmentId");

            entity.HasOne(r => r.Enrolment)
                .WithOne(e => e.Result)
                .HasForeignKey<Result>(r => r.EnrolmentId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Results_Enrolments");
        });
    }
}

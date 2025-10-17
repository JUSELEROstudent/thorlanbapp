using Microsoft.EntityFrameworkCore;
using GotsThorlabs.Database.EntityRepo.Entities;

namespace GotsThorlabs.Database.EntityRepo;

public class ThorlabsDbContext : DbContext
{
    public ThorlabsDbContext(DbContextOptions<ThorlabsDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<Image> Images => Set<Image>();
    public DbSet<Camera> Cameras => Set<Camera>();
    public DbSet<Microscope> Microscopes => Set<Microscope>();
    public DbSet<Increase> Increases => Set<Increase>();
    public DbSet<GroupCalibration> GroupCalibrations => Set<GroupCalibration>();
    public DbSet<PicsCalibration> PicsCalibrations => Set<PicsCalibration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("user");
            b.HasKey(x => x.IdUser);
            b.Property(x => x.IdUser).HasColumnName("idUser");
            b.Property(x => x.Nickname).HasColumnName("nickname").IsRequired();
            b.Property(x => x.EMail).HasColumnName("eMail").IsRequired();
            b.Property(x => x.Password).HasColumnName("password").IsRequired();
        });

        modelBuilder.Entity<Tour>(b =>
        {
            b.ToTable("tour");
            b.HasKey(x => x.IdTour);
            b.Property(x => x.IdTour).HasColumnName("idTour");
            b.Property(x => x.Date).HasColumnName("date").IsRequired();
            b.Property(x => x.NameFolder).HasColumnName("nameFolder").IsRequired();
            b.Property(x => x.NumberX).HasColumnName("NumberX").IsRequired();
            b.Property(x => x.NumberY).HasColumnName("NumberY").IsRequired();
            b.Property(x => x.NumberZ).HasColumnName("NumberZ").IsRequired();
            b.Property(x => x.Camera).HasColumnName("Camera").IsRequired();
            b.Property(x => x.EndStatus).HasColumnName("endStatus");
            b.HasMany(x => x.Images)
             .WithOne(x => x.Tour)
             .HasForeignKey(x => x.IdTour)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Image>(b =>
        {
            b.ToTable("image");
            b.HasKey(x => x.IdImage);
            b.Property(x => x.IdImage).HasColumnName("idImage");
            b.Property(x => x.Name).HasColumnName("name").IsRequired();
            b.Property(x => x.GausianVal).HasColumnName("gausianVal");
            b.Property(x => x.Path).HasColumnName("path").IsRequired();
            b.Property(x => x.X).HasColumnName("X").IsRequired();
            b.Property(x => x.Y).HasColumnName("Y").IsRequired();
            b.Property(x => x.Z).HasColumnName("Z").IsRequired();
            b.Property(x => x.IdTour).HasColumnName("idTour").IsRequired();
        });

        // Camera
        modelBuilder.Entity<Camera>(b =>
        {
            b.ToTable("camera");
            b.HasKey(x => x.CameraId);
            b.Property(x => x.CameraId).HasColumnName("cameraId");
            b.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(50);
            b.Property(x => x.LocalIdentifier).HasColumnName("localIdentifier").IsRequired().HasMaxLength(250);
            b.Property(x => x.Features).HasColumnName("features").IsRequired().HasMaxLength(250);
        });

        // Microscope
        modelBuilder.Entity<Microscope>(b =>
        {
            b.ToTable("microscope");
            b.HasKey(x => x.MicroscopeId);
            b.Property(x => x.MicroscopeId).HasColumnName("microscopeId");
            b.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(250);
            b.Property(x => x.Brand).HasColumnName("brand").IsRequired().HasMaxLength(250);
            b.Property(x => x.Site).HasColumnName("site").IsRequired().HasMaxLength(250);
            b.Property(x => x.AditionalInfo).HasColumnName("aditionalInfo").IsRequired().HasMaxLength(250);
        });

        // Increase
        modelBuilder.Entity<Increase>(b =>
        {
            b.ToTable("increase");
            b.HasKey(x => x.IncreaseId);
            b.Property(x => x.IncreaseId).HasColumnName("increaseId");
            b.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(250);
            b.Property(x => x.Value).HasColumnName("value").HasPrecision(18, 6);
            b.Property(x => x.AditionalInfo).HasColumnName("aditionalInfo").IsRequired().HasMaxLength(250);
        });

        // GroupCalibration
        modelBuilder.Entity<GroupCalibration>(b =>
        {
            b.ToTable("groupCalibration");
            b.HasKey(x => x.GroupCailbrationId);
            b.Property(x => x.GroupCailbrationId).HasColumnName("groupCailbrationId");
            b.Property(x => x.CameraId).HasColumnName("cameraId").IsRequired();
            b.Property(x => x.MicroscopeId).HasColumnName("microscopeId").IsRequired();
            b.Property(x => x.IncreaseId).HasColumnName("increaseId").IsRequired();
            b.Property(x => x.Date).HasColumnName("date").IsRequired();
            b.Property(x => x.AditionalInfo).HasColumnName("aditionalInfo").IsRequired().HasMaxLength(250);

            b.HasOne(x => x.Camera)
             .WithMany(x => x.GroupCalibrations)
             .HasForeignKey(x => x.CameraId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Microscope)
             .WithMany(x => x.GroupCalibrations)
             .HasForeignKey(x => x.MicroscopeId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Increase)
             .WithMany(x => x.GroupCalibrations)
             .HasForeignKey(x => x.IncreaseId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // PicsCalibration
        modelBuilder.Entity<PicsCalibration>(b =>
        {
            b.ToTable("picsCalibration");
            b.HasKey(x => x.PicsCalibrationId);
            b.Property(x => x.PicsCalibrationId).HasColumnName("picsCalibrationId");
            b.Property(x => x.GroupCailbrationId).HasColumnName("groupCailbrationId").IsRequired();
            b.Property(x => x.Pic1).HasColumnName("pic1").IsRequired().HasMaxLength(1200);
            b.Property(x => x.Pic2).HasColumnName("pic2").IsRequired().HasMaxLength(1200);
            b.Property(x => x.AxeDirectionCalibration).HasColumnName("axeDirectionCalibration").IsRequired().HasMaxLength(2);
            b.Property(x => x.Acepted).HasColumnName("acepted").IsRequired();
            b.Property(x => x.dx).HasColumnName("dx").HasPrecision(18, 6);
            b.Property(x => x.dy).HasColumnName("dy").HasPrecision(18, 6);
            b.Property(x => x.Confidence).HasColumnName("confidence").HasPrecision(18, 6);
            b.Property(x => x.MeasureUnit).HasColumnName("measureUnit").IsRequired().HasMaxLength(250);
            b.Property(x => x.MovementValue).HasColumnName("movementValue").HasPrecision(18, 6);

            b.HasOne(x => x.GroupCalibration)
             .WithMany(x => x.PicsCalibrations)
             .HasForeignKey(x => x.GroupCailbrationId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
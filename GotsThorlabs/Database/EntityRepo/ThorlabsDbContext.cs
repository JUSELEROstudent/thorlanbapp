using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using GotsThorlabs.Database.EntityRepo.Entities;

namespace GotsThorlabs.Database.EntityRepo
{
    public partial class ThorlabsDbContext : DbContext
    {
        public ThorlabsDbContext()
        {
        }

        public ThorlabsDbContext(DbContextOptions<ThorlabsDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Camera> Cameras { get; set; } = null!;
        public virtual DbSet<GroupCalibration> GroupCalibrations { get; set; } = null!;
        public virtual DbSet<Image> Images { get; set; } = null!;
        public virtual DbSet<Increase> Increases { get; set; } = null!;
        public virtual DbSet<Microscope> Microscopes { get; set; } = null!;
        public virtual DbSet<PicsCalibration> PicsCalibrations { get; set; } = null!;
        public virtual DbSet<Tour> Tours { get; set; } = null!;
        public virtual DbSet<User> Users { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Camera>(entity =>
            {
                entity.ToTable("camera");

                entity.Property(e => e.CameraId).HasColumnName("cameraId");

                entity.Property(e => e.DriverType)
                    .HasColumnName("driverType")
                    .HasDefaultValueSql("'generic'");

                entity.Property(e => e.Features).HasColumnName("features");

                entity.Property(e => e.LocalIdentifier).HasColumnName("localIdentifier");

                entity.Property(e => e.Name).HasColumnName("name");

                // Parámetros de captura por cámara (JSON). Nullable: una cámara sin
                // configurar usa el comportamiento por defecto de su driver.
                entity.Property(e => e.SettingsJson).HasColumnName("settingsJson");
            });

            modelBuilder.Entity<GroupCalibration>(entity =>
            {
                entity.HasKey(e => e.GroupCailbrationId);

                entity.ToTable("groupCalibration");

                entity.HasIndex(e => e.CameraId, "IX_groupCalibration_cameraId");

                entity.HasIndex(e => e.IncreaseId, "IX_groupCalibration_increaseId");

                entity.HasIndex(e => e.MicroscopeId, "IX_groupCalibration_microscopeId");

                entity.Property(e => e.GroupCailbrationId).HasColumnName("groupCailbrationId");

                entity.Property(e => e.AditionalInfo).HasColumnName("aditionalInfo");

                entity.Property(e => e.CameraId).HasColumnName("cameraId");

                entity.Property(e => e.Date).HasColumnName("date");

                entity.Property(e => e.IncreaseId).HasColumnName("increaseId");

                entity.Property(e => e.MicroscopeId).HasColumnName("microscopeId");

                entity.HasOne(d => d.Camera)
                    .WithMany(p => p.GroupCalibrations)
                    .HasForeignKey(d => d.CameraId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Increase)
                    .WithMany(p => p.GroupCalibrations)
                    .HasForeignKey(d => d.IncreaseId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Microscope)
                    .WithMany(p => p.GroupCalibrations)
                    .HasForeignKey(d => d.MicroscopeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Image>(entity =>
            {
                entity.HasKey(e => e.IdImage);

                entity.ToTable("image");

                entity.HasIndex(e => e.IdTour, "IX_image_idTour");

                entity.Property(e => e.IdImage).HasColumnName("idImage");

                entity.Property(e => e.GausianVal).HasColumnName("gausianVal");

                entity.Property(e => e.IdTour).HasColumnName("idTour");

                entity.Property(e => e.Name).HasColumnName("name");

                entity.Property(e => e.Path).HasColumnName("path");

                entity.HasOne(d => d.IdTourNavigation)
                    .WithMany(p => p.Images)
                    .HasForeignKey(d => d.IdTour);
            });

            modelBuilder.Entity<Increase>(entity =>
            {
                entity.ToTable("increase");

                entity.Property(e => e.IncreaseId).HasColumnName("increaseId");

                entity.Property(e => e.AditionalInfo).HasColumnName("aditionalInfo");

                entity.Property(e => e.Name).HasColumnName("name");

                entity.Property(e => e.Value).HasColumnName("value");
            });

            modelBuilder.Entity<Microscope>(entity =>
            {
                entity.ToTable("microscope");

                entity.Property(e => e.MicroscopeId).HasColumnName("microscopeId");

                entity.Property(e => e.AditionalInfo).HasColumnName("aditionalInfo");

                entity.Property(e => e.Brand).HasColumnName("brand");

                entity.Property(e => e.Name).HasColumnName("name");

                entity.Property(e => e.Site).HasColumnName("site");
            });

            modelBuilder.Entity<PicsCalibration>(entity =>
            {
                entity.ToTable("picsCalibration");

                entity.HasIndex(e => e.GroupCailbrationId, "IX_picsCalibration_groupCailbrationId");

                entity.Property(e => e.PicsCalibrationId).HasColumnName("picsCalibrationId");

                entity.Property(e => e.Acepted).HasColumnName("acepted");

                entity.Property(e => e.AxeDirectionCalibration).HasColumnName("axeDirectionCalibration");

                entity.Property(e => e.AxisMovementName)
                    .HasColumnType("TEXT(5)")
                    .HasColumnName("axisMovementName");

                entity.Property(e => e.Confidence).HasColumnName("confidence");

                entity.Property(e => e.Dx).HasColumnName("dx");

                entity.Property(e => e.Dy).HasColumnName("dy");

                entity.Property(e => e.GroupCailbrationId).HasColumnName("groupCailbrationId");

                entity.Property(e => e.MeasureUnit).HasColumnName("measureUnit");

                entity.Property(e => e.MovementValue).HasColumnName("movementValue");

                entity.Property(e => e.NumberOfSteps)
                    .HasColumnName("numberOfSteps")
                    .HasDefaultValueSql("1");

                entity.Property(e => e.Pic1).HasColumnName("pic1");

                entity.Property(e => e.Pic2).HasColumnName("pic2");

                entity.HasOne(d => d.GroupCailbration)
                    .WithMany(p => p.PicsCalibrations)
                    .HasForeignKey(d => d.GroupCailbrationId);
            });

            modelBuilder.Entity<Tour>(entity =>
            {
                entity.HasKey(e => e.IdTour);

                entity.ToTable("tour");

                entity.Property(e => e.IdTour).HasColumnName("idTour");

                entity.Property(e => e.Date).HasColumnName("date");

                entity.Property(e => e.EndStatus).HasColumnName("endStatus");

                entity.Property(e => e.NameFolder).HasColumnName("nameFolder");

                entity.HasOne(d => d.PicsCalibration)
                    .WithMany(p => p.Tours)
                    .HasForeignKey(d => d.PicsCalibrationId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.IdUser);

                entity.ToTable("user");

                entity.Property(e => e.IdUser).HasColumnName("idUser");

                entity.Property(e => e.EMail).HasColumnName("eMail");

                entity.Property(e => e.Nickname).HasColumnName("nickname");

                entity.Property(e => e.Password).HasColumnName("password");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}

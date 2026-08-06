using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Database.EntityRepo.Entities;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using Microsoft.EntityFrameworkCore;

namespace GotsThorlabs.Services
{
    public class GroupCalibrationService : IGroupCalibrationService
    {
        private readonly ThorlabsDbContext _db;

        public GroupCalibrationService(ThorlabsDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<GroupCalibrationResponseDTO>> GetAllAsync(CancellationToken ct)
        {
            return await Project(_db.GroupCalibrations.AsNoTracking()).ToListAsync(ct);
        }

        /// <summary>
        /// Proyecta a DTO resolviendo los nombres en la misma consulta.
        ///
        /// Se usa Select y no Include porque solo hacen falta tres nombres: con Include,
        /// EF trae las entidades completas de cámara, microscopio y objetivo por cada
        /// grupo. Los navegadores se leen con ?. porque las claves foráneas de esta base
        /// no siempre apuntan a un registro existente (los ids se escriben como texto);
        /// un grupo con un id huérfano debe seguir listándose con el resto de sus datos
        /// y el nombre en null, no desaparecer ni romper la consulta.
        /// </summary>
        private static IQueryable<GroupCalibrationResponseDTO> Project(IQueryable<GroupCalibration> query) =>
            query.Select(g => new GroupCalibrationResponseDTO
            {
                GroupCailbrationId = g.GroupCailbrationId,
                CameraId = g.CameraId,
                MicroscopeId = g.MicroscopeId,
                IncreaseId = g.IncreaseId,
                Date = g.Date,
                AditionalInfo = g.AditionalInfo,
                CameraName = g.Camera != null ? g.Camera.Name : null,
                MicroscopeName = g.Microscope != null ? g.Microscope.Name : null,
                IncreaseName = g.Increase != null ? g.Increase.Name : null,
                IncreaseValue = g.Increase != null ? g.Increase.Value : null
            });

        public async Task<IEnumerable<GroupCalibration>> GetAllWithDetailsAsync(CancellationToken ct)
        {
            // Se devuelven las entidades completas, como el resto de los Get* del
            // proyecto; los ciclos de referencias los resuelve el
            // ReferenceHandler.IgnoreCycles configurado en Program.cs.
            return await _db.GroupCalibrations
                .AsNoTracking()
                .Include(g => g.Camera)
                .Include(g => g.Microscope)
                .Include(g => g.Increase)
                .Include(g => g.PicsCalibrations)
                .ToListAsync(ct);
        }

        public async Task<GroupCalibrationResponseDTO?> GetByIdAsync(string id, CancellationToken ct)
        {
            return await Project(_db.GroupCalibrations.AsNoTracking().Where(g => g.GroupCailbrationId == id))
                .FirstOrDefaultAsync(ct);
        }

        public async Task<GroupCalibration> CreateAsync(GroupCalibrationDTO dto, CancellationToken ct)
        {
            var entity = new GroupCalibration
            {
                GroupCailbrationId = string.IsNullOrWhiteSpace(dto.GroupCailbrationId) ? Guid.NewGuid().ToString() : dto.GroupCailbrationId,
                CameraId = dto.CameraId,
                MicroscopeId = dto.MicroscopeId,
                IncreaseId = dto.IncreaseId,
                Date = dto.Date,
                AditionalInfo = dto.AditionalInfo
            };

            _db.GroupCalibrations.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity;
        }

        public async Task UpdateAsync(string id, GroupCalibrationDTO dto, CancellationToken ct)
        {
            var existing = await _db.GroupCalibrations.FirstOrDefaultAsync(g => g.GroupCailbrationId == id, ct);
            if (existing is null) throw new KeyNotFoundException($"GroupCalibration {id} not found.");

            existing.CameraId = dto.CameraId;
            existing.MicroscopeId = dto.MicroscopeId;
            existing.IncreaseId = dto.IncreaseId;
            existing.Date = dto.Date;
            existing.AditionalInfo = dto.AditionalInfo;

            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(string id, CancellationToken ct)
        {
            var group = await _db.GroupCalibrations.FirstOrDefaultAsync(g => g.GroupCailbrationId == id, ct);
            if (group is null) throw new KeyNotFoundException($"GroupCalibration {id} not found.");

            _db.GroupCalibrations.Remove(group);
            await _db.SaveChangesAsync(ct);
        }
    }
}

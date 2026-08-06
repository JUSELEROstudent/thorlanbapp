using GotsThorlabs.Database.EntityRepo;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using Microsoft.EntityFrameworkCore;
using TourEntity = GotsThorlabs.Database.EntityRepo.Entities.Tour;

namespace GotsThorlabs.Services
{
    public class TourCrudService : ITourCrudService
    {
        /// <summary>Nombre fijo con el que ProcessTourData guarda el mosaico.</summary>
        private const string StitchingFileName = "openNative.jpg";

        private readonly ThorlabsDbContext _db;
        private readonly IWebHostEnvironment _environment;

        public TourCrudService(ThorlabsDbContext db, IWebHostEnvironment environment)
        {
            _db = db;
            _environment = environment;
        }

        public async Task<IEnumerable<TourResponseDTO>> GetAllAsync(CancellationToken ct)
        {
            var tours = await _db.Tours.AsNoTracking().ToListAsync(ct);

            // El orden lo decide quien consulta, pero el caso habitual es querer el
            // recorrido más reciente, así que se devuelven ya ordenados. Date se guarda
            // como texto: se ordena por fecha cuando se puede parsear y, si no, por
            // IdTour, que es incremental y sirve de desempate estable.
            return tours
                .OrderByDescending(t => DateTime.TryParse(t.Date, out var parsed) ? parsed : DateTime.MinValue)
                .ThenByDescending(t => t.IdTour)
                .Select(ToResponse)
                .ToList();
        }

        private TourResponseDTO ToResponse(TourEntity tour)
        {
            // Se comprueba contra ContentRootPath, que es la misma raíz que usa el
            // PhysicalFileProvider de Program.cs para servir /SouerceStaticFiles. Con
            // CurrentDirectory (lo que usa el resto del archivo) el resultado coincide
            // al arrancar normalmente, pero no si el proceso se lanza desde otra carpeta
            // — y entonces el flag diría que hay mosaico donde la URL da 404.
            var relativePath = $"/SouerceStaticFiles/{tour.NameFolder}/{StitchingFileName}";
            var fullPath = Path.Combine(_environment.ContentRootPath, "StaticFiles", tour.NameFolder, StitchingFileName);
            var exists = System.IO.File.Exists(fullPath);

            return new TourResponseDTO
            {
                IdTour = tour.IdTour,
                Date = tour.Date,
                NameFolder = tour.NameFolder,
                NumberX = tour.NumberX,
                NumberY = tour.NumberY,
                NumberZ = tour.NumberZ,
                Camera = tour.Camera,
                EndStatus = tour.EndStatus,
                PicsCalibrationId = tour.PicsCalibrationId,
                HasStitching = exists,
                StitchingUrl = exists ? relativePath : null
            };
        }

        public async Task<TourEntity?> GetByIdAsync(long id, CancellationToken ct)
        {
            return await _db.Tours.AsNoTracking().FirstOrDefaultAsync(t => t.IdTour == id, ct);
        }

        public async Task<TourEntity> CreateAsync(TourDTO dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.NameFolder))
                throw new ArgumentException("El campo NameFolder es requerido.");

            var entity = new TourEntity
            {
                Date = string.IsNullOrWhiteSpace(dto.Date) ? DateTime.Now.ToString("o") : dto.Date,
                NameFolder = dto.NameFolder,
                NumberX = dto.NumberX,
                NumberY = dto.NumberY,
                NumberZ = dto.NumberZ,
                Camera = dto.Camera,
                EndStatus = dto.EndStatus,
                PicsCalibrationId = dto.PicsCalibrationId
            };

            _db.Tours.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity;
        }

        public async Task UpdateAsync(long id, TourDTO dto, CancellationToken ct)
        {
            var existing = await _db.Tours.FirstOrDefaultAsync(t => t.IdTour == id, ct);
            if (existing is null) throw new KeyNotFoundException($"Tour {id} not found.");

            existing.Date = string.IsNullOrWhiteSpace(dto.Date) ? existing.Date : dto.Date;
            existing.NameFolder = dto.NameFolder;
            existing.NumberX = dto.NumberX;
            existing.NumberY = dto.NumberY;
            existing.NumberZ = dto.NumberZ;
            existing.Camera = dto.Camera;
            existing.EndStatus = dto.EndStatus;

            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(long id, CancellationToken ct)
        {
            var tour = await _db.Tours.FirstOrDefaultAsync(t => t.IdTour == id, ct);
            if (tour is null) throw new KeyNotFoundException($"Tour {id} not found.");

            // Las imágenes (tabla Images, FK IdTour) se borran en cascada por EF Core
            // automáticamente al borrar el Tour (relación requerida, sin OnDelete
            // explícito → Cascade por defecto), así que no hace falta borrarlas a mano.
            _db.Tours.Remove(tour);
            await _db.SaveChangesAsync(ct);

            // Borrado best-effort de la carpeta de imágenes en disco del tour
            // (StaticFiles/<NameFolder>), que hasta ahora quedaba huérfana: el registro
            // desaparecía de la base pero las fotos y el mosaico seguían ocupando disco
            // para siempre. Si falla (permisos, carpeta en uso, etc.) NO se revierte el
            // borrado del tour — solo se registra el error, para que "borrar el tour"
            // desde el front nunca falle por un problema de archivos.
            try
            {
                var folder = Path.Combine(Directory.GetCurrentDirectory(), "StaticFiles", tour.NameFolder);
                if (Directory.Exists(folder))
                    Directory.Delete(folder, recursive: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TourCrudService] No se pudo borrar la carpeta de imágenes del tour {id} ('{tour.NameFolder}'): {ex.Message}");
            }
        }
    }
}

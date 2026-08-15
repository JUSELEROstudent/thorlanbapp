using GotsThorlabs.BLL;

namespace GotsThorlabs.Interfaces
{
    /// <summary>
    /// Planificación de un recorrido antes de lanzarlo.
    ///
    /// Existe porque un recorrido puede durar una hora larga y hasta ahora no había forma
    /// de saberlo hasta empezarlo. El cálculo usa el mismo MosaicGridCalculator que ejecuta
    /// el recorrido real, de modo que la estimación no puede divergir de lo que después pasa.
    /// </summary>
    public interface ITourPlanningService
    {
        /// <summary>
        /// Estima duración, número de imágenes y área realmente cubierta para un grupo de
        /// calibración, un área pedida en milímetros y un patrón de barrido.
        /// </summary>
        Task<TourTimeEstimator.Estimate> EstimateAsync(
            string groupCalibrationId, decimal areaXmm, decimal areaYmm, SweepPattern pattern, CancellationToken ct);
    }
}

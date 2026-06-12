using GotsThorlabs.Models;

namespace GotsThorlabs.Interfaces
{
    public interface IPhaseCorrelationService
    {
        PhaseCorrelationResultDTO DetectShiftFromPaths(string img1Path, string img2Path);
        PhaseCorrelationResultDTO DetectShiftFromFiles(Stream img1Stream, Stream img2Stream);
    }
}
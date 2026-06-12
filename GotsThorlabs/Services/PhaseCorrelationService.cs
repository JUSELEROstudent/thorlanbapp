using GotsThorlabs.BLL;
using GotsThorlabs.Interfaces;
using GotsThorlabs.Models;
using OpenCvSharp;

namespace GotsThorlabs.Services
{
    public class PhaseCorrelationService : IPhaseCorrelationService
    {
        public PhaseCorrelationResultDTO DetectShiftFromPaths(string img1Path, string img2Path)
        {
            using var mat1 = new Mat(img1Path, ImreadModes.Grayscale);
            using var mat2 = new Mat(img2Path, ImreadModes.Grayscale);
            using var mat1f = new Mat();
            using var mat2f = new Mat();
            mat1.ConvertTo(mat1f, MatType.CV_32F);
            mat2.ConvertTo(mat2f, MatType.CV_32F);

            var (shift, confidence) = PhaseCorrelationDetector.DetectShiftSimple(mat1f, mat2f);

            return new PhaseCorrelationResultDTO
            {
                Dx = shift.X,
                Dy = shift.Y,
                Confidence = confidence
            };
        }

        public PhaseCorrelationResultDTO DetectShiftFromFiles(Stream img1Stream, Stream img2Stream)
        {
            Mat? mat1 = null;
            Mat? mat2 = null;
            Mat? mat1Gray = null;
            Mat? mat2Gray = null;
            Mat? mat1f = null;
            Mat? mat2f = null;

            try
            {
                byte[] bytes1, bytes2;

                using (var ms1 = new MemoryStream())
                {
                    img1Stream.CopyTo(ms1);
                    bytes1 = ms1.ToArray();
                }
                using (var ms2 = new MemoryStream())
                {
                    img2Stream.CopyTo(ms2);
                    bytes2 = ms2.ToArray();
                }

                mat1 = Cv2.ImDecode(bytes1, ImreadModes.Color);
                mat2 = Cv2.ImDecode(bytes2, ImreadModes.Color);

                if (mat1.Empty() || mat2.Empty())
                    throw new InvalidOperationException("No se pudo decodificar una o ambas imágenes.");

                mat1Gray = new Mat();
                mat2Gray = new Mat();
                Cv2.CvtColor(mat1, mat1Gray, ColorConversionCodes.BGR2GRAY);
                Cv2.CvtColor(mat2, mat2Gray, ColorConversionCodes.BGR2GRAY);

                mat1f = new Mat();
                mat2f = new Mat();
                mat1Gray.ConvertTo(mat1f, MatType.CV_32F);
                mat2Gray.ConvertTo(mat2f, MatType.CV_32F);

                var (shift, confidence) = PhaseCorrelationDetector.DetectShiftSimple(mat1f, mat2f);

                return new PhaseCorrelationResultDTO
                {
                    Dx = shift.X,
                    Dy = shift.Y,
                    Confidence = confidence
                };
            }
            finally
            {
                mat1?.Dispose();
                mat2?.Dispose();
                mat1Gray?.Dispose();
                mat2Gray?.Dispose();
                mat1f?.Dispose();
                mat2f?.Dispose();
            }
        }
    }
}
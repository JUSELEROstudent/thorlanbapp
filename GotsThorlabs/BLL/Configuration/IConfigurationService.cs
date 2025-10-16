namespace GotsThorlabs.BLL.Configuration
{
    public interface IConfigurationService<T> where T : class
    {
        public T CreateAkindOfCalibration(string configName, decimal factorMicroscopio);
    }
}

namespace GotsThorlabs.Models.Configuration
{
    /// <summary>
    /// DOT que se encarga de llevar los datos de la configuracion  para la calibracion de cada uno de los elementos 
    /// </summary>
    public class ConfigurationDTO
    {
        public decimal FactorMicroscopio { get; set; }
        public string Name { get; set; }
        public int IdConfiguration { get; set; }
    }
}

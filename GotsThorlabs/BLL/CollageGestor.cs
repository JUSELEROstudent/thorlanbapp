namespace GotsThorlabs.BLL
{
    public class CollageGestor
    {
        //string[] encabezados = { "Nombre", "Apellido", "Edad" };
        //string[][] datos = {
        //    new string[] { "Juan", "Pérez", "30" },
        //    new string[] { "María", "López", "25" }
        //};
        private List<string> ejeX;
        private List<string> ejeY;
        private string nombreCsv;

        public CollageGestor(string nombreCsvdoc) {
            nombreCsv = nombreCsvdoc;
        }
        public bool saveTrackDeviceInCsv(string ejeX, string ejeY) {
            string rutaArchivo = "datos.csv";

            using (StreamWriter writer = new StreamWriter(rutaArchivo))
            {
                // Escribir los encabezados
                writer.WriteLine(string.Join(",", encabezados));

                // Escribir los datos
                foreach (string[] fila in datos)
                {
                    writer.WriteLine(string.Join(",", fila));
                }
            }

            Console.WriteLine("Archivo CSV creado exitosamente.");
            return true;
        }

    }
}

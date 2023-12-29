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
        private string pathNameCsv;
        private int currentStep;

        /// <summary>
        /// Clase usada para crear los registro csv del recorrido de la platina 
        /// </summary>
        public CollageGestor(string nombreCsvdoc, string path) {

            string[] encabezados = { "Numero", "EjeX", "EjeZ" };
            string pathsavedoc = Path.Combine( path , nombreCsvdoc);
            using (StreamWriter writer = new StreamWriter(pathsavedoc)) {
                // Escribir los encabezados
                writer.WriteLine(string.Join(",", encabezados));
            }
            
            pathNameCsv = pathsavedoc;
        }
        public bool saveStepDeviceInCsv(string ejeX, string ejeY) {
            currentStep  =  currentStep + 1;
            using (StreamWriter writer = new StreamWriter(pathNameCsv, true))
            {

                // Escribir los datos
                string[] fila = { currentStep.ToString(), ejeX, ejeY };
                writer.WriteLine(string.Join(",", fila));

            }

            //Console.WriteLine("Archivo CSV creado exitosamente.");
            return true;
        }

        public bool endRecordtrackInCsv() {

            return false;
        }

    }
}

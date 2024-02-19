namespace GotsThorlabs.BLL
{
    public class Utilities
    {
        /// <summary>
        /// Metodo usado para traer la fecha en string sin slash o backslash que pueden generar problemas en linux
        /// </summary>
        /// <returns></returns>
        public static string getTimeInString() {
            DateTime fechaHoraActual = DateTime.Now;
            string fecha = fechaHoraActual.ToString();
            fecha = fecha.Replace("/", "_").Replace(" ", "_").Replace(":", "_");
            return fecha; 
        }

        /// <summary>
        /// Metodo usado para crear una carpeta en la direccion especificada
        /// </summary>
        /// <param name="pathFolder">direccion de creacion de la carpeta</param>
        /// <returns>Bool true si fue posible crear la carpeta, false de lo contrario o que la carpeta ya existe</returns>
        public static bool createFolder(string pathFolder) {

            if (!Directory.Exists(pathFolder))
            {
                // Crear la carpeta
                Directory.CreateDirectory(pathFolder);
                Console.WriteLine("Se ha creado la carpeta.");
            }
            else
            {
                Console.WriteLine("La carpeta ya existe.");
                return false;
            }
            return true;
        }
    }
}

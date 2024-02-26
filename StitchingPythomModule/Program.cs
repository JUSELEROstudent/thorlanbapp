using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using System.Diagnostics;

namespace Dynamic
{
    class Program
    {
        //static void Main()
        //{
        //    string pathPython = @"C:\Users\cocuy\OneDrive\Escritorio\thorlabs\GotsThorlabs\StitchingPythomModule\pythontest.py";
        //    string pathcv2 = ".venv/Scripts";
        //    ScriptRuntime py = Python.CreateRuntime();
        //    py.GetClrModule();
        //    py.ImportModule(pathcv2);
        //    dynamic pyProgram = py.UseFile(pathPython);
        //    pyProgram.initfunction();
        //}
        static void Main()
        {
            // Ruta al ejecutable de Python
            string pythonPath = @"C:\Python\python.exe";

            // Ruta al script de Python que quieres ejecutar
            string scriptPath = @"C:\Users\cocuy\OneDrive\Escritorio\thorlabs\GotsThorlabs\StitchingPythomModule\pythontest.py";

            // Argumentos del script de Python (si es necesario)
            string arguments = "argument1 argument2";

            // Crear un proceso para ejecutar el script de Python
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = $"\"{scriptPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // Iniciar el proceso
            using (Process process = Process.Start(startInfo))
            {
                if (process != null)
                {
                    // Leer la salida estándar y los errores estándar (si los hay)
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    // Esperar a que el proceso termine
                    process.WaitForExit();

                    // Mostrar la salida y los errores en la consola de C#
                    Console.WriteLine("Output:");
                    Console.WriteLine(output);
                    Console.WriteLine("Error:");
                    Console.WriteLine(error);
                }
                else
                {
                    Console.WriteLine("Failed to start Python process.");
                }
            }
        }
}
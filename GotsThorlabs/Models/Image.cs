namespace GotsThorlabs.Models
{
        public class Image
        {
            public int IdImage { get; set; }
            public string Nombre { get; set; }
            public int GausianVal { get; set; }
            public string Path { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public int Z { get; set; }
            public int IdTour { get; set; }

        public Image(string nombre,int gausianval,string path,int x,int y,int z )
        {
            Nombre = nombre;
            GausianVal = gausianval;
            Path = path;
            X = x;
            Y = y;
            Z = z;
        }

            public override string ToString()
            {
                return $"Image(IdImage={IdImage}, Nombre='{Nombre}', GausianVal={GausianVal}, Path='{Path}', X={X}, Y={Y}, Z={Z}, IdTour={IdTour})";
            }
        }
}

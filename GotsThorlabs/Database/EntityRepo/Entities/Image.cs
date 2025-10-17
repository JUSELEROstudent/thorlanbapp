namespace GotsThorlabs.Database.EntityRepo.Entities;

public class Image
{
    public int IdImage { get; set; }
    public string Name { get; set; } = default!;
    public double? GausianVal { get; set; }
    public string Path { get; set; } = default!;
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }

    public int IdTour { get; set; }
    public Tour? Tour { get; set; }
}
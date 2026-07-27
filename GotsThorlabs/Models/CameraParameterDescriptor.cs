namespace GotsThorlabs.Models
{
    /// <summary>
    /// Describe UN parámetro configurable de una cámara.
    ///
    /// El backend construye esta lista consultando al dispositivo real; el frontend
    /// arma el formulario a partir de ella, sin conocer nada del driver. Así una sola
    /// vista sirve para la cámara genérica, la uEye y cualquier driver que se agregue
    /// después, sin tocar el frontend.
    /// </summary>
    public class CameraParameterDescriptor
    {
        /// <summary>Clave estable. Es la que se guarda en settingsJson.values.</summary>
        public string Name { get; set; } = default!;

        /// <summary>Etiqueta legible para la interfaz.</summary>
        public string Label { get; set; } = default!;

        /// <summary>"number" | "bool" | "enum". Determina el control que dibuja el frontend.</summary>
        public string Type { get; set; } = "number";

        /// <summary>Mínimo permitido. Nulo cuando el driver no puede informarlo (OpenCV).</summary>
        public double? Min { get; set; }

        /// <summary>Máximo permitido. Nulo cuando el driver no puede informarlo.</summary>
        public double? Max { get; set; }

        /// <summary>Incremento sugerido. Nulo si no aplica o se desconoce.</summary>
        public double? Step { get; set; }

        /// <summary>Unidad para mostrar junto al valor ("ms", "MHz", "fps", "%").</summary>
        public string? Unit { get; set; }

        /// <summary>Valores válidos cuando Type = "enum".</summary>
        public string[]? Options { get; set; }

        /// <summary>Valor leído del dispositivo en este momento, como texto invariante.</summary>
        public string? Current { get; set; }

        /// <summary>Agrupación para la interfaz ("Exposición", "Color", "Temporización").</summary>
        public string? Group { get; set; }

        /// <summary>Explicación breve que se muestra como ayuda en el formulario.</summary>
        public string? Description { get; set; }
    }
}

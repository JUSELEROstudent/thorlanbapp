namespace GotsThorlabs.BLL
{
    /// <summary>
    /// Forma en que el recorrido barre la muestra.
    ///
    /// La elección importa porque el actuador piezoeléctrico inercial avanza distinto
    /// según el sentido: en el montaje medido, la vuelta rinde un 28-32 % menos que la
    /// ida. El eje X nunca retrocede en ningún patrón, así que esto solo afecta a Y.
    /// </summary>
    public enum SweepPattern
    {
        /// <summary>
        /// Serpentina clásica: el eje Y alterna el sentido en cada columna y se comandan
        /// los mismos pasos en ambos. Es el más rápido, pero las columnas de vuelta
        /// quedan comprimidas en la proporción en que difieren los dos sentidos.
        /// Se conserva como referencia y para comparar.
        /// </summary>
        Serpentine = 0,

        /// <summary>
        /// Serpentina con los pasos escalados según el sentido, de modo que las columnas
        /// de ida y de vuelta cubran la misma distancia física. Corrige el espaciado sin
        /// añadir movimientos en vacío, y como cada par de columnas recorre lo mismo en
        /// ambos sentidos, el origen no se desplaza de forma sistemática.
        /// </summary>
        SerpentineScaled = 1,

        /// <summary>
        /// Unidireccional: todas las columnas se recorren en sentido positivo y entre
        /// ellas se vuelve al inicio con un movimiento compensado. Todas las capturas
        /// ocurren en el mismo régimen de fricción, a costa de duplicar el tiempo y de
        /// añadir un retorno cuyo error no aporta ninguna imagen.
        /// </summary>
        Unidirectional = 2
    }
}

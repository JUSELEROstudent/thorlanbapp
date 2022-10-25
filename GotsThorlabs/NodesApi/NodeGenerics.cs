namespace GotsThorlabs.NodesApi
{
    public class NodeGenerics
    {
        public NodeGenerics(WebApplication App)
        {

            // Getters de las peticiones.
            App.MapGet("/hola", () => 
            {
                return "hola persona nueva";
                }
            );

            App.MapGet("/1", () =>
            {
                return "un numero devulelto 1";
            }
            );
        }
    }
}

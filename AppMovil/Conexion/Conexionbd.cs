namespace AppMovil.Conexion
{
    public class Conexionbd
    {
        private String conexion = String.Empty;

        public Conexionbd()
        {
            var constructor = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).
                AddJsonFile("appsettings.json").Build();
            conexion = constructor.GetSection("ConnectionStrings:conexionEscritura").Value;
        }

        public string cadenaSQL()
        {
            return conexion;
        }
    }
}

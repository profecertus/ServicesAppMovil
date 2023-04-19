namespace AppMovil.Models
{
    public class Aplicacion
    {
        public decimal num_version { get; set; } = 0;
        public string nombre_version { get; set; } = String.Empty;
        public DateTime fecha_pub { get; set; }
        public string estado { get; set; } = String.Empty;
    }
}

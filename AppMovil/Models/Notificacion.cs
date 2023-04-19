namespace AppMovil.Models
{
    public class Notificacion
    {
        public int idNotificacion { get; set; } = 0;
        public string titulo { get; set; } = string.Empty;
        public string descripcion { get; set; } = string.Empty;
        public string importancia { get; set; } = string.Empty;
        public string link { get; set; } = string.Empty;
        public DateTime? fechaPublicacion { get; set; } = null;
        public int diasPublicacion { get; set; } = 0;
        public string estado { get; set; } = string.Empty;
    }
}

using System.Diagnostics.CodeAnalysis;

namespace AppMovil.Models
{
    public class Documento
    {
        public int IdDocumento { get; set; }    = 0;
        public string tipoDocumento { get; set; } = string.Empty;
        public string numeroDocumento { get; set; } = string.Empty;
        public string nombreDocumento { get; set; } = string.Empty;
        public string periodo { get; set; } = string.Empty; 
        public string empresa { get; set; } = string.Empty;
        public string recibido { get; set; } = string.Empty;
        public int periodoNum { get; set; } = 0;
        [AllowNull]
        public string? fechaRecepcion { get; set; } = string.Empty;
        [AllowNull]
        public string? contenido { get; set; } = string.Empty;   

    }
}

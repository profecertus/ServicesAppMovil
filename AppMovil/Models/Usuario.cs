using System;
using System.ComponentModel.DataAnnotations;


namespace AppMovil.Models
{
    public class Usuario
    {
        public string tipo_documento { get; set; }
        public string numero_documento { get; set; }
        public string nombres { get; set; }
        public string apellidos { get; set; }
        public string email { get; set; }
        public string num_celular { get; set; }
        public string contrasenna { get; set; }
        public string email_validado { get; set; }
        public int estado { get; set; } = 0;
        public string contrasenna_perdido { get; set; }

    }
}

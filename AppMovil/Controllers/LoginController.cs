using AppMovil.DAO;
using AppMovil.Models;
using Microsoft.AspNetCore.Mvc;

namespace AppMovil.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        [HttpGet]
        [Route("olvidarPassword")]
        public string olvidarPassword(string direccionMail)
        {
            UsuarioDAO usuarioDAO = new UsuarioDAO();
            return usuarioDAO.recuperarClave(direccionMail);            
        }

        [HttpPost]
        [Route("validar")]
        public dynamic validarLogin(LoginDatos info)
        {
            UsuarioDAO usuarioDAO = new UsuarioDAO();
            List<Usuario> usuario = usuarioDAO.getUsuario(info);
            

            if (usuario.Count == 0)
            {
                return new
                {
                    success = false,
                    message = "Las credenciales no son correctas",
                    resultado = usuario
                };
            }
            else
            {
                return new
                {
                    success = true,
                    message = "ok",
                    resultado = usuario
                };
            }
        }

        [HttpPost]
        [Route("cambiarPassword")]
        public dynamic cambiarPassword(Login info)
        {
            UsuarioDAO usuarioDAO = new UsuarioDAO();
            int rpta = usuarioDAO.changePassword(info);

            if (rpta == 0)
            {
                return new
                {
                    success = false,
                    message = "Error al cambiar el password"
                };
            }
            else
            {
                return new
                {
                    success = true,
                    message = "ok"
                };
            }
        }

        [HttpPut]
        [Route("insertar")]
        public dynamic insertarLogin(Login info)
        {
            UsuarioDAO usuarioDAO = new UsuarioDAO();
            int respuesta = usuarioDAO.saveUsuario(info);

            if(respuesta == 0)
            {
                return new
                {
                    success = false,
                    message = "Sucedio un error al momento de registrar su solicitud"
                };
            }
            else
            {
                return new
                {
                    success = true,
                    message = "ok"
                };
            }
        }
    }

    public class ResponseLogin
    {
        public string nombre { get; set; }
        public string apellidos { get; set; }
        public string num_documento { get; set; }
        public string tipo_documento { get; set; }

        public ResponseLogin(string tipo_documento, string num_documento, string nombre, string apellidos)
        {
            this.tipo_documento = tipo_documento;
            this.num_documento = num_documento;
            this.nombre = nombre;
            this.apellidos = apellidos;
        }
    }

    public class Login
    {
        public string tipoDocumento { get; set; }
        public string numDocumento { get; set; }
        public string email { get; set; }
        public string numCelular { get; set; }
        public string contrasenna { get; set; }
        public string fechaEmision { get; set; }
    }

    public class LoginDatos
    {
        public string _tipoDocumento { get; set; }
        public string _numDocumento { get; set; }
        public string _password { get; set; }
    }
}

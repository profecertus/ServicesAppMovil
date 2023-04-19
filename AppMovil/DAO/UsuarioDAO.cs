using System.Data.SqlClient;
using System.Reflection;
using AppMovil.Conexion;
using AppMovil.Controllers;
using AppMovil.Models;
using log4net.Config;
using log4net.Core;
using log4net;
using System.Text;
using RabbitMQ.Client;

namespace AppMovil.DAO
{
    public class UsuarioDAO
    {
        private static ILog getLog()
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("web.config"));
            ILog _logger = LogManager.GetLogger(typeof(LoggerManager));
            return _logger;
        }
        private SqlConnection GetSqlConnection()
        {            
            SqlConnection conn = new SqlConnection( (new Conexionbd()).cadenaSQL() );
            conn.Open();
            return conn;
        }

        public string recuperarClave(string email)
        {
            SqlConnection conn = GetSqlConnection();

            var query = $"select count(1) from USUARIO where email = '{email}' and estado = 1";
            var command = new SqlCommand(query, conn);
            var reader = command.ExecuteReader();
            string respuesta = "";
            int totalFilas = 0;


            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                        totalFilas = reader.GetInt32(0);

                    if (totalFilas == 1)
                    {
                        var factory = new ConnectionFactory() { HostName = "localhost" };
                        using (var connection = factory.CreateConnection())
                        {
                            using (var channel = connection.CreateModel())
                            {
                                channel.QueueDeclare(queue: "QEnviarMail", durable: false, exclusive: false, autoDelete: false, arguments: null);
                                var body = Encoding.UTF8.GetBytes(email);
                                channel.BasicPublish(exchange: "", routingKey: "QEnviarMail", basicProperties: null, body: body);
                            }
                        }
                        respuesta = "Se enviará un correo electrónico con indicaciones.";
                    }
                    else
                    {
                        if (totalFilas == 0)
                        {
                            respuesta = "Su correo electrónico no se encuentra registrado";
                        }
                        else
                        {
                            respuesta = "Su correo electrónico esta asociado a más de una cuenta";
                        }
                    }
                }
            }
            return respuesta;
        }

        public int saveUsuario(Login login)
        {
            SqlConnection conn = GetSqlConnection();
            getLog().Info("numeroDocumento = " + login.numDocumento);
            getLog().Info("fechaEmision = " + login.fechaEmision);
            var query = $"SET DATEFORMAT DMY; insert into SOLICITUD_USUARIO(tipo_documento, numero_documento, fechaEmision, email, num_celular, contrasenna, email_validado, estado) " +
                $"values ('{login.tipoDocumento}', '{login.numDocumento}', CONVERT(DATE, '{login.fechaEmision}'), '{login.email}', '{login.numCelular}', '{login.contrasenna}', 'N', 0);SELECT @@IDENTITY";
            var Command = new SqlCommand(query, conn);
            int rowAffected = 0;
            try
            {
                rowAffected = Int32.Parse(Command.ExecuteScalar().ToString());
            }
            catch (Exception ex) { return-1; }
            
            getLog().Info("RowAffected = " + rowAffected.ToString());
            Command.Dispose();
            conn.Close();
            if (rowAffected > 0)
            {
                var factory = new ConnectionFactory() { HostName = "localhost" };
                using (var connection = factory.CreateConnection())
                {
                    using (var channel = connection.CreateModel())
                    {
                        channel.QueueDeclare(queue: "QValidarSolicitud", durable: false, exclusive: false, autoDelete: false, arguments: null);
                        string message = rowAffected.ToString();
                        var body = Encoding.UTF8.GetBytes(message);
                        channel.BasicPublish(exchange: "", routingKey: "QValidarSolicitud", basicProperties: null, body: body);
                    }
                }
                return 1;
            }
            return 0;
        }

        public int changePassword(Login login)
        {
            SqlConnection conn = GetSqlConnection();
            var query = $"update USUARIO SET contrasenna_perdido = 'N',  contrasenna = ENCRYPTBYPASSPHRASE(numero_documento, '{login.contrasenna}') WHERE tipo_documento = '{login.tipoDocumento}' and numero_documento = '{login.numDocumento}' and estado = 1";

            var Command = new SqlCommand(query, conn);
            int rowAffected = Command.ExecuteNonQuery();

            conn.Close();

            if (rowAffected > 0)
                return 1;
            else
                return 0;
        }

        public List<Usuario> getUsuario(LoginDatos info)
        {
            try
            {
                SqlConnection conn = GetSqlConnection();

                var query = $"select tipo_documento, numero_documento, nombres, apellidos, email, num_celular, convert(varchar(MAX), DECRYPTBYPASSPHRASE(numero_documento, contrasenna)) AS contrasenna, email_validado, estado, contrasenna_perdido from USUARIO where tipo_documento = '{info._tipoDocumento}' and numero_documento = '{info._numDocumento}' and convert(varchar(MAX), DECRYPTBYPASSPHRASE(numero_documento, contrasenna)) = '{info._password}' and estado = 1";

                var command = new SqlCommand(query, conn);
                var reader = command.ExecuteReader();

                var list = new List<Usuario>();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Usuario usuario = new Usuario();
                        if (!reader.IsDBNull(0))
                            usuario.tipo_documento = reader.GetString(0);
                        if (!reader.IsDBNull(1))
                            usuario.numero_documento = reader.GetString(1);
                        if (!reader.IsDBNull(2))
                            usuario.nombres = reader.GetString(2);
                        if (!reader.IsDBNull(3))
                            usuario.apellidos = reader.GetString(3);
                        if (!reader.IsDBNull(4))
                            usuario.email = reader.GetString(4);
                        if (!reader.IsDBNull(5))
                            usuario.num_celular = reader.GetString(5);
                        if (!reader.IsDBNull(6))
                            usuario.contrasenna = reader.GetString(6);
                        if (!reader.IsDBNull(7))
                            usuario.email_validado = reader.GetString(7);
                        if (!reader.IsDBNull(9))
                            usuario.contrasenna_perdido = reader.GetString(9);

                        usuario.estado = reader.GetInt32(8);
                        list.Add(usuario);
                    }
                }

                conn.Close();

                return list;
            }
            catch(Exception e)
            {
                getLog().Error(e.Message);
            }
            return null;
        }
    }
}

using AppMovil.Conexion;
using AppMovil.Models;
using log4net.Config;
using log4net.Core;
using log4net;
using System.Data.SqlClient;
using System.Reflection;

namespace AppMovil.DAO
{
    public class AplicacionDAO
    {
        public static ILog getLog()
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("web.config"));
            ILog _logger = LogManager.GetLogger(typeof(LoggerManager));
            return _logger;
        }
        private SqlConnection GetSqlConnection()
        {
            SqlConnection conn = new SqlConnection((new Conexionbd()).cadenaSQL());
            conn.Open();
            return conn;
        }
        public Aplicacion getAppVersion()
        {
            Aplicacion aplicacion = new Aplicacion();

            using(SqlConnection conn = GetSqlConnection())
            {
                String sqlQuery = "select top 1 num_version, nombre_version, fecha_pub, estado from Aplicacion where estado = 'A'";
                SqlCommand command = new SqlCommand(sqlQuery,conn);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                        while (reader.Read())
                        {
                            aplicacion.num_version = !reader.IsDBNull(0) ? reader.GetDecimal(0) : 0;
                            aplicacion.nombre_version = !reader.IsDBNull(1) ? reader.GetString(1) : string.Empty;
                            aplicacion.fecha_pub = reader.GetDateTime(2);
                            aplicacion.estado = !reader.IsDBNull(3) ? reader.GetString(3) : string.Empty;                            
                        }
                }
            }
            return aplicacion;
        }
    }
}

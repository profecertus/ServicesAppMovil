using AppMovil.Conexion;
using AppMovil.Models;
using log4net.Config;
using log4net.Core;
using log4net;
using System.Data.SqlClient;
using System.Reflection;

namespace AppMovil.DAO
{
    public class NotificacionDAO
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
            try
            {
                getLog().Info((new Conexionbd()).cadenaSQL());
                SqlConnection conn = new SqlConnection((new Conexionbd()).cadenaSQL());
                conn.Open();
                return conn;
            }
            catch(Exception e)
            {
                getLog().Error(e.Message);
            }
            return null;
        }
        public List<Notificacion> getNotificaciones()
        {
            ILog logger = getLog();
            logger.Info("Comenzando la llamada");
            List<Notificacion> listRpta = new List<Notificacion>();
            using (SqlConnection conn = GetSqlConnection())
            {
                string sqlSentence = "select * from NOTIFICACION where estado = 'A' order by fecha_publicacion DESC";
                SqlCommand command = new SqlCommand(sqlSentence, conn);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if(reader.HasRows)
                        while(reader.Read())
                        {
                            Notificacion n = new Notificacion();
                            n.idNotificacion = !reader.IsDBNull(0)? reader.GetInt32(0):0;
                            n.titulo = !reader.IsDBNull(1)?reader.GetString(1):string.Empty;
                            n.descripcion   = !reader.IsDBNull(2)?reader.GetString(2):string.Empty;                            
                            n.link = !reader.IsDBNull(3)?reader.GetString(3):string.Empty;
                            n.fechaPublicacion = reader.GetDateTime(4);
                            n.diasPublicacion = !reader.IsDBNull(5)?reader.GetInt32(5):0;
                            n.estado = !reader.IsDBNull(6)?reader.GetString(6):string.Empty;
                            listRpta.Add(n);
                        }
                }
            }
            return listRpta;
        }
    }
}

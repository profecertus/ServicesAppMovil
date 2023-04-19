using AppMovil.Conexion;
using log4net.Config;
using log4net.Core;
using log4net;
using System.Data.SqlClient;
using System.Reflection;
using AppMovil.Models;

namespace AppMovil.DAO
{
    public class DocumentoDAO
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

        public urlDocument getUrlDocument(int idDocumento)
        {
            string respuesta = string.Empty;
            SqlConnection conn = GetSqlConnection();
            urlDocument ud = new urlDocument();

            var query = $"select url, fecha_recepcion, u.nombres + ' ' + u.apellidos as nombres from documento d inner join usuario u on d.tipo_documento = u.tipo_documento and d.numero_documento = u.numero_documento where id_documento = {idDocumento} and recibido = 'S' order by d.periodoNum DESC ";
            AppMovil.DAO.DocumentoDAO.getLog().Info($"getUrlDocument - LLEGUE 0 = {query}");
            var command = new SqlCommand(query, conn);
            var reader = command.ExecuteReader();

            AppMovil.DAO.DocumentoDAO.getLog().Info($"getUrlDocument - LLEGUE 1 = {reader.HasRows}");
            if (reader.HasRows)
                while (reader.Read())
                {
                    ud.url = reader.GetString(0);
                    ud.fechaRecepcion = reader.GetDateTime(1);
                    ud.nombres = reader.GetString(2);
                }
                    
            return ud;
        }
               
                public void marcarError(int idDocumento)
        {
            try
            {
                SqlConnection con = GetSqlConnection();
                //SqlTransaction tx = con.BeginTransaction();
                var query = $"update DOCUMENTO SET eror = 1 WHERE Id_documento = '{idDocumento}'";
                var Command = new SqlCommand(query, con);
                int rowAffected = Command.ExecuteNonQuery();
                con.Close();
            }
            catch (Exception ex) {
                throw(ex);
            }
        }
        

        public int firmaReporte(int IdReporte)
        {
            try
            {
                SqlConnection con = GetSqlConnection();
                //SqlTransaction tx = con.BeginTransaction();
                var query = $"update DOCUMENTO SET recibido = 'S', fecha_recepcion = SYSDATETIME() WHERE Id_documento = '{IdReporte}'";
                var Command = new SqlCommand(query, con);
                int rowAffected = Command.ExecuteNonQuery();
                
                
                if (rowAffected > 0)
                {
                    //tx.Commit();
                    con.Close();
                    return 1;
                }
                else
                {
                    //tx.Rollback();  
                    con.Close() ;
                    return 0;
                }
                 
            } catch (Exception ex)
            {
                throw(ex);
                return -1;
            }

        }

        public BoletaXML getAllXML(int IdDocumento)
        {
            SqlConnection conn = GetSqlConnection();

            var query = $"SELECT contenido.query('/NewDataSet/EMPR') AS EMPR, contenido.query('/NewDataSet/CABE') AS CABE, contenido.query('/NewDataSet/NUEV') AS NUEV, contenido.query('/NewDataSet/DETA') AS DETA, contenido.query('/NewDataSet/TIEM') AS TIEM, contenido.query('/NewDataSet/HORA') AS HORA, contenido.query('/NewDataSet/EVAL') AS EVAL, fecha_recepcion FROM Documento WHERE id_documento = {IdDocumento}";
            var command = new SqlCommand(query, conn);
            var reader = command.ExecuteReader();

            BoletaXML bolXML = new BoletaXML();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                        bolXML.empr = reader.GetString(0);
                    if (!reader.IsDBNull(1))
                        bolXML.cabe = reader.GetString(1);
                    if (!reader.IsDBNull(2))
                        bolXML.nuev = reader.GetString(2);
                    if (!reader.IsDBNull(3))
                        bolXML.deta = reader.GetString(3);
                    if (!reader.IsDBNull(4))
                        bolXML.tiem = reader.GetString(4);
                    if (!reader.IsDBNull(5))
                        bolXML.hora = reader.GetString(5);
                    if (!reader.IsDBNull(6))
                        bolXML.eval = reader.GetString(6);
                    if (!reader.IsDBNull(7))
                        bolXML.fechaRecepcion = reader.GetDateTime(7);
                }
            }
            conn.Close();
            return bolXML;
        }

        public Documento getDocumentoXML(int idDocumento)
        {
            SqlConnection conn = GetSqlConnection();

            var query = $"select recibido, contenido, fecha_recepcion from documento where id_documento = {idDocumento}";
            var command = new SqlCommand(query, conn);
            var reader = command.ExecuteReader();

            Documento documento = new Documento();
            if (reader.HasRows)
                while (reader.Read())
                {
                    documento.recibido = reader.GetString(0);
                    documento.contenido = reader.GetString(1);
                    if (!reader.IsDBNull(2))
                        documento.fechaRecepcion = reader.GetDateTime(2).ToString();
                }
            conn.Close();
            return documento;
        }

        public List<Documento> getDocumentos(string tipoDocumento, string numeroDocumento)
        {
            SqlConnection conn = GetSqlConnection();

            var query = $"select id_documento, tipo_documento, numero_documento, nombre_documento, periodo, empresa, recibido, periodoNum from DOCUMENTO where tipo_documento = '{tipoDocumento}' and numero_documento = '{numeroDocumento}' ORDER BY periodoNum DESC";
            var command = new SqlCommand(query, conn);
            var reader = command.ExecuteReader();

            var list = new List<Documento>();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    Documento documento = new Documento();
                    if (!reader.IsDBNull(0))
                        documento.IdDocumento = reader.GetInt32(0);
                    if (!reader.IsDBNull(1))
                        documento.tipoDocumento = reader.GetString(1);
                    if (!reader.IsDBNull(2))
                        documento.numeroDocumento = reader.GetString(2);
                    if (!reader.IsDBNull(3))
                        documento.nombreDocumento = reader.GetString(3);
                    if (!reader.IsDBNull(4))
                        documento.periodo = reader.GetString(4);
                    if (!reader.IsDBNull(5))
                        documento.empresa = reader.GetString(5);
                    if (!reader.IsDBNull(6))
                        documento.recibido = reader.GetString(6);
                    if (!reader.IsDBNull(7))
                        documento.periodoNum = reader.GetInt32(7);
                    list.Add(documento);
                }
            }

            conn.Close();

            return list;
        }
    }

    public class BoletaXML{
        public string empr { get; set; } = string.Empty; 
        public string cabe { get;set; } = string.Empty;
        public string nuev { get;set;} = string.Empty;
        public string deta { get;set; } = string.Empty;
        public string tiem { get;set; } = string.Empty;
        public string hora { get;set; } = string.Empty;
        public string eval { get;set; } = string.Empty;
        public DateTime fechaRecepcion { get;set; } 

    }

    public class urlDocument
    {
        public string url { get; set; }
        public DateTime fechaRecepcion { get; set; }
        public string nombres { get; set; }
    }
}

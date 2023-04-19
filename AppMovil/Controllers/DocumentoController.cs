using AppMovil.DAO;
using AppMovil.Models;
using AppMovil.Proceso;
using Microsoft.AspNetCore.Mvc;

namespace AppMovil.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentoController : ControllerBase
    {
       
        [HttpGet]
        [Route("getPDF")]
        public async Task<FileStreamResult> getPDFAsync(int  idDocumento)
        {
            AppMovil.DAO.DocumentoDAO.getLog().Info($"ID DOCUMENTO - {idDocumento}");
            DocumentoProcess documentoProcess = new DocumentoProcess();
            AppMovil.DAO.DocumentoDAO.getLog().Info($"LLEGUE 0");
            byte[] mybytearray = await documentoProcess.getPDFAsync(idDocumento);
            AppMovil.DAO.DocumentoDAO.getLog().Info($"LLEGUE 1");
            return File(new MemoryStream(mybytearray), "application/pdf", "employee.pdf");
        }

        [HttpGet]
        [Route("getDocumentos")]
        public List<Documento> getDocumentos(string tipoDocumento, string numeroDocumento)
        {
            DocumentoDAO documentoDAO = new DocumentoDAO();
            List<Documento> documentos = documentoDAO.getDocumentos(tipoDocumento, numeroDocumento);
            return documentos;
        }

        [HttpPost]
        [Route("saveFirma")]
        public dynamic saveFirma(int IdReporte)
        {
            DocumentoDAO documentoDAO = new DocumentoDAO();
            int respuesta = documentoDAO.firmaReporte(IdReporte);
            if(respuesta == -1 || respuesta == 0)
            {
                return new
                {
                    success = false,
                    message = "Error al momento de actualizar la Firma",
                    resultado = respuesta
                };
            }
            return new
            {
                success = true,
                message = "Se Firmo correctamente",
                resultado = respuesta
            };
        }
    }
}

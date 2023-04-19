using AppMovil.DAO;
using AppMovil.Models;
using AppMovil.Proceso;
using Microsoft.AspNetCore.Mvc;

namespace AppMovil.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AplicacionController:ControllerBase
    {
        [HttpGet]
        [Route("getLastVersion")]
        public Aplicacion getLastVersion()
        {
            AplicacionDAO.getLog().Info("Inicio de Version");
            AplicacionDAO app = new AplicacionDAO();
            return app.getAppVersion(); 
        }
    }
}

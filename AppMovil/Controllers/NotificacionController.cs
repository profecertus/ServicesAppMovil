using AppMovil.DAO;
using AppMovil.Models;
using AppMovil.Proceso;
using Microsoft.AspNetCore.Mvc;

namespace AppMovil.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificacionController : ControllerBase
    {
        [HttpGet]
        [Route("getNotificacionesActivas")]
        public List<Notificacion> getNotificacionesActivas()
        {
            NotificacionDAO notificacionDAO = new NotificacionDAO();
            List<Notificacion> notificaciones = notificacionDAO.getNotificaciones();
            return notificaciones;
        }
    }
}

using AirVinyl.DataAccessLayer;
using System.Linq;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Routing;

namespace AirVinyl.Api.Controllers
{
    public class VinylRecordController : ODataController
    {
        private AirVinylDbContext _context = new AirVinylDbContext();

        [HttpGet]
        [ODataRoute("VinylRecords")]
        public IHttpActionResult GetAllVinylRecords()
        {
            return Ok(_context.VinylRecords);
        }

        [HttpGet]
        [ODataRoute("VinylRecords({key})")]
        public IHttpActionResult GetAllVinylRecords([FromODataUri]int key)
        {
            var record = _context.VinylRecords.FirstOrDefault(x => x.VinylRecordId == key);
            if (record == null)
                return NotFound();

            return Ok(record);
        }
        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
            base.Dispose(disposing);
        }
    }
}
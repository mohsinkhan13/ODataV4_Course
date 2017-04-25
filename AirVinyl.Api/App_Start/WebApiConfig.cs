using System.Web.Http;
using System.Web.OData.Extensions;
using Microsoft.OData.Edm;
using System.Web.OData.Builder;
using AirVinyl.Model;

namespace AirVinyl.Api
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.MapODataServiceRoute("ODataRoute", "odata", GetEdmModel());
            config.EnsureInitialized();
        }

        private static IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.Namespace = "AirVinyl";
            builder.ContainerName = "AirVinylContainer";

            builder.EntitySet<Person>("People");
            builder.EntitySet<VinylRecord>("VinylRecords");

            return builder.GetEdmModel();
        }
    }
}

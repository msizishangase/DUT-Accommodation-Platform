using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(DUT_Accommodation_Platform.Startup))]
namespace DUT_Accommodation_Platform
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}

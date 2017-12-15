using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens;
using Microsoft.Owin.Security.ActiveDirectory;
using Owin;
using Thinktecture.IdentityModel.Owin;
using System.Threading.Tasks;
using System.Security.Claims;

namespace AcmeOData
{
    public partial class Startup
    {
        //hardcode our user store so we don't need a db for this example
        public static Dictionary<string, string> users = new Dictionary<string, string>()
        {
            {"acmeodata", "Password1!" }
        };

        public void ConfigureAuth(IAppBuilder app)
        {
            var basicAuthOptions = new BasicAuthenticationOptions("contosoodata", async (username, password) => await Authenticate(username, password));
            app.UseBasicAuthentication(basicAuthOptions);
        }

        private async Task<IEnumerable<Claim>> Authenticate(string username, string password)
        {
            if (users.ContainsKey(username))
                if (users[username] == password)
                    return new List<Claim> { new Claim("name", username) };
            return null;
        }

        // For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuthAAD(IAppBuilder app)
        {
            app.UseWindowsAzureActiveDirectoryBearerAuthentication(
                new WindowsAzureActiveDirectoryBearerAuthenticationOptions
                {
                    Tenant = ConfigurationManager.AppSettings["ida:Tenant"],
                    TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidAudience = ConfigurationManager.AppSettings["ida:Audience"]
                    },
                });
        }
    }
}

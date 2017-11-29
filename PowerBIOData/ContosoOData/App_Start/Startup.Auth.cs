using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens;
using Microsoft.Owin.Security.ActiveDirectory;
using Owin;
using Thinktecture.IdentityModel.Owin;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ContosoOData
{
    public partial class Startup
    {
        //hardcode user store so we don't need to deploy a db in this sample
        public static Dictionary<string, string> users = new Dictionary<string, string>()
        {
            {"contosoodata", "Password1!" }
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

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Swashbuckle.Swagger.Annotations;
using System.Threading;
using System.Runtime.InteropServices;
using mscoree;
using System.Reflection;

namespace testappdomain.Controllers
{
    class ApplicationProxy : MarshalByRefObject
    {
        public void DoSomething(string pathToDll, string webRoot, string msg)
        {
            int lastDot = pathToDll.LastIndexOf(@"/");
            string binRoot = pathToDll.Substring(0, lastDot);
            Assembly asm = Assembly.Load(new AssemblyName()
            {
                CodeBase = binRoot  + @"/mylib.dll"
            });

            Type myclass = asm.GetType("mylib.DummyClass");
            MethodInfo mymethod = myclass.GetMethod("somehtml");
           
            myclass.InvokeMember("somehtml", BindingFlags.InvokeMethod, null, null, 
                new object[2] { webRoot, msg });
        }
    }
    public class ValuesController : ApiController
    {
        // GET api/values
        [SwaggerOperation("GetAll")]
        public IEnumerable<string> Get()
        {
            string[] domainNames = new string[10];
            int ii = 0;
            
            ICorRuntimeHost host = null;
            IntPtr enumHandle = IntPtr.Zero;
            try
            {
                host = new mscoree.CorRuntimeHost();
                host.EnumDomains(out enumHandle);
                object domain = null;

                host.NextDomain(enumHandle, out domain);
                while (domain != null && ii < 10)
                {
                    AppDomain ad = (AppDomain)domain;
                    domainNames[ii++] = ad.FriendlyName;
                    host.NextDomain(enumHandle, out domain);
                }
            }
            finally
            {
                if (host != null)
                {
                    if (enumHandle != IntPtr.Zero)
                    {
                        host.CloseEnum(enumHandle);
                    }

                    Marshal.ReleaseComObject(host);
                }
            }

            return domainNames;
        }

        // POST api/values
        [SwaggerOperation("Create")]
        [SwaggerResponse(HttpStatusCode.Created)]
        public void Post([FromBody]string value)
        {
            string pathToDll = Assembly.GetExecutingAssembly().CodeBase;
            AppDomainSetup domainSetup = new AppDomainSetup { PrivateBinPath = pathToDll };
            AppDomain newDomain = AppDomain.CreateDomain(value, null, domainSetup);
            ApplicationProxy proxy = (ApplicationProxy)(newDomain.CreateInstanceFromAndUnwrap(
                pathToDll, typeof(ApplicationProxy).FullName));

            string webRoot = System.Web.HttpContext.Current.Server.MapPath(@"~");
            proxy.DoSomething(pathToDll, webRoot, value);
        }
    }
}

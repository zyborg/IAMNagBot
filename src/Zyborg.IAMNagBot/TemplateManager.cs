using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Amazon.S3;
using Newtonsoft.Json;

namespace Zyborg.IAMNagBot
{
    public class TemplateManager
    {
        public static async Task<T> Resolve<T>(object model, string urlTemplate)
        {
            var urlParsed = Scriban.Template.Parse(urlTemplate);
            var urlResolved = urlParsed.Render(model);

            var url = new Uri(urlResolved);
            string templateContent;
            
            if (url.Scheme == "asm-resource")
            {
                Assembly asm = Assembly.GetEntryAssembly();
                switch (url.Host)
                {
                    case "_entry":
                        break;
                    case "_calling":
                        asm = Assembly.GetCallingAssembly();
                        break;
                    case "_executing":
                        asm = Assembly.GetExecutingAssembly();
                        break;
                    case string asmType when !string.IsNullOrEmpty(asmType):
                        asm = Assembly.GetAssembly(Type.GetType(asmType));
                        break;
                }

                var resName = url.PathAndQuery.Trim('/');
                using (var res = asm.GetManifestResourceStream(resName))
                using (var reader = new StreamReader(res))
                {
                    templateContent = await reader.ReadToEndAsync();
                }
            }
            if (url.Scheme == "s3")
            {
                var bucketName = url.Host;
                var objectKey = url.PathAndQuery.TrimStart('/');

                using (var s3Client = new AmazonS3Client())
                using (var resp = await s3Client.GetObjectAsync(bucketName, objectKey))
                using (var stream = resp.ResponseStream)
                using (var reader = new StreamReader(stream))
                {
                    templateContent = await reader.ReadToEndAsync();
                }
            }
            else
            {
                using (var web = new WebClient())
                {
                    templateContent = await web.DownloadStringTaskAsync(url.ToString());
                }
            }

            var templateParsed = Scriban.Template.Parse(templateContent);
            var templateResolved = templateParsed.Render(model);

            return new YamlDotNet.Serialization.Deserializer().Deserialize<T>(templateResolved);
        }
    }
}
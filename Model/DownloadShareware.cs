using System.IO.Compression;
using System.Net;
using System.Net.Security;

namespace WOLF3D
{
    public class DownloadShareware
    {
        public static void Main(string[] args = null)
        {
            if (args == null || args.Length == 0)
                args = new string[] { string.Empty };
            if (!System.IO.File.Exists(System.IO.Path.Combine(args[0], "WOLF3D.EXE")))
            {
                // I would very much prefer to use the official URL ftp://ftp.3drealms.com/share/1wolf14.zip
                // However, that packs the shareware episode inside it's original installer, and extracting files from that is a pain.
                // To avoid that, I'll probably just use a zip of a fully installed shareware version instead.
                // In case I ever want to revisit the issue of extracting from the shareware installer, I found some C code to extract the shareware files here: https://github.com/rpmfusion/wolf3d-shareware
                // That code seems to depend on this library here: https://github.com/twogood/dynamite
                ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback((sender, certificate, chain, policyErrors) => { return true; });
                using (var client = new WebClient())
                {
                    client.DownloadFile("https://archive.org/download/Wolfenstein3d/Wolfenstein3dV14sw.ZIP", "Wolfenstein3dV14sw.ZIP");
                }
                ZipFile.ExtractToDirectory("Wolfenstein3dV14sw.ZIP", args[0]);
                System.IO.File.Delete("Wolfenstein3dV14sw.ZIP");
            }
        }
    }
}

using Godot;
using System.IO.Compression;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace WOLF3D
{
    public class DownloadSharewareWOLF3D
    {
        public static void Main()
        {
            Directory directory = new Directory();
            if (!directory.FileExists("WOLF3D\\WOLF3D.EXE"))
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
                ZipFile.ExtractToDirectory("Wolfenstein3dV14sw.ZIP", "WOLF3D");
                directory.Remove("Wolfenstein3dV14sw.ZIP");
            }
            directory.Dispose();
        }

        #region helper functions
        private static bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            //Return true if the server certificate is ok
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            bool acceptCertificate = true;
            string msg = "The server could not be validated for the following reason(s):\r\n";

            //The server did not present a certificate
            if ((sslPolicyErrors &
                 SslPolicyErrors.RemoteCertificateNotAvailable) == SslPolicyErrors.RemoteCertificateNotAvailable)
            {
                msg = msg + "\r\n    -The server did not present a certificate.\r\n";
                acceptCertificate = false;
            }
            else
            {
                //The certificate does not match the server name
                if ((sslPolicyErrors &
                     SslPolicyErrors.RemoteCertificateNameMismatch) == SslPolicyErrors.RemoteCertificateNameMismatch)
                {
                    msg = msg + "\r\n    -The certificate name does not match the authenticated name.\r\n";
                    acceptCertificate = false;
                }

                //There is some other problem with the certificate
                if ((sslPolicyErrors &
                     SslPolicyErrors.RemoteCertificateChainErrors) == SslPolicyErrors.RemoteCertificateChainErrors)
                {
                    foreach (X509ChainStatus item in chain.ChainStatus)
                    {
                        if (item.Status != X509ChainStatusFlags.RevocationStatusUnknown &&
                            item.Status != X509ChainStatusFlags.OfflineRevocation)
                            break;

                        if (item.Status != X509ChainStatusFlags.NoError)
                        {
                            msg = msg + "\r\n    -" + item.StatusInformation;
                            acceptCertificate = false;
                        }
                    }
                }
            }
            if (acceptCertificate == false)
            {
                acceptCertificate = true;
            }
            return acceptCertificate;
        }
        #endregion
    }
}

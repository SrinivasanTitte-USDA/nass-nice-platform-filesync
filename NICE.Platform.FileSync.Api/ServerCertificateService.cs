using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace NICE.Platform.FileSync.Api;

public class ServerCertificateService
{
    public X509Certificate2 Certificate { get; }

    public ServerCertificateService()
    {
        using var rsa = RSA.Create(2048);

        var req = new CertificateRequest("CN=SignalRServer", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(10));

        // IMPORTANT: Make it memory-only
        Certificate = new X509Certificate2(cert.Export(X509ContentType.Pkcs12));
    }
}

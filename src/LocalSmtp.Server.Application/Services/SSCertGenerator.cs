/*
Copyright(c) 2009 - 2018, smtp4dev project contributors All rights reserved.
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
    * Neither the name of smtp4dev nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS;
OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
source: https://github.com/rnwood/smtp4dev/tree/master/Rnwood.Smtp4dev/Server
*/


using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

[assembly: InternalsVisibleTo("Rnwood.Smtp4dev.Tests")]
namespace LocalSmtp.Server.Application.Services;

internal static class SSCertGenerator
{

    public static X509Certificate2 CreateSelfSignedCertificate(string hostname)
    {
        var randomGenerator = new CryptoApiRandomGenerator();
        var random = new SecureRandom(randomGenerator);

        var certGenerator = new X509V3CertificateGenerator();
        certGenerator.SetSubjectDN(new X509Name("CN=" + hostname));
        certGenerator.SetIssuerDN(new X509Name("CN=" + hostname));

        var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), random);
        certGenerator.SetSerialNumber(serialNumber);

        certGenerator.SetNotBefore(DateTime.UtcNow.Date);
        certGenerator.SetNotAfter(DateTime.UtcNow.Date.AddYears(10));

        var keyGenerationParameters = new KeyGenerationParameters(random, 2048);

        var keyPairGenerator = new RsaKeyPairGenerator();
        keyPairGenerator.Init(keyGenerationParameters);
        var keyPair = keyPairGenerator.GenerateKeyPair();
        certGenerator.SetPublicKey(keyPair.Public);

        var signatureFactory = new Asn1SignatureFactory("SHA256WithRSA", keyPair.Private, random);
        var cert = certGenerator.Generate(signatureFactory);

        var store = new Pkcs12Store();
        var certificateEntry = new X509CertificateEntry(cert);
        store.SetCertificateEntry("cert", certificateEntry);
        store.SetKeyEntry("cert", new AsymmetricKeyEntry(keyPair.Private), new[] { certificateEntry });
        var stream = new MemoryStream();
        store.Save(stream, "".ToCharArray(), random);

        return new X509Certificate2(stream.ToArray(), "", X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
    }
}

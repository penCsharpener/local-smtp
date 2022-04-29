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


using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace LocalSmtp.Server.Application.Services;

public static class CertificateHelper
{
    /// <summary>
    /// Load certificate and private key
    /// </summary>
    /// <param name="certificatePath"></param>
    /// <param name="certificateKeyPath"></param>
    /// <returns>Exported x509 Certificate</returns>
    public static X509Certificate2 LoadCertificateWithKey(string certificatePath, string certificateKeyPath)
    {
        using var rsa = RSA.Create();
        var keyPem = File.ReadAllText(certificateKeyPath);
        var keyDer = UnPem(keyPem);
        rsa.ImportPkcs8PrivateKey(keyDer, out _);
        var certNoKey = new X509Certificate2(certificatePath);
        return new X509Certificate2(certNoKey.CopyWithPrivateKey(rsa).Export(X509ContentType
            .Pfx));
    }

    public static X509Certificate2 LoadCertificate(string certificatePath, string password = "")
    {
        return new X509Certificate2(certificatePath, password);
    }

    /// <summary>
    /// This is a shortcut that assumes valid PEM
    /// -----BEGIN words-----\r\nbase64\r\n-----END words-----
    /// </summary>
    /// <param name="pem"></param>
    /// <returns></returns>
    public static byte[] UnPem(string pem)
    {
        const string dashes = "-----";
        const string newLine = "\r\n";
        pem = NormalizeLineEndings(pem);
        var index0 = pem.IndexOf(dashes, StringComparison.Ordinal);
        var index1 = pem.IndexOf(newLine, index0 + dashes.Length, StringComparison.Ordinal) + newLine.Length;
        var index2 = pem.IndexOf(dashes, index1, StringComparison.Ordinal) - newLine.Length; //TODO: /n
        return Convert.FromBase64String(pem.Substring(index1, index2 - index1));
    }

    private static string NormalizeLineEndings(string val)
    {
        return Regex.Replace(val, @"\r\n|\n\r|\n|\r", "\r\n");
    }
}
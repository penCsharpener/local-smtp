/*
Copyright(c) 2009 - 2018, smtp4dev project contributors All rights reserved.
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
    * Neither the name of smtp4dev nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS;
OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
source: https://github.com/rnwood/smtp4dev/tree/master/Rnwood.Smtp4dev/Services
*/

using LocalSmtp.Server.Application.Services.Abstractions;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LocalSmtp.Server.Application.Services;

public class HostingEnvironmentHelper : IHostingEnvironmentHelper
{
    private readonly IHostEnvironment hostEnvironment;

    public HostingEnvironmentHelper(IHostEnvironment hostEnvironment)
    {
        this.hostEnvironment = hostEnvironment;
    }

    /// <summary>
    /// Check if this process is running on Windows in an in process instance in IIS
    /// </summary>
    /// <returns>True if Windows and in an in process instance on IIS, false otherwise</returns>
    private static bool IsRunningInProcessIIS()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return false;
        }

        var processName = Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().ProcessName);
        return (processName.Contains("w3wp", StringComparison.OrdinalIgnoreCase) ||
                processName.Contains("iisexpress", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get path to appsettings.json, for IIS this is the runtime path.
    /// </summary>
    /// <returns>appsettings.json filePath</returns>
    public string GetSettingsFilePath()
    {
        var dataDir = IsRunningInProcessIIS()
            ? Path.Join(hostEnvironment.ContentRootPath, "smtp4dev")
            : Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "smtp4dev");
        return Path.Join(dataDir, "appsettings.json");
    }
}

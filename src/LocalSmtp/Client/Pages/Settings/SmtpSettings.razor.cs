using Blazored.LocalStorage;
using LocalSmtp.Shared.ApiModels;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace LocalSmtp.Client.Pages.Settings
{
    public partial class SmtpSettings
    {

        [Inject]
        public ILocalStorageService LocalStore { get; set; }

        private Server _server;
        private string _hostName;
        private int _smtpPort;
        private bool _allowRemoteConnections;
        private bool _isFormValid;
        private MudForm _form;

        protected override async Task OnInitializedAsync()
        {
            _server = await LocalStore.GetItemAsync<Server>("smtpInfo");
            _hostName = _server.HostName;
            _smtpPort = _server.PortNumber;
            _allowRemoteConnections = _server.AllowRemoteConnections;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await _form.Validate();
        }

    }
}
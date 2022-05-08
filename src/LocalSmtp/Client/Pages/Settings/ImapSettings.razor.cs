using Blazored.LocalStorage;
using LocalSmtp.Shared.ApiModels;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace LocalSmtp.Client.Pages.Settings
{
    public partial class ImapSettings
    {
        [Inject]
        public ILocalStorageService LocalStore { get; set; }

        private Server _server;
        private int? _imapPort;
        private bool _isFormValid;
        private MudForm _form;

        protected override async Task OnInitializedAsync()
        {
            _server = await LocalStore.GetItemAsync<Server>("smtpInfo");
            _imapPort = _server.ImapPortNumber;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await _form.Validate();
        }
    }
}
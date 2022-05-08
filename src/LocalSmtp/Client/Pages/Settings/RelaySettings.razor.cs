using Blazored.LocalStorage;
using LocalSmtp.Shared.ApiModels;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace LocalSmtp.Client.Pages.Settings
{
    public partial class RelaySettings
    {
        [Inject]
        public ILocalStorageService LocalStore { get; set; }

        private Server _server;
        private string _hostName;
        private string _username;
        private string _password;
        private string _senderAddresses;
        private string _autoRelay;
        private int _relaySmtpPort;
        private bool _isRelayEnabled;
        private string _tlsMode;
        private bool _isFormValid;
        private MudForm _form;
        private bool _isPasswordVisible;
        private InputType _passwordInput = InputType.Password;
        private string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;


        protected override async Task OnInitializedAsync()
        {
            _server = await LocalStore.GetItemAsync<Server>("smtpInfo");
            _isRelayEnabled = !string.IsNullOrWhiteSpace(_server.RelayOptions.SmtpServer);
            _hostName = _server.RelayOptions.SmtpServer;
            _relaySmtpPort = _server.RelayOptions.SmtpPort;
            _username = _server.RelayOptions.Login;
            _password = _server.RelayOptions.Password;
            _senderAddresses = _server.RelayOptions.SenderAddress;
            _autoRelay = string.Join(", ", _server.RelayOptions.AutomaticEmails);
            _tlsMode = _server.RelayOptions.TlsMode;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await _form.Validate();
        }

        void PasswordButtonClick()
        {
            if (_isPasswordVisible)
            {
                _isPasswordVisible = false;
                _passwordInputIcon = Icons.Material.Filled.VisibilityOff;
                _passwordInput = InputType.Password;
            }
            else
            {
                _isPasswordVisible = true;
                _passwordInputIcon = Icons.Material.Filled.Visibility;
                _passwordInput = InputType.Text;
            }
        }

    }
}
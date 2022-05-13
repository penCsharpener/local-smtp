using Blazored.LocalStorage;
using LocalSmtp.Client.Models.Settings;
using LocalSmtp.Shared.ApiModels;
using LocalSmtp.Shared.Extensions;
using Mapster;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Net.Http.Json;

namespace LocalSmtp.Client.Pages.Settings
{
    public partial class RelaySettings
    {
        [Inject]
        public ILocalStorageService LocalStore { get; set; }

        [Inject]
        public HttpClient HttpClient { get; set; }

        [Inject]
        public ISnackbar Snackbar { get; set; }

        private Server _server;
        private RelayOptionsFormModel _formModel;
        private bool _isFormValid;
        private MudForm? _form;
        private bool _isPasswordVisible;
        private InputType _passwordInput = InputType.Password;
        private string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;

        protected override async Task OnInitializedAsync()
        {
            _server = await LocalStore.GetItemAsync<Server>("smtpInfo");
            _formModel = _server.RelayOptions is null ? new() { AutomaticEmails = Array.Empty<string>() } : _server.RelayOptions.Adapt<RelayOptionsFormModel>();
            _formModel.IsRelayEnabled = !string.IsNullOrWhiteSpace(_formModel.SmtpServer);
            _formModel.AutomaticRelayRecipients = _formModel.AutomaticEmails!.IsNullOrEmpty() ? string.Empty : _formModel.AutomaticEmails!.JoinString(", ");

            await ValidateFormAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await ValidateFormAsync();
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

        private async Task SubmitAsync()
        {
            await ValidateFormAsync();

            _server.RelayOptions = _formModel;
            _server.RelayOptions.AutomaticEmails = _formModel.AutomaticRelayRecipients.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()).ToArray();
            _server.RelayOptions.SmtpServer = _formModel.IsRelayEnabled ? _formModel.SmtpServer : default;

            await LocalStore.SetItemAsync("smtpInfo", _server);
            await HttpClient.PostAsJsonAsync("/api/server", _server);

            Snackbar.Add("Saved", Severity.Success);
        }

        private async Task ValidateFormAsync()
        {
            if (_form is not null)
            {
                await _form.Validate();
            }

            _isFormValid = _form?.IsValid == true;
        }
    }
}
using Blazored.LocalStorage;
using LocalSmtp.Shared.ApiModels;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Net.Http.Json;

namespace LocalSmtp.Client.Pages.Settings
{
    public partial class LimitSettings
    {
        [Inject]
        public ILocalStorageService LocalStore { get; set; }

        [Inject]
        public HttpClient HttpClient { get; set; }

        [Inject]
        public ISnackbar Snackbar { get; set; }

        private Server _server;
        private bool _isFormValid;
        private MudForm? _form;

        protected override async Task OnInitializedAsync()
        {
            _server = await LocalStore.GetItemAsync<Server>("smtpInfo");

            await ValidateFormAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await ValidateFormAsync();
        }

        private async Task SubmitAsync()
        {
            await ValidateFormAsync();

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
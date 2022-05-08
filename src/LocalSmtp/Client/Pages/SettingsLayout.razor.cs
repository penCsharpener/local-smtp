using Blazored.LocalStorage;
using LocalSmtp.Shared.ApiModels;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace LocalSmtp.Client.Pages
{
    public partial class SettingsLayout
    {
        [Inject]
        public HttpClient Http { get; set; }

        [Inject]
        public ILocalStorageService LocalStore { get; set; }

        private Server _server;

        protected override async Task OnInitializedAsync()
        {
            _server = await Http.GetFromJsonAsync<Server>("/api/Server");
            await LocalStore.SetItemAsync("smtpInfo", _server!);
        }
    }
}
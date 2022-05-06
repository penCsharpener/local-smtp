using Blazored.LocalStorage;
using LocalSmtp.Shared.ApiModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using System.Net.Http.Json;

namespace LocalSmtp.Client.Pages.Messages
{
    public partial class Messages : IAsyncDisposable
    {
        List<MessageSummary>? messageSummaries = new();
        private string searchString = "";
        private MudTable<MessageSummary> mudTable;
        HubConnection _hubConnection = null;
        private Message? _messageContent;
        private string? _messageRaw;
        private string? _messageHtml;
        private int _selectedRowNumber = -1;
        private Server? _serverInfo;

        private string _overrideAddressesToRelay;
        private bool _relayPopupOpen;

        public MessageSummary SelectedMessage { get; set; }

        [Inject]
        public ILocalStorageService LocalStorage { get; set; }

        [Inject]
        public HttpClient HttpClient { get; set; }

        [Inject]
        public NavigationManager Nav { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadDataAsync();
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(Nav.ToAbsoluteUri("/hubs/notifications"))
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<string>("messageschanged", UpdateMessagesAsync);
            _hubConnection.On<Guid>("messageReadChanged", UpdateMessageReadAsync);

            await _hubConnection.StartAsync();

            _serverInfo = await LocalStorage.GetItemAsync<Server>("smtpInfo");
        }

        private async Task UpdateMessagesAsync(string msg)
        {
            await LoadDataAsync();
            await InvokeAsync(StateHasChanged);
        }

        private async Task UpdateMessageReadAsync(Guid id)
        {
            var message = messageSummaries.Where(m => m.Id == id).FirstOrDefault();

            if (message is null)
            {
                return;
            }

            message.IsUnread = false;

            await InvokeAsync(StateHasChanged);
        }

        public async void OnSelectedMessageChangedAsync(TableRowClickEventArgs<MessageSummary> args)
        {
            var messageSummary = args.Item;

            if (messageSummary.IsUnread)
            {
                await HttpClient.PostAsync($"api/Messages/{messageSummary.Id}", null);
            }

            _messageRaw = await HttpClient.GetStringAsync($"/api/Messages/{messageSummary.Id}/raw");
            _messageHtml = await HttpClient.GetStringAsync($"/api/Messages/{messageSummary.Id}/html");
            _messageContent = await HttpClient.GetFromJsonAsync<Message>($"/api/Messages/{messageSummary.Id}");

            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadDataAsync()
        {
            messageSummaries = await HttpClient.GetFromJsonAsync<List<MessageSummary>>("/api/messages?sortColumn=receivedDate&sortIsDescending=true");
        }

        private async Task DeleteDataAsync()
        {
            await HttpClient.DeleteAsync("/api/messages/*");
            messageSummaries.Clear();
        }

        private async Task DeleteSelectedAsync()
        {
            await HttpClient.DeleteAsync($"/api/messages/{SelectedMessage.Id}");
            messageSummaries.Remove(SelectedMessage);
        }

        private void OpenRelayPopOver()
        {
            _relayPopupOpen = !_relayPopupOpen;
            _overrideAddressesToRelay = SelectedMessage.To;
        }

        private async Task RelayAsync()
        {
            var addresses = _overrideAddressesToRelay.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()).ToArray();
            await HttpClient.PostAsJsonAsync($"/api/messages/{SelectedMessage.Id}/relay", new { OverrideRecipientAddresses = addresses });
            OpenRelayPopOver();
        }

        private bool FilterFunc1(MessageSummary element) => FilterFunc(element, searchString);

        private bool FilterFunc(MessageSummary element, string searchString)
        {
            if (string.IsNullOrWhiteSpace(searchString))
            {
                return true;
            }

            if (element.Subject.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (element.From.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (element.To.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if ($"{element.ReceivedDate}".Contains(searchString))
            {
                return true;
            }

            return false;
        }

        private string SelectedRowStyleFunc(MessageSummary messageSummary, int rowNumber)
        {
            _selectedRowNumber = mudTable.SelectedItem?.Equals(messageSummary) == true ? rowNumber : -1;

            return _selectedRowNumber == -1 ? string.Empty : "background-color: lightblue;";
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection is not null)
            {
                await _hubConnection.DisposeAsync();
            }
        }

        public string IsMailRead(bool isUnread)
        {
            return isUnread ? "font-weight: 700;" : "font-weight: 400;";
        }
    }
}
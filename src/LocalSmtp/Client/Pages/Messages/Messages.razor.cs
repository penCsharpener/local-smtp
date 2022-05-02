using LocalSmtp.Shared.ApiModels;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using System.Net.Http.Json;

namespace LocalSmtp.Client.Pages.Messages
{
    public partial class Messages
    {
        IEnumerable<MessageSummary>? Elements = new List<MessageSummary>();
        private MessageSummary _selectedMessage;
        private string searchString = "";
        private MudTable<MessageSummary> mudTable;
        HubConnection _hubConnection = null;
        private Message? _messageContent;
        private string? _messageRaw;
        private string? _messageHtml;

        public MessageSummary SelectedMessage
        {
            get => _selectedMessage;
            set
            {
                _selectedMessage = value;
                OnSelectedMessageChangedAsync(value);
            }
        }

        protected override async Task OnInitializedAsync()
        {
            await LoadDataAsync();
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(Nav.ToAbsoluteUri("/hubs/notifications"))
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<string>("messageschanged", UpdateMessagesAsync);

            await _hubConnection.StartAsync();
        }

        private async Task UpdateMessagesAsync(string msg)
        {
            await LoadDataAsync();
            await InvokeAsync(StateHasChanged);
        }

        private async void OnSelectedMessageChangedAsync(MessageSummary messageSummary)
        {
            _messageRaw = await httpClient.GetStringAsync($"/api/Messages/{messageSummary.Id}/raw");
            _messageHtml = await httpClient.GetStringAsync($"/api/Messages/{messageSummary.Id}/html");
            _messageContent = await httpClient.GetFromJsonAsync<Message>($"/api/Messages/{messageSummary.Id}");
            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadDataAsync()
        {
            Elements = await httpClient.GetFromJsonAsync<List<MessageSummary>>("/api/messages?sortColumn=receivedDate&sortIsDescending=true");
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
            if (mudTable.SelectedItem != null && mudTable.SelectedItem.Equals(messageSummary))
            {
                return "background-color: lightblue";
            }

            return string.Empty;
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection is not null)
            {
                await _hubConnection.DisposeAsync();
            }
        }
    }
}
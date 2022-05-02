using LocalSmtp.Shared.ApiModels;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using System.Net.Http.Json;

namespace LocalSmtp.Client.Pages.Messages
{
    public partial class Messages
    {
        List<MessageSummary>? Elements = new();
        private string searchString = "";
        private MudTable<MessageSummary> mudTable;
        HubConnection _hubConnection = null;
        private Message? _messageContent;
        private string? _messageRaw;
        private string? _messageHtml;
        private int _selectedRowNumber = -1;

        public MessageSummary SelectedMessage { get; set; }

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
        }

        private async Task UpdateMessagesAsync(string msg)
        {
            await LoadDataAsync();
            await InvokeAsync(StateHasChanged);
        }

        private async Task UpdateMessageReadAsync(Guid id)
        {
            var message = Elements.Where(m => m.Id == id).FirstOrDefault();

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
                await httpClient.PostAsync($"api/Messages/{messageSummary.Id}", null);
            }

            _messageRaw = await httpClient.GetStringAsync($"/api/Messages/{messageSummary.Id}/raw");
            _messageHtml = await httpClient.GetStringAsync($"/api/Messages/{messageSummary.Id}/html");
            _messageContent = await httpClient.GetFromJsonAsync<Message>($"/api/Messages/{messageSummary.Id}");
            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadDataAsync()
        {
            Elements = await httpClient.GetFromJsonAsync<List<MessageSummary>>("/api/messages?sortColumn=receivedDate&sortIsDescending=true");
        }

        private async Task DeleteDataAsync()
        {
            await httpClient.DeleteAsync("/api/messages/*");
            Elements.Clear();
        }

        private async Task DeleteSelectedAsync()
        {
            await httpClient.DeleteAsync($"/api/messages/{SelectedMessage.Id}");
            Elements.Remove(SelectedMessage);
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
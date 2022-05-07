using LocalSmtp.Shared.ApiModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using System.Net.Http.Json;

namespace LocalSmtp.Client.Pages.Sessions;

public partial class Sessions
{
    List<SessionSummary>? sessionSummaries = new();
    private MudTable<SessionSummary> mudTable;
    HubConnection _hubConnection = null;
    private Session? _sessionContent;
    private string? _sessionRaw;
    private int _selectedRowNumber = -1;

    public SessionSummary SelectedSession { get; set; }

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

        _hubConnection.On<string>("sessionschanged", UpdateDataAsync);

        await _hubConnection.StartAsync();
    }

    private async Task UpdateDataAsync(string msg)
    {
        await LoadDataAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadDataAsync()
    {
        sessionSummaries = await HttpClient.GetFromJsonAsync<List<SessionSummary>>("/api/sessions");
    }

    public async void OnSelectedMessageChangedAsync(TableRowClickEventArgs<SessionSummary> args)
    {
        var sessionSummary = args.Item;

        _sessionRaw = await HttpClient.GetStringAsync($"/api/sessions/{sessionSummary.Id}/log");
        _sessionContent = await HttpClient.GetFromJsonAsync<Session>($"/api/sessions/{sessionSummary.Id}");

        await InvokeAsync(StateHasChanged);

    }

    private async Task DeleteDataAsync()
    {
        await HttpClient.DeleteAsync("/api/sessions/*");
        sessionSummaries.Clear();
    }

    private async Task DeleteSelectedAsync()
    {
        await HttpClient.DeleteAsync($"/api/sessions/{SelectedSession.Id}");
        sessionSummaries.Remove(SelectedSession);
    }

    private string SelectedRowStyleFunc(SessionSummary sessionSummary, int rowNumber)
    {
        _selectedRowNumber = mudTable.SelectedItem?.Equals(sessionSummary) == true ? rowNumber : -1;

        return _selectedRowNumber == -1 ? string.Empty : Constants.Css.SelectedRowBackgroundColor;
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}

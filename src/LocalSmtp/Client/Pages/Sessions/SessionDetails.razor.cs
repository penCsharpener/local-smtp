using LocalSmtp.Shared.ApiModels;
using Microsoft.AspNetCore.Components;

namespace LocalSmtp.Client.Pages.Sessions
{
    public partial class SessionDetails
    {
        [Parameter]
        public Session? Session { get; set; }

        [Parameter]
        public string? SessionRaw { get; set; }

        [Parameter]
        public int DecreaseHeight { get; set; }

        [Inject]
        public NavigationManager Nav { get; set; }

        private void OpenLog()
        {
            Nav.NavigateTo(Nav.ToAbsoluteUri($"/api/Sessions/{Session?.Id}/log").ToString());
        }
    }
}
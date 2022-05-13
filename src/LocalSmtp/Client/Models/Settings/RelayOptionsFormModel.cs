using LocalSmtp.Shared.ApiModels;

namespace LocalSmtp.Client.Models.Settings;

public class RelayOptionsFormModel : ServerRelayOptions
{
    public bool IsRelayEnabled { get; set; }
    public string AutomaticRelayRecipients { get; set; }
}

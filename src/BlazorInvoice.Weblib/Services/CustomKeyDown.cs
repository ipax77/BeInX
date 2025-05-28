using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorInvoice.Weblib.Services;

[EventHandler("oncustomkeydown", typeof(KeyboardEventArgs),
    enableStopPropagation: true, enablePreventDefault: true)]
public static class EventHandlers
{
}

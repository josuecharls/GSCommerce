using Microsoft.JSInterop;

namespace GSCommerce.Client.Services
{
    public class SessionSyncService : IAsyncDisposable
    {
        private readonly IJSRuntime _js;
        private DotNetObjectReference<SessionSyncService>? _dotNetRef;
        
        public event Action? SessionEndedInAnotherWindow;

        public SessionSyncService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task StartListeningAsync()
        {
            try
            {
                _dotNetRef = DotNetObjectReference.Create(this);
                await _js.InvokeVoidAsync("sessionSync.startListening", _dotNetRef);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error starting session sync: {ex.Message}");
            }
        }

        [JSInvokable]
        public void OnSessionEndedInAnotherWindow()
        {
            Console.WriteLine("🔔 Invoking SessionEndedInAnotherWindow event");
            SessionEndedInAnotherWindow?.Invoke();
        }

        public async ValueTask DisposeAsync()
        {
            _dotNetRef?.Dispose();
        }
    }
}




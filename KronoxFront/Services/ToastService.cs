using Microsoft.JSInterop;

namespace KronoxFront.Services;

public class ToastService : IToastService
{
    private readonly IJSRuntime _js;

    public ToastService(IJSRuntime js)
        => _js = js;

    public Task Success(string message)
        => _js.InvokeVoidAsync("toast.success", message).AsTask();

    public Task Error(string message)
        => _js.InvokeVoidAsync("toast.error", message).AsTask();

    public Task Warning(string message)
=> _js.InvokeVoidAsync("toast.warning", message).AsTask();

    public Task Info(string message)
        => _js.InvokeVoidAsync("toast.info", message).AsTask();
}
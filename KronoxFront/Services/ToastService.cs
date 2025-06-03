using Microsoft.JSInterop;

// Service för att visa toast-notiser via JavaScript

namespace KronoxFront.Services;

public class ToastService : IToastService
{
    private readonly IJSRuntime _js;

    public ToastService(IJSRuntime js) => _js = js;

    public Task Success(string message) => Show("success", message);
    public Task Error(string message) => Show("error", message);
    public Task Warning(string message) => Show("warning", message);
    public Task Info(string message) => Show("info", message);

    private Task Show(string type, string message)
        => _js.InvokeVoidAsync($"toast.{type}", message).AsTask();
}
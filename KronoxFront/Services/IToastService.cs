
namespace KronoxFront.Services;

// Abstraktion för att visa toast-notiser från komponenterna.
public interface IToastService
{
    Task Success(string message);
    Task Error(string message);
    Task Warning(string message);
    Task Info(string message);
}
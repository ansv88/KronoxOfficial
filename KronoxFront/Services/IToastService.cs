
namespace KronoxFront.Services;

// Interface för att visa toast-notiser i användargränssnittet
public interface IToastService
{
    Task Success(string message);
    Task Error(string message);
    Task Warning(string message);
    Task Info(string message);
}
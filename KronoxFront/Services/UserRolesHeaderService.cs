using Microsoft.AspNetCore.Components.Authorization;

namespace KronoxFront.Services;

// Tjanst som beraknar det kommaseparerade v?rdet for X-User-Roles-headern utifran
// den INLOGGADE ANVANDARENS faktiska roller i den korrekta Blazor-kretsens
// DI-scope.
//
// OBS: Denna tjanst maste injiceras direkt i den anropande komponenten/tjansten
// (via vanlig konstruktorinjektion) - ALDRIG losas upp inifran
// IHttpClientFactory:s ConfigureClient-delegat eller en DelegatingHandler.
// IHttpClientFactory bygger sin interna handler-pipeline (inklusive
// ConfigureClient-anrop och eventuella AddHttpMessageHandler<T>-instanser) via
// sitt EGET internt cachade DI-scope, som INTE ar samma scope som den
// anropande Blazor-kretsen. Scopeade tjanster (som AuthenticationStateProvider
// eller IHttpContextAccessor) som loses upp harifran pekar darfor pa fel
// scope/anvandare - vilket var den bakomliggande orsaken till att tidigare
// forsok (bade HttpContext- och AuthenticationStateProvider-baserade) att
// baka in rollheadern i ConfigureApiClient fungerade forsta gangen (av en
// tillfallighet under forrendringen) men inte i den riktiga interaktiva
// kretsen.
public class UserRolesHeaderService
{
    private const string RoleHeaderName = "X-User-Roles";

    private readonly AuthenticationStateProvider _authStateProvider;
    private Task<string?>? _cachedRolesHeaderValue;

    public UserRolesHeaderService(AuthenticationStateProvider authStateProvider)
    {
        _authStateProvider = authStateProvider;
    }

    // Beraknar (och cachar for kretsens livslangd) X-User-Roles-vardet.
    public Task<string?> GetRolesHeaderValueAsync()
        => _cachedRolesHeaderValue ??= ComputeRolesHeaderValueAsync();

    private async Task<string?> ComputeRolesHeaderValueAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var roles = authState.User?.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        return roles != null && roles.Count > 0 ? string.Join(",", roles) : null;
    }

    // Applicerar X-User-Roles-headern pa den angivna HttpClient-instansen.
    // Sakert att anropa flera ganger per klient (skriver alltid over ev. gammalt
    // varde) eftersom klientinstanser fran IHttpClientFactory kan ateranvandas
    // mellan anrop for namngivna klienter.
    public async Task ApplyToAsync(HttpClient client)
    {
        var rolesHeaderValue = await GetRolesHeaderValueAsync();

        if (client.DefaultRequestHeaders.Contains(RoleHeaderName))
        {
            client.DefaultRequestHeaders.Remove(RoleHeaderName);
        }

        if (!string.IsNullOrEmpty(rolesHeaderValue))
        {
            client.DefaultRequestHeaders.Add(RoleHeaderName, rolesHeaderValue);
        }
    }
}

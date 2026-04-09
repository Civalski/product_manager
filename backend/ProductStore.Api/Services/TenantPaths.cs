using Microsoft.AspNetCore.Hosting;

namespace ProductStore.Api.Services;

public static class TenantPaths
{
    public static string ResolveUsersDataDirectory(IWebHostEnvironment env) =>
        env.IsDevelopment()
            ? Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "..", "data", "users"))
            : Path.Combine(env.ContentRootPath, "Data", "users");
}

namespace OopsBlazorStudyWebApp;

public sealed record MenuNavigationLink(
    string MenuName,
    string Icon,
    string Description,
    string Href,
    string Note,
    int? MenuId = null,
    string Status = "");

public static class MenuNavigationCatalog
{
    private sealed record MenuNavigationDefinition(MenuNavigationLink Link, string[] MenuNames);

    private static readonly MenuNavigationDefinition[] MasterDefinitions =
    {
        new(new("User Master", "👤", "Create users, set passwords, and assign operational rights.", "user-master", "Security"), new[] { "User Master" }),
        new(new("Printer Master", "🖨️", "Register printer IDs, ports, and activation status.", "printer-master", "Devices"), new[] { "Printer Master" }),
        new(new("Role Master", "🛡️", "Configure warehouse roles and decide menu availability.", "role-master", "Access"), new[] { "Role Master" }),
        new(new("Menu Master", "🧭", "Review client navigation groups and route-level visibility.", "menu-master", "Navigation"), new[] { "Menu Master" })
    };

    private static readonly MenuNavigationDefinition[] StoreDefinitions =
    {
        new(new("Reports", "📄", "Open store transaction and stock movement report workspace.", "store-reports", "Store"), new[] { "Store", "Store Reports", "Store Report" })
    };

    private static readonly MenuNavigationDefinition[] ProductionDefinitions =
    {
        new(new("Reports", "📄", "Open production activity and job tracking report workspace.", "production-reports", "Production"), new[] { "Production", "Production Reports", "Production Report" })
    };

    private static readonly MenuNavigationDefinition[] DispatchDefinitions =
    {
        new(new("Reports", "📄", "Open outbound movement and client handover report workspace.", "dispatch-reports", "Dispatch"), new[] { "Dispatch", "Dispatch Reports", "Dispatch Report" })
    };

    private static IReadOnlyList<MenuNavigationDefinition> AllDefinitions =>
        MasterDefinitions
            .Concat(StoreDefinitions)
            .Concat(ProductionDefinitions)
            .Concat(DispatchDefinitions)
            .ToList();

    public static IReadOnlyList<MenuNavigationLink> DefaultMasterLinks => MasterDefinitions.Select(definition => definition.Link).ToList();

    public static IReadOnlyList<MenuNavigationLink> GetActiveMasterLinks(IEnumerable<MenuMasterListDto> menus) =>
        GetActiveLinks(menus, MasterDefinitions);

    public static IReadOnlyList<MenuNavigationLink> GetActiveStoreLinks(IEnumerable<MenuMasterListDto> menus) =>
        GetActiveLinks(menus, StoreDefinitions);

    public static IReadOnlyList<MenuNavigationLink> GetActiveProductionLinks(IEnumerable<MenuMasterListDto> menus) =>
        GetActiveLinks(menus, ProductionDefinitions);

    public static IReadOnlyList<MenuNavigationLink> GetActiveDispatchLinks(IEnumerable<MenuMasterListDto> menus) =>
        GetActiveLinks(menus, DispatchDefinitions);

    public static bool IsKnownHref(string href)
    {
        var normalizedHref = href.Trim().TrimStart('/').ToLowerInvariant();
        return AllDefinitions.Any(definition => string.Equals(definition.Link.Href, normalizedHref, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<MenuNavigationLink> GetActiveLinks(IEnumerable<MenuMasterListDto> menus, IReadOnlyList<MenuNavigationDefinition> definitions)
    {
        var definitionsByMenuName = definitions
            .SelectMany(definition => definition.MenuNames.Select(menuName => new
            {
                MenuName = NormalizeMenuName(menuName),
                Definition = definition
            }))
            .ToDictionary(item => item.MenuName, item => item.Definition, StringComparer.OrdinalIgnoreCase);

        var mappedLinks = new List<MenuNavigationLink>();
        var mappedHrefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var menu in menus.Where(menu => IsActive(menu.Status)).OrderBy(menu => menu.Id))
        {
            var normalizedMenuName = NormalizeMenuName(menu.MenuName);
            if (!definitionsByMenuName.TryGetValue(normalizedMenuName, out var definition) ||
                !mappedHrefs.Add(definition.Link.Href))
            {
                continue;
            }

            mappedLinks.Add(definition.Link with
            {
                MenuId = menu.Id,
                MenuName = menu.MenuName,
                Status = menu.Status
            });
        }

        return mappedLinks;
    }

    private static bool IsActive(string status) =>
        string.Equals(status, "Activate", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeMenuName(string menuName) =>
        new(menuName.Where(char.IsLetterOrDigit).ToArray());
}

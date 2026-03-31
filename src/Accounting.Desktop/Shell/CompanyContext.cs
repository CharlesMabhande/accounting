namespace Accounting.Desktop.Shell;

public static class CompanyContext
{
    public static int? SelectedCompanyId { get; private set; }

    public static event Action? Changed;

    public static void Set(int companyId)
    {
        if (SelectedCompanyId == companyId)
            return;
        SelectedCompanyId = companyId;
        Changed?.Invoke();
    }

    public static void Clear()
    {
        if (SelectedCompanyId == null)
            return;
        SelectedCompanyId = null;
        Changed?.Invoke();
    }
}

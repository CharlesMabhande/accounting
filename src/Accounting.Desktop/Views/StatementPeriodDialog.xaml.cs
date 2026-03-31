using System.Windows;

namespace Accounting.Desktop.Views;

public partial class StatementPeriodDialog
{
    public DateOnly FromDate { get; private set; }
    public DateOnly ToDate { get; private set; }

    public StatementPeriodDialog()
    {
        InitializeComponent();
        var today = DateOnly.FromDateTime(DateTime.Today);
        FromPicker.SelectedDate = new DateTime(today.Year, 1, 1);
        ToPicker.SelectedDate = DateTime.Today;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (FromPicker.SelectedDate is not { } f || ToPicker.SelectedDate is not { } t)
        {
            MessageBox.Show("Select both dates.", "Statement", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        FromDate = DateOnly.FromDateTime(f);
        ToDate = DateOnly.FromDateTime(t);
        if (ToDate < FromDate)
            (FromDate, ToDate) = (ToDate, FromDate);

        DialogResult = true;
    }
}

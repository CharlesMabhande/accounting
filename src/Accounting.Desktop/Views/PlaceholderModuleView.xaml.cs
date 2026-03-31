namespace Accounting.Desktop.Views;

public partial class PlaceholderModuleView
{
    public PlaceholderModuleView(string title, string description)
    {
        InitializeComponent();
        TitleText.Text = title;
        BodyText.Text = description;
    }
}

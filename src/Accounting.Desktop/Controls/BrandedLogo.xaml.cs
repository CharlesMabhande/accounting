using System.Windows;
using System.Windows.Controls;

namespace Accounting.Desktop.Controls;

public partial class BrandedLogo : UserControl
{
    public static readonly DependencyProperty LogoHeightProperty = DependencyProperty.Register(
        nameof(LogoHeight),
        typeof(double),
        typeof(BrandedLogo),
        new PropertyMetadata(48.0, OnSizeChanged));

    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
        nameof(CornerRadius),
        typeof(double),
        typeof(BrandedLogo),
        new PropertyMetadata(14.0, OnSizeChanged));

    public BrandedLogo()
    {
        InitializeComponent();
        Loaded += (_, _) => Apply();
    }

    /// <summary>Height of the logo; width follows aspect ratio.</summary>
    public double LogoHeight
    {
        get => (double)GetValue(LogoHeightProperty);
        set => SetValue(LogoHeightProperty, value);
    }

    /// <summary>Rounded rectangle radius for the logo frame.</summary>
    public double CornerRadius
    {
        get => (double)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BrandedLogo b)
            b.Apply();
    }

    private void Apply()
    {
        var h = LogoHeight;
        if (h <= 0)
            h = 48;
        ClipBorder.CornerRadius = new CornerRadius(CornerRadius);
        LogoImage.Height = h;
        LogoImage.Width = double.NaN;
    }
}

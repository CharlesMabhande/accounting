using System.Windows;
using System.Windows.Controls;

namespace Accounting.Desktop.Controls;

public partial class BrandedFormHeader : UserControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(BrandedFormHeader),
        new PropertyMetadata(""));

    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(
        nameof(Subtitle),
        typeof(string),
        typeof(BrandedFormHeader),
        new PropertyMetadata("", OnSubtitleChanged));

    public static readonly DependencyProperty IconGlyphProperty = DependencyProperty.Register(
        nameof(IconGlyph),
        typeof(string),
        typeof(BrandedFormHeader),
        new PropertyMetadata("", OnIconGlyphChanged));

    public static readonly DependencyProperty LogoHeightProperty = DependencyProperty.Register(
        nameof(LogoHeight),
        typeof(double),
        typeof(BrandedFormHeader),
        new PropertyMetadata(38.0));

    public BrandedFormHeader()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshSubtitleVisibility();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    /// <summary>Single Segoe MDL2 / Fluent Icons character.</summary>
    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public double LogoHeight
    {
        get => (double)GetValue(LogoHeightProperty);
        set => SetValue(LogoHeightProperty, value);
    }

    private static void OnSubtitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BrandedFormHeader h)
            h.RefreshSubtitleVisibility();
    }

    private static void OnIconGlyphChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not BrandedFormHeader h)
            return;
        var s = e.NewValue as string;
        if (string.IsNullOrEmpty(s))
        {
            h.IconBlock.Visibility = Visibility.Collapsed;
            h.IconBlock.Text = "";
        }
        else
        {
            h.IconBlock.Text = s;
            h.IconBlock.Visibility = Visibility.Visible;
        }
    }

    private void RefreshSubtitleVisibility()
    {
        SubtitleBlock.Visibility = string.IsNullOrWhiteSpace(Subtitle) ? Visibility.Collapsed : Visibility.Visible;
    }
}

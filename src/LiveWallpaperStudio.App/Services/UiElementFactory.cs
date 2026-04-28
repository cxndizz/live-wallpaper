using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfOrientation = System.Windows.Controls.Orientation;

namespace LiveWallpaperStudio.App.Services;

public static class UiElementFactory
{
    public static TextBlock CreateIcon(Func<string, object> findResource, string glyph, double fontSize = 16)
    {
        return new TextBlock
        {
            Text = glyph,
            FontFamily = (WpfFontFamily)findResource("FluentIconFont"),
            FontSize = fontSize,
            Foreground = (WpfBrush)findResource("Text"),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };
    }

    public static StackPanel CreateIconText(Func<string, object> findResource, string glyph, string label, double iconSize = 14)
    {
        return new StackPanel
        {
            Orientation = WpfOrientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            Children =
            {
                CreateIcon(findResource, glyph, iconSize),
                new TextBlock
                {
                    Text = label,
                    Margin = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.SemiBold
                }
            }
        };
    }

    public static StackPanel CreateOptionContent(
        Func<string, object> findResource,
        bool isSelected,
        string glyph,
        string title,
        string description)
    {
        var indicator = new Border
        {
            Width = 18,
            Height = 18,
            CornerRadius = new CornerRadius(9),
            BorderThickness = new Thickness(1),
            BorderBrush = isSelected ? (WpfBrush)findResource("AccentBlue") : (WpfBrush)findResource("Border"),
            Background = isSelected ? (WpfBrush)findResource("AccentGradient") : WpfBrushes.Transparent,
            Margin = new Thickness(0, 0, 10, 0)
        };

        if (isSelected)
        {
            indicator.Child = new TextBlock
            {
                Text = "\uE73E",
                FontFamily = (WpfFontFamily)findResource("FluentIconFont"),
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        var header = new StackPanel
        {
            Orientation = WpfOrientation.Horizontal,
            Children =
            {
                indicator,
                CreateIcon(findResource, glyph, 18),
                new TextBlock
                {
                    Text = title,
                    Margin = new Thickness(10, 0, 0, 0),
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                }
            }
        };

        return new StackPanel
        {
            Children =
            {
                header,
                new TextBlock
                {
                    Text = description,
                    Foreground = (WpfBrush)findResource("MutedText"),
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(28, 8, 0, 0)
                }
            }
        };
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace IPhoneCopier.Views;

public class ConfirmDialog : Window
{
    public ConfirmDialog(string message)
    {
        Title = "Bevestigen";
        Width = 460;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;

        var yes = new Button { Content = "Ja, uitvoeren", IsDefault = true, MinWidth = 110 };
        var no = new Button { Content = "Annuleren", IsCancel = true, MinWidth = 90 };
        yes.Click += (_, _) => Close(true);
        no.Click += (_, _) => Close(false);

        Content = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 20,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, FontSize = 14 },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Right,
                    Children = { yes, no }
                }
            }
        };
    }
}

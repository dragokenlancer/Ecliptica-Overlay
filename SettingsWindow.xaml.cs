using System.Windows;
using System.Windows.Controls;
using EclipticaOverlay.Services;

namespace EclipticaOverlay;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        EnabledCheckBox.IsChecked = settings.ChatboxEnabled;
        NotifyCheckBox.IsChecked = settings.ChatboxNotifySound;
        IntervalBox.Text = settings.ChatboxIntervalSeconds.ToString("0.0");
        LobbyTemplateBox.Text = settings.LobbyTemplate;
        StageTemplateBox.Text = settings.StageTemplate;
        BossTemplateBox.Text = settings.BossTemplate;
        IntermissionTemplateBox.Text = settings.IntermissionTemplate;

        foreach (var (key, description) in ChatboxMessageBuilder.AvailableKeys)
        {
            KeysList.Items.Add(new TextBlock
            {
                Text = $"{key} — {description}",
                Foreground = System.Windows.Media.Brushes.Gainsboro,
                FontSize = 10.5,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 3)
            });
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _settings.ChatboxEnabled = EnabledCheckBox.IsChecked == true;
        _settings.ChatboxNotifySound = NotifyCheckBox.IsChecked == true;
        _settings.ChatboxIntervalSeconds = double.TryParse(IntervalBox.Text, out var seconds)
            ? Math.Max(0.5, seconds)
            : 1.5;
        _settings.LobbyTemplate = LobbyTemplateBox.Text;
        _settings.StageTemplate = StageTemplateBox.Text;
        _settings.BossTemplate = BossTemplateBox.Text;
        _settings.IntermissionTemplate = IntermissionTemplateBox.Text;

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void ResetDefaults_Click(object sender, RoutedEventArgs e)
    {
        var defaults = new AppSettings();
        LobbyTemplateBox.Text = defaults.LobbyTemplate;
        StageTemplateBox.Text = defaults.StageTemplate;
        BossTemplateBox.Text = defaults.BossTemplate;
        IntermissionTemplateBox.Text = defaults.IntermissionTemplate;
    }
}

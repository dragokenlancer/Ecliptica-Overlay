using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using EclipticaOverlay.Models;
using EclipticaOverlay.Services;

namespace EclipticaOverlay;

public partial class MainWindow : Window
{
    private static readonly Brush ConnectedBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0xC3, 0x8A));
    private static readonly Brush DisconnectedBrush = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));

    private static readonly Brush LobbyBrush = new SolidColorBrush(Color.FromRgb(0x7F, 0xB2, 0xE5));
    private static readonly Brush StageBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0xC3, 0x8A));
    private static readonly Brush BossBrush = new SolidColorBrush(Color.FromRgb(0xE5, 0x55, 0x5F));
    private static readonly Brush IntermissionBrush = new SolidColorBrush(Color.FromRgb(0xE5, 0xA9, 0x3C));
    private static readonly Brush IdleBrush = new SolidColorBrush(Color.FromRgb(0xAA, 0xB0, 0xC0));

    private static readonly TimeSpan KillToastLifetime = TimeSpan.FromSeconds(15);

    private readonly LogWatcherService _watcher;
    private readonly DispatcherTimer _refreshTimer;
    private readonly AppSettings _settings;
    private readonly ChatboxController _chatbox;

    public MainWindow(LogWatcherService watcher)
    {
        InitializeComponent();
        _watcher = watcher;

        _settings = AppSettings.Load();
        _chatbox = new ChatboxController { Enabled = _settings.ChatboxEnabled };
        UpdateChatboxToggleVisual();

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _refreshTimer.Tick += (_, _) => Refresh(_watcher.GetSnapshot());
        _refreshTimer.Start();
    }

    private void Refresh(MatchState state)
    {
        ConnectionDot.Fill = state.LogConnected ? ConnectedBrush : DisconnectedBrush;

        if (!state.LogConnected)
        {
            StatusText.Text = "NO LOG FOUND";
            StatusText.Foreground = DisconnectedBrush;
            DetailText.Text = "Waiting for VRChat...";
            ProgressRow.Visibility = Visibility.Collapsed;
            BossInfoBox.Visibility = Visibility.Collapsed;
            AggroBox.Visibility = Visibility.Collapsed;
            OtherAggroText.Visibility = Visibility.Collapsed;
            KillToast.Visibility = Visibility.Collapsed;
            ClassText.Text = "";
            ElapsedText.Text = "";
            SessionText.Text = "";
            return;
        }

        switch (state.Status)
        {
            case RunStatus.Lobby:
                StatusText.Text = "LOBBY";
                StatusText.Foreground = LobbyBrush;
                DetailText.Text = "";
                ProgressRow.Visibility = Visibility.Collapsed;
                break;

            case RunStatus.Stage:
                StatusText.Text = "IN STAGE";
                StatusText.Foreground = StageBrush;
                DetailText.Text = state.StageName ?? "";
                ProgressRow.Visibility = Visibility.Visible;
                ProgressFill.Background = StageBrush;
                break;

            case RunStatus.BossFight:
                StatusText.Text = "BOSS FIGHT";
                StatusText.Foreground = BossBrush;
                DetailText.Text = state.BossName ?? "";
                ProgressRow.Visibility = Visibility.Visible;
                ProgressFill.Background = BossBrush;
                break;

            case RunStatus.Intermission:
                StatusText.Text = "INTERMISSION";
                StatusText.Foreground = IntermissionBrush;
                DetailText.Text = "";
                ProgressRow.Visibility = Visibility.Collapsed;
                break;

            default:
                StatusText.Text = "WAITING...";
                StatusText.Foreground = IdleBrush;
                DetailText.Text = "";
                ProgressRow.Visibility = Visibility.Collapsed;
                break;
        }

        if (ProgressRow.Visibility == Visibility.Visible && state.PhaseProgress is { } phase)
        {
            var percent = Math.Clamp(phase * 100.0, 0, 100);
            ProgressLabel.Text = $"{DifficultyTier.Name(phase)} · {percent:0}%";
            ProgressFill.Width = ProgressTrack.ActualWidth * (percent / 100.0);
        }

        RefreshBossInfo(state);
        RefreshAggro(state);
        RefreshKillToast(state);
        _chatbox.Update(state, _settings);

        var dealt = state.DamageDealtStrike + state.DamageDealtNonStrike;
        DealtText.Text = dealt.ToString("N0");
        DealtBreakdownText.Text = state.DamageDealtNonStrike > 0
            ? $"S {state.DamageDealtStrike:N0} · N {state.DamageDealtNonStrike:N0}"
            : "";
        TakenText.Text = state.DamageTakenTotal.ToString("N0");

        LastHitText.Text = state.LastHitAmount is { } hitAmt
            ? $"Last hit: -{hitAmt} ({(state.LastHitSource is { Length: > 0 } src ? src : "unknown")})"
            : "";

        ClassText.Text = state.PlayerClass is { Length: > 0 } cls ? $"Class: {cls}" : "";

        if (state.SegmentStartedAt is { } startedAt)
        {
            var elapsed = DateTime.Now - startedAt;
            if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;
            ElapsedText.Text = elapsed.TotalHours >= 1
                ? elapsed.ToString(@"h\:mm\:ss")
                : elapsed.ToString(@"mm\:ss");
        }
        else
        {
            ElapsedText.Text = "";
        }

        SessionText.Text = state.SessionId is { Length: > 0 } sid ? $"#{sid}" : "";
    }

    private void RefreshBossInfo(MatchState state)
    {
        if (state.Status == RunStatus.BossFight
            && BossReference.TryGet(state.BossName, out var info))
        {
            BossTitleText.Text = info.Title;
            BossStatsText.Text = $"{info.DamageType} · {info.Phases} phase{(info.Phases > 1 ? "s" : "")}";

            BossAffinityText.Inlines.Clear();
            if (info.WeakTo is { Length: > 0 } weak)
            {
                BossAffinityText.Inlines.Add(new Run("Weak: ") { Foreground = new SolidColorBrush(Color.FromRgb(0x6A, 0x70, 0x80)) });
                BossAffinityText.Inlines.Add(new Run(weak) { Foreground = new SolidColorBrush(Color.FromRgb(0xE5, 0x78, 0x7F)) });
            }
            if (info.ResistTo is { Length: > 0 } resist)
            {
                if (info.WeakTo is { Length: > 0 })
                    BossAffinityText.Inlines.Add(new Run("   "));
                BossAffinityText.Inlines.Add(new Run("Resist: ") { Foreground = new SolidColorBrush(Color.FromRgb(0x6A, 0x70, 0x80)) });
                BossAffinityText.Inlines.Add(new Run(resist) { Foreground = new SolidColorBrush(Color.FromRgb(0x7F, 0xB2, 0xE5)) });
            }
            BossAffinityText.Visibility = info.WeakTo is { Length: > 0 } || info.ResistTo is { Length: > 0 }
                ? Visibility.Visible : Visibility.Collapsed;

            BossPhaseTriggerText.Text = info.PhaseTrigger is { Length: > 0 } trigger ? $"⚠ {trigger}" : "";
            BossPhaseTriggerText.Visibility = info.PhaseTrigger is { Length: > 0 } ? Visibility.Visible : Visibility.Collapsed;
            BossStrategyText.Text = info.Strategy;
            BossInfoBox.Visibility = Visibility.Visible;
        }
        else
        {
            BossInfoBox.Visibility = Visibility.Collapsed;
        }
    }

    private void RefreshAggro(MatchState state)
    {
        if (state.Status == RunStatus.BossFight
            && state.BossName is { Length: > 0 } boss
            && state.EnemyAggro.TryGetValue(boss, out var bossAggro))
        {
            var held = DateTime.Now - bossAggro.Since;
            var heldSecs = Math.Max(0, (int)held.TotalSeconds);
            AggroText.Text = $"{bossAggro.Player} ({heldSecs}s)";
            AggroBox.Visibility = Visibility.Visible;
        }
        else
        {
            AggroBox.Visibility = Visibility.Collapsed;
        }

        var others = state.EnemyAggro
            .Where(kv => kv.Key != state.BossName)
            .OrderByDescending(kv => kv.Value.Since)
            .Take(6)
            .Select(kv => $"{kv.Key} → {kv.Value.Player}")
            .ToArray();

        if (others.Length > 0)
        {
            OtherAggroText.Text = string.Join("   ", others);
            OtherAggroText.Visibility = Visibility.Visible;
        }
        else
        {
            OtherAggroText.Visibility = Visibility.Collapsed;
        }
    }

    private void RefreshKillToast(MatchState state)
    {
        if (state.LastDefeatedAt is { } defeatedAt && DateTime.Now - defeatedAt < KillToastLifetime)
        {
            var total = (state.LastDefeatedStrikeDmg ?? 0) + (state.LastDefeatedNonStrikeDmg ?? 0);
            KillToastText.Text = $"✓ {state.LastDefeatedBoss} defeated — you dealt {total:N0} dmg";
            KillToast.Visibility = Visibility.Visible;
        }
        else
        {
            KillToast.Visibility = Visibility.Collapsed;
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void CloseButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Handle (and stop the event bubbling to the window) before it reaches
        // Window_MouseLeftButtonDown, which would otherwise swallow the click into DragMove().
        e.Handled = true;
        _chatbox.Dispose();
        Application.Current.Shutdown();
    }

    private void ChatboxToggle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _settings.ChatboxEnabled = !_settings.ChatboxEnabled;
        _settings.Save();
        _chatbox.Enabled = _settings.ChatboxEnabled;
        UpdateChatboxToggleVisual();
    }

    private void SettingsButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var window = new SettingsWindow(_settings) { Owner = this };
        if (window.ShowDialog() == true)
        {
            _settings.Save();
            _chatbox.Enabled = _settings.ChatboxEnabled;
            UpdateChatboxToggleVisual();
        }
    }

    private void UpdateChatboxToggleVisual()
    {
        ChatboxToggle.Foreground = _settings.ChatboxEnabled ? ConnectedBrush : DisconnectedBrush;
    }
}

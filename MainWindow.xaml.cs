using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace RedLight;

public partial class MainWindow : Window
{
    private DispatcherTimer? _sliderTimer;
    private bool _loadingSettings = true;

    public MainWindow()
    {
        InitializeComponent();
        InitializeSliderTimer();

        var preferences = UserPreferences.Load();
        EnableFilterCheckBox.IsChecked = preferences.Enabled;
        WarmthSlider.Value = preferences.WarmthPosition;
        DimSlider.Value = preferences.DimPosition;
        StartupCheckBox.IsChecked = UserPreferences.IsStartupEnabled();
        _loadingSettings = false;
        UpdateLabels();

        Deactivated += (_, _) => HidePopup();
        ApplyCurrentFilter();
    }

    public void ShowPopup()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 12;
        Top = workArea.Bottom - Height - 24;

        Opacity = 0;
        Show();
        Activate();

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
        var slideUp = new DoubleAnimation(Top + 10, Top, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = new QuadraticEase()
        };
        BeginAnimation(OpacityProperty, fadeIn);
        BeginAnimation(TopProperty, slideUp);
    }

    public void HidePopup()
    {
        Hide();
    }

    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
    {
        // The dim row controls dimming; the rest of the popup controls warmth.
        Slider target = DimRow.IsMouseOver ? DimSlider : WarmthSlider;
        target.Value = Math.Clamp(target.Value + e.Delta / 60.0, target.Minimum, target.Maximum);
        e.Handled = true;
        base.OnPreviewMouseWheel(e);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        HidePopup();
    }

    private void InitializeSliderTimer()
    {
        _sliderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _sliderTimer.Tick += (s, e) =>
        {
            _sliderTimer.Stop();
            SavePreferences();
        };
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loadingSettings || _sliderTimer == null) return;
        UpdateLabels();
        ApplyCurrentFilter();
        _sliderTimer.Stop();
        _sliderTimer.Start();
    }

    private void UpdateLabels()
    {
        WarmthValueText.Text = $"≈{WarmthCurve.Kelvin(WarmthSlider.Value):0}K";
        DimValueText.Text = $"{DimSlider.Value:0}%";
    }

    private void ApplyFilter(object? sender, RoutedEventArgs? e)
    {
        if (_loadingSettings) return;
        _sliderTimer?.Stop();
        UpdateLabels();
        ApplyCurrentFilter();
        SavePreferences();
    }

    private void SavePreferences()
    {
        UserPreferences.Save(
            EnableFilterCheckBox.IsChecked == true,
            WarmthSlider.Value,
            DimSlider.Value);
    }

    private void StartupCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_loadingSettings) return;

        try
        {
            UserPreferences.SetStartupEnabled(StartupCheckBox.IsChecked == true);
        }
        catch (Exception ex)
        {
            _loadingSettings = true;
            StartupCheckBox.IsChecked = UserPreferences.IsStartupEnabled();
            _loadingSettings = false;
            System.Windows.MessageBox.Show($"Could not change startup setting: {ex.Message}",
                "Red Light Compact", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ApplyCurrentFilter()
    {
        if (EnableFilterCheckBox == null || WarmthSlider == null || DimSlider == null)
            return;

        if (EnableFilterCheckBox.IsChecked != true)
        {
            NativeMethods.ResetColorEffect();
            return;
        }

        var (green, blue) = WarmthCurve.Channels(WarmthSlider.Value);
        float brightness = 1 - (float)(DimSlider.Value / 100);
        float[] matrix = GetWarmDimMatrix(brightness, green, blue);
        var effect = new NativeMethods.MagColorEffect(matrix);
        NativeMethods.MagSetFullscreenColorEffect(ref effect);
    }

    private void QuitButton_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }

    // Dim and warmth are applied together as one full-screen color matrix.
    private static float[] GetWarmDimMatrix(float brightness, float green, float blue) =>
    [
        brightness, 0,                  0,                 0, 0,
        0,          brightness * green, 0,                 0, 0,
        0,          0,                  brightness * blue, 0, 0,
        0,          0,                  0,                 1, 0,
        0,          0,                  0,                 0, 1
    ];
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace genpiyi
{
    public partial class MainWindow : Window
    {
        private bool _loading = true; // chặn sự kiện phát sinh trong InitializeComponent
        private readonly DispatcherTimer _previewTimer;
        private string _plain = "";
        private bool _recordingHotkey;

        private static readonly Brush OkBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x1E, 0x9E, 0x5A)));
        private static readonly Brush ErrBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xD1, 0x3C, 0x40)));

        private static Brush Freeze(Brush b) { b.Freeze(); return b; }

        public MainWindow()
        {
            InitializeComponent();
            var logo = IconFactory.GetImageSource();
            Icon = logo;
            LogoImage.Source = logo;
            VersionText.Text = "GenPiYi v" + (typeof(App).Assembly.GetName().Version?.ToString(3) ?? "1.0");

            _previewTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
            _previewTimer.Tick += (_, _) => { _previewTimer.Stop(); UpdatePreview(); };

            SourceInitialized += (_, _) =>
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                NativeMethods.TryRoundCorners(hwnd);
                if (NativeMethods.IsWindows11) RootBorder.BorderThickness = new Thickness(0); // Win11 tự vẽ viền bo góc
            };
            StateChanged += (_, _) =>
            {
                // Cửa sổ không viền khi phóng to bị tràn ~7px ra ngoài màn hình → bù lại
                RootBorder.Padding = WindowState == WindowState.Maximized ? new Thickness(7) : new Thickness(0);
            };

            InputBox.Text = "你好！今天晚上我们去哪里吃饭？\n我还没想好，你决定吧。";
            ReloadSettings();
            StartScan();
        }

        /// <summary>Đóng cửa sổ = ẩn xuống khay hệ thống, app vẫn chạy.</summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        // ---------------- Ngôn ngữ ----------------

        private void BtnLang_Click(object sender, RoutedEventArgs e) =>
            App.Current.SetLanguage(Loc.IsEn ? "vi" : "en");

        private void Language_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            App.Current.SetLanguage(RbEn.IsChecked == true ? "en" : "vi");
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void BtnClose_Click(object sender, RoutedEventArgs e) => Hide();

        private void Nav_Checked(object sender, RoutedEventArgs e)
        {
            if (PageTry == null || PageSettings == null || PageGuide == null || PageTheme == null) return;
            PageTry.Visibility = NavTry.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            PageTheme.Visibility = NavTheme.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            PageSettings.Visibility = NavSettings.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            PageGuide.Visibility = NavGuide.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        // ------------------------------------------------------------------

        public void ReloadSettings()
        {
            _loading = true;
            var s = App.Current.Settings;
            ChkAuto.IsChecked = s.AutoOnCopy;
            ChkChatOnly.IsChecked = s.OnlyChatApps;
            RbMark.IsChecked = s.ToneStyle != "number";
            RbNumber.IsChecked = s.ToneStyle == "number";
            CurrentThemeName.Text = s.GetTheme().LocName;
            RbVi.IsChecked = !Loc.IsEn;
            RbEn.IsChecked = Loc.IsEn;
            LangLabel.Text = Loc.IsEn ? "EN" : "VI";
            ChkToneColors.IsChecked = s.ToneColors;
            ChkVocab.IsChecked = s.ShowVocab;
            ChkHanViet.IsChecked = s.ShowHanViet;
            var ml = s.ResolvedMeaningLang;
            RbMeanVi.IsChecked = ml == "vi";
            RbMeanEn.IsChecked = ml == "en";
            RbMeanBoth.IsChecked = ml == "both";
            ChkStartup.IsChecked = s.StartWithWindows;
            FontSlider.Value = s.HanziFontSize;
            FontLabel.Text = $"{s.HanziFontSize:0}";
            if (!_recordingHotkey) HotkeyBox.Text = DisplayHotkey(s.Hotkey);
            GuideHotkey.Text = DisplayHotkey(s.Hotkey);
            _loading = false;

            UpdateEnabledStates();
            RenderChatApps();
            RenderToneLegend();
            RenderThemes();
            ShowHotkeyStatus();
            UpdateEngineStatus();
            UpdatePreview();
        }

        private void UpdateEnabledStates()
        {
            var s = App.Current.Settings;
            ChkChatOnly.IsEnabled = s.AutoOnCopy;
            bool appsActive = s.AutoOnCopy && s.OnlyChatApps;
            ChatAppsArea.IsEnabled = appsActive;
            ChatAppsArea.Opacity = appsActive ? 1 : 0.45;

            bool dict = DictionaryService.Available;
            ChkVocab.IsEnabled = dict;
            DictOptions.IsEnabled = dict;
            DictOptions.Opacity = dict ? 1 : 0.45;
            DictStatus.Text = dict ? Loc.F("set.dictCredit", DictionaryService.EntryCount) : Loc.T("set.dictMissing");
            DictStatus.Foreground = dict ? (Brush)FindResource("TextMutedBrush") : ErrBrush;
        }

        private void Setting_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            var s = App.Current.Settings;
            s.AutoOnCopy = ChkAuto.IsChecked == true;
            s.OnlyChatApps = ChkChatOnly.IsChecked == true;
            s.ToneColors = ChkToneColors.IsChecked == true;
            s.ToneStyle = RbNumber.IsChecked == true ? "number" : "mark";
            s.ShowVocab = ChkVocab.IsChecked == true;
            s.ShowHanViet = ChkHanViet.IsChecked == true;
            if (RbMeanVi.IsChecked == true || RbMeanEn.IsChecked == true || RbMeanBoth.IsChecked == true)
            {
                var ml = RbMeanEn.IsChecked == true ? "en" : RbMeanBoth.IsChecked == true ? "both" : "vi";
                // Giữ "auto" nếu người dùng chưa đổi (nghĩa đi theo ngôn ngữ app)
                if (ml != s.ResolvedMeaningLang) s.MeaningLang = ml;
            }

            bool startup = ChkStartup.IsChecked == true;
            if (startup != s.StartWithWindows)
            {
                s.StartWithWindows = startup; // hiện ngay, kết quả thật cập nhật sau
                ApplyStartupAsync(startup);
            }

            App.Current.SettingsChanged(); // lưu + đồng bộ menu khay + nạp lại cửa sổ
        }

        /// <summary>Bật/tắt khởi động cùng Windows (StartupTask khi chạy bản Store, registry khi chạy .exe).</summary>
        private async void ApplyStartupAsync(bool enable)
        {
            ChkStartup.IsEnabled = false;
            var (enabled, message) = await StartupHelper.SetEnabledAsync(enable);
            ChkStartup.IsEnabled = true;
            App.Current.Settings.StartWithWindows = enabled;
            App.Current.SettingsChanged();
            StartupStatus.Text = message ?? "";
            StartupStatus.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
        }

        private void FontSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (FontLabel != null) FontLabel.Text = $"{e.NewValue:0}";
            if (_loading) return;
            App.Current.Settings.HanziFontSize = e.NewValue;
            App.Current.Settings.Save();
            UpdatePreview();
        }

        // ---------------- Danh sách app chat: quét máy + bật/tắt ----------------

        private List<DetectedChatApp>? _apps;
        private bool _scanning;

        /// <summary>Quét app chat trên máy ở luồng nền rồi vẽ lại danh sách.</summary>
        private async void StartScan()
        {
            if (_scanning) return;
            _scanning = true;
            BtnScan.IsEnabled = false;
            ScanStatus.Text = Loc.T("set.scanning");
            var s = App.Current.Settings;
            var enabled = s.ChatApps.ToList();
            var custom = s.CustomApps.ToList();
            try
            {
                _apps = await Task.Run(() => ChatAppScanner.Scan(enabled, custom));
            }
            catch (Exception ex)
            {
                App.Log(ex);
                _apps = new List<DetectedChatApp>();
            }
            finally
            {
                _scanning = false;
                BtnScan.IsEnabled = true;
            }
            RenderChatApps();
            if (OthersArea.Visibility == Visibility.Visible) await LoadOtherApps();
        }

        private void BtnScanApps_Click(object sender, RoutedEventArgs e) => StartScan();

        private static bool IsAppEnabled(DetectedChatApp app) =>
            app.Processes.Any(p => App.Current.Settings.ChatApps.Contains(p, StringComparer.OrdinalIgnoreCase));

        private void SetAppEnabled(DetectedChatApp app, bool on)
        {
            var list = App.Current.Settings.ChatApps;
            list.RemoveAll(a => app.Processes.Contains(a, StringComparer.OrdinalIgnoreCase));
            if (on) list.AddRange(app.Processes);
            App.Current.SettingsChanged();
        }

        private void RenderChatApps()
        {
            if (_apps == null) return; // đang quét lần đầu
            AppRowsPanel.Children.Clear();

            int found = _apps.Count(a => a.Installed || a.Running);
            int on = _apps.Count(IsAppEnabled);
            ScanStatus.Text = found == 0
                ? Loc.T("scan.none")
                : Loc.F("scan.found", found, on);

            if (_apps.Count == 0)
            {
                AppRowsPanel.Children.Add(new TextBlock
                {
                    Text = Loc.T("scan.empty"),
                    Margin = new Thickness(14, 12, 14, 12),
                    Foreground = (Brush)FindResource("TextMutedBrush")
                });
                return;
            }

            for (int i = 0; i < _apps.Count; i++)
            {
                var row = BuildAppRow(_apps[i]);
                if (i < _apps.Count - 1)
                {
                    row.BorderBrush = (Brush)FindResource("DividerBrush");
                    row.BorderThickness = new Thickness(0, 0, 0, 1);
                }
                AppRowsPanel.Children.Add(row);
            }
        }

        private FrameworkElement AppIcon(ImageSource? icon, string name, string color, double size = 26)
        {
            if (icon != null)
                return new Image { Source = icon, Width = size, Height = size, VerticalAlignment = VerticalAlignment.Center };

            // Không lấy được icon → ô màu có chữ cái đầu
            return new Border
            {
                Width = size, Height = size, CornerRadius = new CornerRadius(size * 0.25),
                Background = ThemeCatalog.B(color),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = string.IsNullOrEmpty(name) ? "?" : name.Substring(0, 1).ToUpperInvariant(),
                    Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = size * 0.5,
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
                }
            };
        }

        private Border BuildAppRow(DetectedChatApp app)
        {
            bool enabled = IsAppEnabled(app);

            var grid = new Grid { Margin = new Thickness(12, 9, 12, 9) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var icon = AppIcon(app.Icon, app.Name, app.Color);
            Grid.SetColumn(icon, 0);
            grid.Children.Add(icon);

            // Trạng thái: đang chạy / đã cài / không tìm thấy
            string statusText;
            Brush dot;
            if (app.Running) { statusText = Loc.T("app.running"); dot = OkBrush; }
            else if (app.Installed) { statusText = Loc.T("app.installed"); dot = (Brush)FindResource("TextMutedBrush"); }
            else { statusText = Loc.T("app.notFound"); dot = ThemeCatalog.B("#D5D9E0"); }
            if (app.IsCustom) statusText += Loc.T("app.custom");

            var info = new StackPanel { Margin = new Thickness(12, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center };
            info.Children.Add(new TextBlock { Text = app.Name, FontSize = 13.5, Foreground = (Brush)FindResource("TextBrush") });
            info.Children.Add(new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 2, 0, 0),
                Children =
                {
                    new Ellipse { Width = 6, Height = 6, Fill = dot, VerticalAlignment = VerticalAlignment.Center },
                    new TextBlock { Text = statusText, FontSize = 11.5, Margin = new Thickness(6, 0, 0, 0),
                                    Foreground = (Brush)FindResource("TextSubBrush") }
                }
            });
            info.ToolTip = Loc.T("app.processes") + string.Join(", ", app.Processes.Select(p => p + ".exe"))
                           + (string.IsNullOrEmpty(app.ExePath) ? "" : "\n" + app.ExePath);
            Grid.SetColumn(info, 1);
            grid.Children.Add(info);

            if (app.IsCustom)
            {
                var remove = new Button
                {
                    Content = "\uE74D",
                    FontFamily = (FontFamily)FindResource("IconFont"),
                    FontSize = 12,
                    Style = (Style)FindResource("GhostButton"),
                    ToolTip = Loc.T("app.remove"),
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                remove.Click += (_, _) =>
                {
                    var s = App.Current.Settings;
                    s.CustomApps.RemoveAll(a => app.Processes.Contains(a, StringComparer.OrdinalIgnoreCase));
                    s.ChatApps.RemoveAll(a => app.Processes.Contains(a, StringComparer.OrdinalIgnoreCase));
                    _apps?.Remove(app);
                    App.Current.SettingsChanged();
                };
                Grid.SetColumn(remove, 2);
                grid.Children.Add(remove);
            }

            var toggle = new CheckBox
            {
                Style = (Style)FindResource("ToggleSwitch"),
                IsChecked = enabled,
                ToolTip = enabled ? Loc.T("app.onTip") : Loc.T("app.offTip")
            };
            toggle.Checked += (_, _) => SetAppEnabled(app, true);
            toggle.Unchecked += (_, _) => SetAppEnabled(app, false);
            Grid.SetColumn(toggle, 3);
            grid.Children.Add(toggle);

            return new Border { Child = grid, Background = Brushes.Transparent };
        }

        // ---------------- Thêm app khác đang mở ----------------

        private async void BtnShowOthers_Click(object sender, RoutedEventArgs e)
        {
            bool show = OthersArea.Visibility != Visibility.Visible;
            OthersArea.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            ShowOthersIcon.Text = show ? "\uE738" : "\uE710";
            if (show) await LoadOtherApps();
        }

        private async Task LoadOtherApps()
        {
            OtherAppsPanel.Children.Clear();
            OtherAppsPanel.Children.Add(new TextBlock
            {
                Text = Loc.T("others.loading"),
                Margin = new Thickness(14, 10, 14, 10),
                Foreground = (Brush)FindResource("TextMutedBrush")
            });

            var exclude = (_apps ?? new List<DetectedChatApp>()).SelectMany(a => a.Processes).ToList();
            List<RunningWindowApp> list;
            try { list = await Task.Run(() => ChatAppScanner.ScanRunningWindows(exclude)); }
            catch (Exception ex) { App.Log(ex); list = new List<RunningWindowApp>(); }

            OtherAppsPanel.Children.Clear();
            if (list.Count == 0)
            {
                OtherAppsPanel.Children.Add(new TextBlock
                {
                    Text = Loc.T("others.none"),
                    Margin = new Thickness(14, 10, 14, 10),
                    Foreground = (Brush)FindResource("TextMutedBrush")
                });
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                var w = list[i];
                var grid = new Grid { Margin = new Thickness(12, 7, 10, 7) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var icon = AppIcon(w.Icon, w.Process, "#8A94A3", 22);
                grid.Children.Add(icon);

                var info = new StackPanel { Margin = new Thickness(10, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
                info.Children.Add(new TextBlock { Text = w.Process, FontSize = 13 });
                info.Children.Add(new TextBlock
                {
                    Text = w.Title, FontSize = 11.5, Foreground = (Brush)FindResource("TextMutedBrush"),
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                Grid.SetColumn(info, 1);
                grid.Children.Add(info);

                var add = new Button { Content = Loc.T("common.add"), Style = (Style)FindResource("SecondaryButton"), Padding = new Thickness(12, 3, 12, 4), VerticalAlignment = VerticalAlignment.Center };
                add.Click += (_, _) => AddCustomApps(new[] { w.Process });
                Grid.SetColumn(add, 2);
                grid.Children.Add(add);

                var row = new Border { Child = grid };
                if (i < list.Count - 1)
                {
                    row.BorderBrush = (Brush)FindResource("DividerBrush");
                    row.BorderThickness = new Thickness(0, 0, 0, 1);
                }
                OtherAppsPanel.Children.Add(row);
            }
        }

        private void BtnAddApp_Click(object sender, RoutedEventArgs e) => AddAppsFromBox();

        private void NewAppBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { AddAppsFromBox(); e.Handled = true; }
        }

        private void AddAppsFromBox()
        {
            var names = NewAppBox.Text
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Select(x => x.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? x[..^4] : x)
                .Where(x => x.Length > 0)
                .ToList();
            NewAppBox.Text = "";
            AddCustomApps(names);
        }

        /// <summary>Thêm + bật app. Tên thuộc app đã biết (vd "LINE") thì bật cả nhóm tiến trình của app đó.</summary>
        private void AddCustomApps(IEnumerable<string> names)
        {
            var s = App.Current.Settings;
            bool changed = false;
            foreach (var n in names.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var known = ChatAppScanner.Known.FirstOrDefault(k => k.Processes.Contains(n, StringComparer.OrdinalIgnoreCase));
                var procs = known?.Processes ?? new[] { n };
                if (known == null && !s.CustomApps.Contains(n, StringComparer.OrdinalIgnoreCase)) s.CustomApps.Add(n);
                foreach (var p in procs)
                    if (!s.ChatApps.Contains(p, StringComparer.OrdinalIgnoreCase)) s.ChatApps.Add(p);
                changed = true;
            }
            if (!changed) return;
            App.Current.SettingsChanged();
            StartScan(); // quét lại để app mới hiện kèm icon
        }

        // ---------------- Chú thích màu thanh điệu ----------------

        private void RenderToneLegend()
        {
            ToneLegend.Children.Clear();
            string[] labels = { "", "1 ā", "2 á", "3 ǎ", "4 à", Loc.T("tone.neutral") };
            for (int t = 1; t <= 5; t++)
            {
                var brush = RubyBuilder.ToneBrush(t, ThemeCatalog.Get("light"));
                ToneLegend.Children.Add(new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 12, 0),
                    Opacity = App.Current.Settings.ToneColors ? 1 : 0.4,
                    Children =
                    {
                        new Ellipse { Width = 8, Height = 8, Fill = brush, VerticalAlignment = VerticalAlignment.Center },
                        new TextBlock { Text = labels[t], FontSize = 12, Margin = new Thickness(5, 0, 0, 0), Foreground = brush }
                    }
                });
            }
        }

        // ---------------- Ghi phím tắt ----------------

        private static string DisplayHotkey(string hk) =>
            string.Join(" + ", hk.Split('+', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()));

        private void HotkeyBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            _recordingHotkey = true;
            App.Current.SuspendHotkey(); // để bấm được chính tổ hợp phím đang dùng
            HotkeyBox.Text = Loc.T("hotkey.press");
            HotkeyStatus.Foreground = (Brush)FindResource("TextSubBrush");
            HotkeyStatus.Text = Loc.T("hotkey.escCancel");
        }

        private void HotkeyBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (!_recordingHotkey) return;
            _recordingHotkey = false;
            App.Current.ApplyHotkey();
            HotkeyBox.Text = DisplayHotkey(App.Current.Settings.Hotkey);
            ShowHotkeyStatus();
        }

        private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            if (key == Key.Escape || key == Key.Tab)
            {
                FinishRecording();
                return;
            }

            var mods = new List<string>();
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) mods.Add("Ctrl");
            if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0) mods.Add("Alt");
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) mods.Add("Shift");
            if ((Keyboard.Modifiers & ModifierKeys.Windows) != 0) mods.Add("Win");

            bool isModifierKey = key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
                                  or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin;
            if (isModifierKey)
            {
                HotkeyBox.Text = string.Join(" + ", mods) + " + …";
                return;
            }

            string keyName = new KeyConverter().ConvertToInvariantString(key) ?? key.ToString();
            string hk = string.Join("+", mods.Append(keyName));

            var s = App.Current.Settings;
            var old = s.Hotkey;
            s.Hotkey = hk;
            if (App.Current.ApplyHotkey())
            {
                s.Save();
                _recordingHotkey = false;
                HotkeyBox.Text = DisplayHotkey(hk);
                GuideHotkey.Text = DisplayHotkey(hk);
                ShowHotkeyStatus();
                Keyboard.ClearFocus();
            }
            else
            {
                var err = App.Current.HotkeyError;
                s.Hotkey = old;
                App.Current.SuspendHotkey();
                HotkeyBox.Text = Loc.T("hotkey.press");
                HotkeyStatus.Foreground = ErrBrush;
                HotkeyStatus.Text = "✗ " + DisplayHotkey(hk) + ": " + err;
            }
        }

        private void FinishRecording()
        {
            Keyboard.ClearFocus(); // → LostKeyboardFocus khôi phục phím cũ
        }

        private void ShowHotkeyStatus()
        {
            var err = App.Current.HotkeyError;
            if (string.IsNullOrEmpty(err))
            {
                HotkeyStatus.Foreground = OkBrush;
                HotkeyStatus.Text = Loc.T("hotkey.active");
            }
            else
            {
                HotkeyStatus.Foreground = ErrBrush;
                HotkeyStatus.Text = "✗ " + err;
            }
        }

        // ---------------- Trạng thái bộ chuyển pinyin ----------------

        private void UpdateEngineStatus()
        {
            if (PinyinService.Available)
            {
                EnginePill.Background = new SolidColorBrush(Color.FromRgb(0xEA, 0xF7, 0xEF));
                EngineDot.Fill = OkBrush;
                EngineText.Foreground = new SolidColorBrush(Color.FromRgb(0x17, 0x69, 0x3D));
                EngineText.Text = Loc.T("engine.ok");
                EnginePill.ToolTip = Loc.T("engine.okTip");
            }
            else
            {
                EnginePill.Background = new SolidColorBrush(Color.FromRgb(0xFD, 0xEC, 0xEC));
                EngineDot.Fill = ErrBrush;
                EngineText.Foreground = ErrBrush;
                EngineText.Text = Loc.T("engine.fail");
                EnginePill.ToolTip = (PinyinService.LoadError ?? "") + "\n" + Loc.T("engine.failTip");
            }
        }

        // ---------------- Trang Giao diện (mẫu popup) ----------------

        private void BtnGoTheme_Click(object sender, RoutedEventArgs e) => NavTheme.IsChecked = true;

        private string SampleText() =>
            string.IsNullOrWhiteSpace(InputBox.Text) ? "你好！很高兴认识你。" : InputBox.Text.Trim();

        private void RenderThemes()
        {
            FeaturedPanel.Children.Clear();
            BasicPanel.Children.Clear();
            var current = App.Current.Settings.GetTheme().Id;
            foreach (var t in ThemeCatalog.All)
            {
                var card = BuildThemeCard(t, t.Id == current);
                if (t.Id == "dark" || t.Id == "light") BasicPanel.Children.Add(card);
                else FeaturedPanel.Children.Add(card);
            }
        }

        private FrameworkElement BuildThemeCard(PopupTheme t, bool active)
        {
            const double w = 196, previewH = 124;
            var accent = ThemeCatalog.B(t.Accent);

            // --- Ảnh xem trước (bo 2 góc trên) ---
            var preview = new Grid { Height = previewH, Cursor = Cursors.Hand, ToolTip = Loc.T("theme.previewTip") };
            preview.Children.Add(ThemeCatalog.BuildPreview(t));
            var clip = new CombinedGeometry(GeometryCombineMode.Union,
                new RectangleGeometry(new Rect(0, 0, w - 2, previewH), 11, 11),
                new RectangleGeometry(new Rect(0, previewH / 2, w - 2, previewH / 2)));
            clip.Freeze();
            preview.Clip = clip;
            preview.MouseLeftButtonUp += (_, _) => App.Current.ShowPopup(SampleText(), Loc.T("theme.previewSource") + " · " + t.LocName, t);

            // Nhãn góc trên phải (Mới / Hot)
            if (!string.IsNullOrEmpty(t.Badge))
            {
                preview.Children.Add(new Border
                {
                    Background = ThemeCatalog.EmblemBrush(t),
                    CornerRadius = new CornerRadius(0, 11, 0, 9),
                    Padding = new Thickness(9, 3, 9, 4),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Child = new TextBlock { Text = t.LocBadge, Foreground = Brushes.White, FontSize = 11, FontWeight = FontWeights.SemiBold }
                });
            }

            // Dấu tích khi đang dùng
            if (active)
            {
                preview.Children.Add(new Border
                {
                    Width = 22, Height = 22, CornerRadius = new CornerRadius(11),
                    Background = accent, BorderBrush = Brushes.White, BorderThickness = new Thickness(2),
                    HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 8, 8),
                    Child = new TextBlock
                    {
                        Text = "\uE73E", FontFamily = (FontFamily)FindResource("IconFont"), FontSize = 10,
                        Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
                    }
                });
            }

            // --- Tên + nút Dùng ---
            var useBtn = new Button
            {
                Cursor = Cursors.Hand,
                FocusVisualStyle = null,
                VerticalAlignment = VerticalAlignment.Center,
                IsEnabled = !active,
                Template = PillTemplate(),
                Background = active ? accent : Brushes.White,
                BorderBrush = accent,
                Foreground = active ? Brushes.White : accent,
                Padding = new Thickness(14, 4, 14, 5),
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Content = active ? Loc.T("theme.inUse") : Loc.T("theme.use")
            };
            useBtn.Click += (_, _) =>
            {
                App.Current.Settings.PopupTheme = t.Id;
                App.Current.SettingsChanged();
                App.Current.ShowPopup(SampleText(), t.LocName, t);
            };

            var info = new DockPanel { Margin = new Thickness(14, 10, 12, 12) };
            DockPanel.SetDock(useBtn, Dock.Right);
            info.Children.Add(useBtn);
            info.Children.Add(new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new TextBlock { Text = t.LocName, FontSize = 13.5, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("TextBrush") },
                    new TextBlock { Text = t.LocDescription, FontSize = 11.5, Foreground = (Brush)FindResource("TextMutedBrush"), Margin = new Thickness(0, 2, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis }
                }
            });

            var body = new StackPanel();
            body.Children.Add(preview);
            body.Children.Add(info);

            var card = new Border
            {
                Width = w,
                Margin = new Thickness(0, 0, 14, 14),
                CornerRadius = new CornerRadius(12),
                Background = Brushes.White,
                BorderBrush = active ? accent : (Brush)FindResource("CardBorderBrush"),
                BorderThickness = new Thickness(active ? 2 : 1),
                Child = body,
                RenderTransform = new TranslateTransform()
            };
            if (active) body.Margin = new Thickness(-1); // bù viền dày hơn để ảnh không bị lệch

            // Hiệu ứng nổi lên khi rê chuột
            card.MouseEnter += (_, _) =>
            {
                card.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 16, ShadowDepth = 3, Direction = 270, Opacity = 0.12 };
                ((TranslateTransform)card.RenderTransform).Y = -2;
            };
            card.MouseLeave += (_, _) =>
            {
                card.Effect = null;
                ((TranslateTransform)card.RenderTransform).Y = 0;
            };
            return card;
        }

        /// <summary>Nút dạng viên thuốc (bo tròn) giống nút "mua" trong ảnh mẫu.</summary>
        private static ControlTemplate PillTemplate()
        {
            var bd = new FrameworkElementFactory(typeof(Border));
            bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(15));
            bd.SetValue(Border.BorderThicknessProperty, new Thickness(1.5));
            bd.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            bd.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
            bd.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            bd.AppendChild(cp);
            return new ControlTemplate(typeof(Button)) { VisualTree = bd };
        }

        // ---------------- Trang Tra pinyin ----------------

        private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _previewTimer?.Stop();
            _previewTimer?.Start();
        }

        private void UpdatePreview()
        {
            if (PreviewHost == null) return;
            var s = App.Current.Settings;
            var lines = PinyinService.Convert(InputBox.Text ?? "", s.ToneStyle);
            PreviewHost.Content = RubyBuilder.Build(lines, s, 600, ThemeCatalog.Get("light"));
            _plain = PinyinService.ToPlainPinyin(lines);
            PlainBox.Text = _plain;

            var vocab = DictionaryService.Available ? DictionaryService.Vocabulary(lines, s.ToneStyle) : new List<WordInfo>();
            VocabCard.Visibility = vocab.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            VocabPreviewHost.Content = vocab.Count > 0 ? RubyBuilder.BuildVocab(vocab, s, 640, ThemeCatalog.Get("light")) : null;
            // Chưa có dữ liệu từ điển → nhắc cách tạo, để khỏi tưởng tính năng bị lỗi
            bool missing = !DictionaryService.Available && PinyinService.ContainsHan(InputBox.Text);
            DictHint.Text = missing ? Loc.T("set.dictMissing") : "";
            DictHint.Visibility = missing ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnPaste_Click(object sender, RoutedEventArgs e)
        {
            var t = ClipboardHelper.TryGetText();
            if (!string.IsNullOrEmpty(t)) InputBox.Text = t;
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            InputBox.Text = "";
            InputBox.Focus();
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_plain)) return;
            App.Current.CopyToClipboard(_plain);
            BtnCopyIcon.Text = "\uE73E";
            BtnCopyText.Text = Loc.T("common.copied");
            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.2) };
            t.Tick += (_, _) => { t.Stop(); BtnCopyIcon.Text = "\uE8C8"; BtnCopyText.SetResourceReference(TextBlock.TextProperty, "S.common.copyPinyin"); };
            t.Start();
        }

        private void BtnPreviewPopup_Click(object sender, RoutedEventArgs e)
        {
            App.Current.ShowPopup(SampleText(), Loc.T("theme.previewSource"));
        }
    }
}

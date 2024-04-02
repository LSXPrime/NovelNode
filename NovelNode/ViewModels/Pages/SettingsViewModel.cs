using System.ComponentModel;
using System.Collections;
using NovelNode.Data;
using NovelNode.Helpers;
using Wpf.Ui.Controls;
using Wpf.Ui.Appearance;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using System.Net.Http;
using Notification.Wpf;
using NovelNode.Views.Pages;
using NovelNode.Views.Windows;
using System.Windows.Forms;

namespace NovelNode.ViewModels.Pages;
public partial class SettingsViewModel : ObservableObject, INavigationAware, INotifyPropertyChanged
{
    [ObservableProperty]
    private ThemeType _currentTheme = ThemeType.Dark;

    [ObservableProperty]
    private string _appVersion = "1.0.0";

    public static IEnumerable ThemesValues => Enum.GetValues(typeof(ThemeType));

    public void OnStartup()
    {
        CurrentTheme = Theme.GetAppTheme();
    }

    public void OnNavigatedTo() { }

    public void OnNavigatedFrom() { }

    [RelayCommand]
    private void SwitchTheme(string parameter) => Theme.Apply(CurrentTheme);

    [RelayCommand]
    private void ModelsPathBrowse_Click(string targetPath)
    {
        
        var dialog = new FolderBrowserDialog();
        DialogResult result = dialog.ShowDialog();

        if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            switch (targetPath)
            {
                case "Path_Projects":
                    AppConfig.Instance.ProjectsPath = dialog.SelectedPath;
                    break;
            }
        }
        
    }

    [RelayCommand]
    private static void SaveSettings()
    {
        AppConfig.Instance.Save();
    }

    [RelayCommand]
    private static void ResetSettings()
    {
        AppConfig.Instance.Reset();
    }

    [RelayCommand]
    public static void NavigateTo(string navTarget)
    {
        var mainWindow = App.GetService<MainWindow>();
        switch (navTarget)
        {
            case "Home":
                mainWindow.NavigationView.Navigate(typeof(HomePage));
                break;
            case "Settings":
                mainWindow.NavigationView.Navigate(typeof(SettingsPage));
                break;
        }
    }

    [RelayCommand]
    public async Task CheckForUpdate()
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "request");

        var response = await client.GetAsync($"https://api.github.com/repos/LSXPrime/NovelNode/releases/latest");
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            dynamic releaseInfo = JsonConvert.DeserializeObject(json);
            string downloadUrl = string.Empty;

            foreach (var asset in releaseInfo.assets)
            {
                if (asset.name == "NovelNode.exe" || asset.name == "NovelNode.zip")
                {
                    downloadUrl = asset.browser_download_url;
                }
            }

            if (releaseInfo != null && releaseInfo.tag_name != AppVersion)
            {
                var VoiceNameR = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Update Available",
                    Content =  $"Update {releaseInfo.tag_name} is available to download.\n\n{releaseInfo.body}",
                    PrimaryButtonText = "Download",
                    CloseButtonText = "Cancel",
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                var result = await VoiceNameR.ShowDialogAsync();

                if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
                {
                    Extensions.Notify(new NotificationContent { Title = "NovelNode", Message = $"Update {releaseInfo.tag_name} started downloading.", Type = NotificationType.Information }, areaName: "NotificationArea");

                    var responseDownload = await client.GetAsync(downloadUrl);
                    if (responseDownload.IsSuccessStatusCode)
                    {
                        var content = await responseDownload.Content.ReadAsByteArrayAsync();

                        // Save the downloaded content to the specified location
                        await $"{Directory.GetCurrentDirectory()}\\NovelNode.exe.update".WriteBytesAsync(content);
                        FinalizeUpdate();
                    }
                }
            }
            else
                Extensions.Notify(new NotificationContent { Title = "NovelNode", Message = $"No available Updates yet.", Type = NotificationType.Information }, areaName: "NotificationArea");
        }

        static void FinalizeUpdate()
        {
            // The embedded batch script as a string
            string batchScript = $@"
@echo off
set ""APP_NAME=NovelNode.exe""
set ""DOWNLOAD_PATH={Directory.GetCurrentDirectory()}\NovelNode.exe.update""
set ""APP_PATH={Directory.GetCurrentDirectory()}\%APP_NAME%""

REM Close the running application
taskkill /IM %APP_NAME% /F

REM Replace the old app with the downloaded one and keep the same name
del /F /Q ""%APP_PATH%"" > nul
ren ""%DOWNLOAD_PATH%"" ""%APP_NAME%""
start %APP_PATH%
";

            // Save the batch script to a temporary file
            string tempBatchFile = Path.Combine(Path.GetTempPath(), "NovelNode_update.bat");
            tempBatchFile.WriteText(batchScript);

            // Execute the batch file
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = tempBatchFile,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };

            Process.Start(processInfo);
        }
    }
}

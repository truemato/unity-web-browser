using System;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Networking;

public sealed class DiscordBuildNotifier : IPostprocessBuildWithReport
{
    private const string WebhookEditorPrefsKey = "DiscordBuildNotifier.WebhookUrl";
    private const string WebhookEnvironmentKey = "DISCORD_BUILD_WEBHOOK_URL";
    private static bool _isHandlingBuild;
    private static DateTimeOffset _buildStartedAt;
    private static string _buildInitiator = "Build Settings";

    public int callbackOrder => 0;

    [InitializeOnLoadMethod]
    private static void RegisterBuildHandler()
    {
        BuildPlayerWindow.RegisterBuildPlayerHandler(HandleBuildPlayer);
    }

    [MenuItem("Tools/Discord Build Notification/Settings")]
    private static void OpenSettings()
    {
        DiscordBuildNotifierSettingsWindow.Open();
    }

    [MenuItem("Tools/Discord Build Notification/Test Webhook")]
    private static void SendTestWebhook()
    {
        if (!TryGetWebhookUrl(out var webhookUrl))
        {
            EditorUtility.DisplayDialog(
                "Discord Build Notification",
                "Webhook URL が未設定です。Tools/Discord Build Notification/Settings から設定してください。",
                "OK");
            return;
        }

        var message = BuildDiscordMessage(
            "Unity build webhook test",
            "\u2139\ufe0f",
            new[]
            {
                $"Project: {Application.productName}",
                $"Unity: {Application.unityVersion}",
                $"Machine: {Environment.MachineName}",
                $"User: {Environment.UserName}",
                $"Time: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}"
            });

        var sent = SendWebhook(webhookUrl, message);
        EditorUtility.DisplayDialog(
            "Discord Build Notification",
            sent ? "テスト通知を送信しました。" : "テスト通知の送信に失敗しました。Console を確認してください。",
            "OK");
    }

    private static void HandleBuildPlayer(BuildPlayerOptions options)
    {
        _isHandlingBuild = true;
        _buildStartedAt = DateTimeOffset.Now;
        _buildInitiator = "Build Settings";

        try
        {
            BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
        }
        finally
        {
        }
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        if (_isHandlingBuild)
        {
            NotifyFromReport(report, _buildStartedAt, _buildInitiator);
            _isHandlingBuild = false;
            _buildInitiator = "Build Settings";
            return;
        }

        NotifyFromReport(report, DateTimeOffset.Now - report.summary.totalTime, "Scripted Build");
    }

    internal static void NotifyFromReport(BuildReport report, DateTimeOffset startedAt, string initiator)
    {
        if (!TryGetWebhookUrl(out var webhookUrl))
        {
            Debug.Log("[DiscordBuildNotifier] Webhook URL が未設定のため通知をスキップしました。");
            return;
        }

        var summary = report.summary;
        var finishedAt = DateTimeOffset.Now;
        var succeeded = summary.result == BuildResult.Succeeded;
        var canceled = summary.result == BuildResult.Cancelled;

        var title = succeeded
            ? "Unity build finished"
            : canceled
                ? "Unity build cancelled"
                : "Unity build failed";

        var statusIcon = succeeded ? "\u2705" : canceled ? "\u23f9\ufe0f" : "\u274c";

        var message = BuildDiscordMessage(
            title,
            statusIcon,
            new[]
            {
                $"Project: {Application.productName}",
                $"Result: {summary.result}",
                $"Target: {summary.platform} ({summary.platformGroup})",
                $"Output: {summary.outputPath}",
                $"Duration: {summary.totalTime:hh\\:mm\\:ss}",
                $"Started: {startedAt:yyyy-MM-dd HH:mm:ss zzz}",
                $"Finished: {finishedAt:yyyy-MM-dd HH:mm:ss zzz}",
                $"Initiator: {initiator}",
                $"Unity: {Application.unityVersion}",
                $"Machine: {Environment.MachineName}",
                $"User: {Environment.UserName}",
                $"Warnings: {summary.totalWarnings}",
                $"Errors: {summary.totalErrors}",
                $"Size: {FormatBytes(summary.totalSize)}"
            });

        SendWebhook(webhookUrl, message);
    }

    private static string BuildDiscordMessage(string title, string statusIcon, string[] lines)
    {
        var builder = new StringBuilder();
        builder.Append(statusIcon).Append(' ').Append(title);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            builder.Append('\n').Append(line);
        }

        return builder.ToString();
    }

    private static bool TryGetWebhookUrl(out string webhookUrl)
    {
        webhookUrl = EditorPrefs.GetString(WebhookEditorPrefsKey, string.Empty).Trim();
        if (!string.IsNullOrEmpty(webhookUrl))
        {
            return true;
        }

        webhookUrl = (Environment.GetEnvironmentVariable(WebhookEnvironmentKey) ?? string.Empty).Trim();
        return !string.IsNullOrEmpty(webhookUrl);
    }

    internal static string GetStoredWebhookUrl()
    {
        return EditorPrefs.GetString(WebhookEditorPrefsKey, string.Empty);
    }

    internal static void SaveWebhookUrl(string webhookUrl)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            EditorPrefs.DeleteKey(WebhookEditorPrefsKey);
            return;
        }

        EditorPrefs.SetString(WebhookEditorPrefsKey, webhookUrl.Trim());
    }

    internal static string GetEnvironmentVariableName()
    {
        return WebhookEnvironmentKey;
    }

    private static bool SendWebhook(string webhookUrl, string message)
    {
        var payload = JsonUtility.ToJson(new DiscordWebhookPayload { content = message });
        using (var request = new UnityWebRequest(webhookUrl, UnityWebRequest.kHttpVerbPOST))
        {
            var bytes = Encoding.UTF8.GetBytes(payload);
            request.uploadHandler = new UploadHandlerRaw(bytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                Thread.Sleep(10);
            }

            if (request.result == UnityWebRequest.Result.ConnectionError
                || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"[DiscordBuildNotifier] Discord通知の送信に失敗しました: {request.error}");
                return false;
            }

            Debug.Log("[DiscordBuildNotifier] Discord通知を送信しました。");
            return true;
        }
    }

    private static string FormatBytes(ulong bytes)
    {
        if (bytes == 0)
        {
            return "0 B";
        }

        string[] units = { "B", "KB", "MB", "GB", "TB" };
        var size = bytes;
        var unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size} {units[unitIndex]}";
    }

    [Serializable]
    private sealed class DiscordWebhookPayload
    {
        public string content;
    }
}

public sealed class DiscordBuildNotifierSettingsWindow : EditorWindow
{
    private string _webhookUrl = string.Empty;

    internal static void Open()
    {
        var window = GetWindow<DiscordBuildNotifierSettingsWindow>("Discord Build Notification");
        window.minSize = new Vector2(520f, 180f);
        window.Show();
    }

    private void OnEnable()
    {
        _webhookUrl = DiscordBuildNotifier.GetStoredWebhookUrl();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Discord Webhook URL", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Webhook URL は EditorPrefs にローカル保存されます。未設定時は環境変数 DISCORD_BUILD_WEBHOOK_URL も参照します。",
            MessageType.Info);

        EditorGUILayout.Space(4f);
        _webhookUrl = EditorGUILayout.TextField(_webhookUrl);

        EditorGUILayout.Space(10f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Save"))
            {
                DiscordBuildNotifier.SaveWebhookUrl(_webhookUrl);
                EditorUtility.DisplayDialog("Discord Build Notification", "Webhook URL を保存しました。", "OK");
            }

            if (GUILayout.Button("Clear"))
            {
                _webhookUrl = string.Empty;
                DiscordBuildNotifier.SaveWebhookUrl(_webhookUrl);
                EditorUtility.DisplayDialog("Discord Build Notification", "保存済みのWebhook URLを削除しました。", "OK");
            }
        }

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Environment Variable", EditorStyles.miniBoldLabel);
        EditorGUILayout.SelectableLabel(
            DiscordBuildNotifier.GetEnvironmentVariableName(),
            EditorStyles.textField,
            GUILayout.Height(EditorGUIUtility.singleLineHeight));
    }
}

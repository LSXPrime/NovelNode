using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Controls;
using NovelNode.Data;
using Newtonsoft.Json;
using Notification.Wpf;
using Serilog;
using Serilog.Events;
using Wpf.Ui.Controls;
using NovelNode.Enums;
using NetFabric.Hyperlinq;
using System.Windows.Media.Imaging;
using System.Reflection;

namespace NovelNode.Helpers;
public static class Extensions
{
    #region UIStatics

    public static SnackbarPresenter? SnackbarArea;
    public static ContentPresenter? ContentArea;
    public static Nodify.NodifyEditor? NodeEditor;
    private static readonly NotificationManager notification = new();

    #endregion

    #region Events
    public delegate void SaveDataEvent();
    public static SaveDataEvent onSaveData;

    public static void SaveData()
    {
        if (onSaveData != null)
            onSaveData();
    }

    public delegate void LoadDataEvent();
    public static LoadDataEvent onLoadData;

    public static void LoadData()
    {
        if (onLoadData != null)
            onLoadData();
    }

    public delegate void ProjectLoadEvent();
    public static ProjectLoadEvent onProjectLoad;

    public static void ProjectLoad()
    {
        if (onProjectLoad != null)
            onProjectLoad();
    }

    #endregion

    #region DataHandlingMethods

    public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    public static void RemoveWhere<T>(this ObservableCollection<T> collection, Func<T, bool> predicate)
    {
        for (int i = collection.Count - 1; i >= 0; i--)
        {
            if (predicate(collection[i]))
                collection.RemoveAt(i);
        }
    }

    public static string RemoveSpaces(this string text)
    {
        return text.Replace(" ", "");
    }

    public static double BytesToMB(this long bytes)
    {
        const double megabyte = 1024 * 1024;

        double megabytes = (double)bytes / megabyte;
        return megabytes;
    }

    public static double BytesToGB(this long bytes)
    {
        const double gigabyte = 1024 * 1024 * 1024;

        double gigabytes = (double)bytes / gigabyte;
        return gigabytes;
    }

    public static BitmapImage GetBitmapImage(this string path)
    {
        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = path.GetFileStream();
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        return bitmapImage;
    }

    // Why all these stream operations ? because all the fucked up System.IO.File operation locking up files and doesn't allow to change access or share options

    public static void CopyStreamTo(this string source, string destination)
    {
        var sourceStream = source.GetFileStream();
        var destinationStream = destination.GetFileStream();
        sourceStream.CopyTo(destinationStream);
        sourceStream.Dispose();
        destinationStream.Dispose();
    }

    public static async Task CopyStreamToAsync(this string source, string destination)
    {
        var sourceStream = source.GetFileStream(true);
        var destinationStream = destination.GetFileStream();
        await sourceStream.CopyToAsync(destinationStream);
        sourceStream.Dispose();
        destinationStream.Dispose();
    }

    public static string ReadText(this string path)
    {
        using var stream = new StreamReader(path, new FileStreamOptions { Access = FileAccess.ReadWrite, Mode = FileMode.OpenOrCreate, Share = FileShare.ReadWrite });
        return stream.ReadToEnd();
    }

    public static void WriteText(this string path, string value)
    {
        var fs = path.GetFileStream();
        fs.SetLength(0);
        fs.Dispose();
        using var stream = new StreamWriter(path, new FileStreamOptions { Access = FileAccess.ReadWrite, Mode = FileMode.OpenOrCreate, Share = FileShare.ReadWrite });
        stream.Write(value);
    }
    
    public static async Task<string> ReadTextAsync(this string path)
    {
        using var stream = new StreamReader(path, new FileStreamOptions { Access = FileAccess.ReadWrite, Mode = FileMode.OpenOrCreate, Share = FileShare.ReadWrite });
        return await stream.ReadToEndAsync();
    }

    public static async Task WriteTextAsync(this string path, string value)
    {
        var fs = path.GetFileStream();
        fs.SetLength(0);
        fs.Dispose();
        using var stream = new StreamWriter(path, new FileStreamOptions { Access = FileAccess.ReadWrite, Mode = FileMode.OpenOrCreate, Share = FileShare.ReadWrite });
        await stream.WriteAsync(value);
    }

    public static byte[] ReadBytes(this string path)
    {
        var stream = path.GetFileStream();
        byte[] buffer = new byte[stream.Length];
        stream.Read(buffer, 0, buffer.Length);
        return buffer;
    }

    public static void WriteBytes(this string path, byte[] value)
    {
        var stream = path.GetFileStream();
        stream.Write(value, 0, value.Length);
    }
    
    public static async Task<byte[]> ReadBytesAsync(this string path)
    {
        var stream = path.GetFileStream(true);
        byte[] buffer = new byte[stream.Length];
        await stream.ReadAsync(buffer);
        return buffer;
    }

    public static async Task WriteBytesAsync(this string path, byte[] value)
    {
        var stream = path.GetFileStream();
        await stream.WriteAsync(value);
    }

    public static FileStream GetFileStream(this string path, bool useAsync = false)
    {
        var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, bufferSize: 4096, useAsync: useAsync);
        return fs;
    }


    public static T Clone<T>(T obj)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj), "Object cannot be null.");
        }

        static object deepCopyInternal(object source)
        {
            if (source == null || source.GetType().IsPrimitive || source.GetType() == typeof(string))
            {
                return source;
            }

            Type type = source.GetType();
            object copy = Activator.CreateInstance(type);

            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (PropertyInfo property in properties)
            {
                if (property.CanRead && property.CanWrite)
                {
                    object value = property.GetValue(source);
                    object copiedValue = deepCopyInternal(value);
                    property.SetValue(copy, copiedValue);
                }
            }

            return copy;
        }

        return (T)deepCopyInternal(obj);
    }

    #endregion

    #region EnumsValues
    public static string[] EnumValues<TEnum>()
    {
        if (!typeof(TEnum).IsEnum)
            throw new ArgumentException($"{nameof(TEnum)} must be enum");

        return Enum.GetValues(typeof(TEnum)).Cast<TEnum>().Select(x => x.ToString()).ToArray();
    }

    public static IEnumerable NodeSwitch => Enum.GetValues(typeof(NodeSwitch));
    public static IEnumerable NodeConnectorFlow => Enum.GetValues(typeof(NodeConnectorFlow));
    public static IEnumerable ValueType => Enum.GetValues(typeof(Enums.ValueType));
    public static IEnumerable EventTask => Enum.GetValues(typeof(EventTask));
    public static IEnumerable ComparisonOperator => Enum.GetValues(typeof(ComparisonOperator));
    public static IEnumerable CheckpointAction => Enum.GetValues(typeof(CheckpointAction));
    public static IEnumerable AssetType => Enum.GetValues(typeof(AssetType));

    #endregion

    #region UIHandlingMethods
    public static void Notify(string title, string message, NotificationType type = NotificationType.None, TimeSpan? expirationTime = null, Action onClick = null, Action onClose = null, bool CloseOnClick = true, bool ShowXbtn = true, Exception ex = null, LogEventLevel logLevel = default)
    {
        Notify(new NotificationContent { Title = title, Message = message, Type = type }, "NotificationArea", expirationTime, onClick, onClose, CloseOnClick, ShowXbtn);
    }

    public static void Notify(NotificationContent content, string areaName = "", TimeSpan? expirationTime = null, Action onClick = null, Action onClose = null, bool CloseOnClick = true, bool ShowXbtn = true, Exception ex = null, LogEventLevel logLevel = default)
    {
        notification.Show(content, areaName, expirationTime, onClick, onClose, CloseOnClick, ShowXbtn);
        Log.Logger.Write(logLevel != default ? logLevel : NotifyToLog(), $"{content.Title}, {content.Message}", ex);

        LogEventLevel NotifyToLog()
        {
            return content.Type switch
            {
                NotificationType.None => LogEventLevel.Debug,
                NotificationType.Information => LogEventLevel.Information,
                NotificationType.Notification => LogEventLevel.Verbose,
                NotificationType.Error => LogEventLevel.Error,
                NotificationType.Warning => LogEventLevel.Warning,
                _ => LogEventLevel.Fatal,
            };
        }
    }

    #endregion

    #region PlayerViewMethods

    public static ObservableCollection<BlackboardData> Copy(this ObservableCollection<BlackboardData> source)
    {
        return new ObservableCollection<BlackboardData>(
            source.Select(item => new BlackboardData
            {
                Name = item.Name,
                ID = item.ID,
                Strings = Copy(item.Strings),
                Floats = Copy(item.Floats),
                Booleans = Copy(item.Booleans),
                All = Copy(item.All)
            }));
    }

    private static ObservableCollection<KeyValue> Copy(ObservableCollection<KeyValue> source)
    {
        return new ObservableCollection<KeyValue>(
            source.Select(keyValue => new KeyValue
            {
                Key = keyValue.Key,
                Type = keyValue.Type,
                Value = new()
                {
                    String = keyValue.Value.String,
                    Float = keyValue.Value.Float,
                    Boolean = keyValue.Value.Boolean
                }
            }));
    }

    #endregion

    #region SecurityMethods
    public static string CalculateMD5Hash(this Stream stream)
    {
        using var md5 = MD5.Create();
        byte[] hashBytes = md5.ComputeHash(stream);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }


    public static byte[] Encrypt<T>(T obj)
    {
        byte[] data;
        using var stream = new MemoryStream();
        var json = JsonConvert.SerializeObject(obj);
        stream.Write(Encoding.UTF8.GetBytes(json));
        data = stream.ToArray();

        var key = MD5.HashData(Encoding.UTF8.GetBytes(AppConfig.Instance.DataSecretKey));
        using var des = new TripleDESCryptoServiceProvider();
        des.Key = key;
        des.Mode = CipherMode.ECB;
        des.Padding = PaddingMode.PKCS7;

        using var encryptedStream = new MemoryStream();
        using var cryptoStream = new CryptoStream(encryptedStream, des.CreateEncryptor(), CryptoStreamMode.Write);
        cryptoStream.Write(data, 0, data.Length);
        cryptoStream.FlushFinalBlock();

        return encryptedStream.ToArray();
    }

    public static T Decrypt<T>(byte[] obj)
    {
        var key = MD5.HashData(Encoding.UTF8.GetBytes(AppConfig.Instance.DataSecretKey));
        using var des = new TripleDESCryptoServiceProvider();
        des.Key = key;
        des.Mode = CipherMode.ECB;
        des.Padding = PaddingMode.PKCS7;

        using var decryptedStream = new MemoryStream();
        using var cryptoStream = new CryptoStream(decryptedStream, des.CreateDecryptor(), CryptoStreamMode.Write);
        cryptoStream.Write(obj, 0, obj.Length);
        cryptoStream.FlushFinalBlock();

        decryptedStream.Position = 0;
        var json = Encoding.UTF8.GetString(decryptedStream.ToArray());

        return JsonConvert.DeserializeObject<T>(json);
    }

    #endregion
}
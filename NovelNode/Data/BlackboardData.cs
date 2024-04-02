using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using NovelNode.Helpers;

namespace NovelNode.Data;

public partial class BlackboardData : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;
    [ObservableProperty]
    private int _iD = 0;
    [ObservableProperty]
    private ObservableCollection<KeyValue> _strings = [];
    [ObservableProperty]
    private ObservableCollection<KeyValue> _floats = [];
    [ObservableProperty]
    private ObservableCollection<KeyValue> _booleans = [];
    [ObservableProperty]
    [property: JsonIgnore]
    private ObservableCollection<KeyValue> _all = [];

    public BlackboardData()
    {
        _strings.CollectionChanged += SourceCollectionChanged;
        _floats.CollectionChanged += SourceCollectionChanged;
        _booleans.CollectionChanged += SourceCollectionChanged;
    }

    private void SourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        All.Clear();
        All.AddRange(Strings);
        All.AddRange(Floats);
        All.AddRange(Booleans);
    }

    [OnDeserialized]
    internal void OnDeserializedMethod(StreamingContext context) => SourceCollectionChanged(null, null);
}

public partial class KeyValue : ObservableObject
{
    [ObservableProperty]
    private string _key;
    [ObservableProperty]
    private ValueData _value = new();
    [ObservableProperty]
    private Enums.ValueType _type;

    public partial class ValueData : ObservableObject
    {
        [ObservableProperty]
        private string _string = string.Empty;
        [ObservableProperty]
        private float _float = 0f;
        [ObservableProperty]
        private bool _boolean = false;
    }
}

// ReSharper disable once CheckNamespace
namespace System;

public class BinaryFile : object, IBinaryFile
{
    #region "Properties"
    public byte[]? Data { get; set; }
    public bool IsNull => Data == null;
    public bool IsEmpty => Data == null || Data.Length == 0;
    public int Length => Data?.Length ?? 0;
    public Text.Encoding Encoding { get; set; } = Text.Encoding.UTF8;
    public string Value
    {
        get => (Data is null) ? string.Empty : Encoding.GetString(Data);
        set
        {
            if (!string.IsNullOrEmpty(value))
                Data = Encoding.GetBytes(value);
        }
    }
    
    #endregion
    
    #region "Constructor"
    
    public BinaryFile() { }

    public BinaryFile(string? str)
    {
        if (!string.IsNullOrEmpty(str))
            Value = str;
    }

    #endregion
    
    public override string ToString() => Value;
    
    
}
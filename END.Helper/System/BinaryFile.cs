
// ReSharper disable once CheckNamespace
namespace System;

public class BinaryFile : object
{
    #region "Properties"
    // ReSharper disable once MemberCanBePrivate.Global
    public byte[]? Data { get; set; }
    public bool IsNull => Data == null;
    public bool IsEmpty => Data == null || Data.Length == 0;
    
    // ReSharper disable once MemberCanBePrivate.Global
    public Text.Encoding Encoding { get; set; } = Text.Encoding.UTF8;

    // ReSharper disable once MemberCanBePrivate.Global
    public string Value
    {
        get => (Data is null) ? string.Empty : Encoding.GetString(Data);
        // ReSharper disable once PropertyCanBeMadeInitOnly.Global
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
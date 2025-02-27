// ReSharper disable once CheckNamespace
namespace System;

public interface IBinaryFile
{
    public byte[]? Data { get; set; }
    public bool IsNull { get; }
    public bool IsEmpty { get; }
    public int Length { get; }
    public Text.Encoding Encoding { get; set; }
    public string Value { get; set; }
    public string ToString();
}
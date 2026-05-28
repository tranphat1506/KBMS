using System;

namespace KBMS.Models;

public class Constant
{
    public string Name { get; set; } = string.Empty;
    public object? Value { get; set; }
    public string Type { get; set; } = "string"; // Can be string, number, boolean
}

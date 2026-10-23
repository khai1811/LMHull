using System.Collections.Generic;

namespace LMHull.Core.Validation;

public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new();

    public void AddError(string error) => Errors.Add(error);

    public string GetErrorMessage() => string.Join("\n• ", Errors);
}
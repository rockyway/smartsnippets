namespace SmartSnippets.Core.Domain;

public static class SnippetValidator
{
    public static IReadOnlyList<ValidationError> Validate(string text, string label)
    {
        var errors = new List<ValidationError>();
        if (string.IsNullOrWhiteSpace(text))
        {
            errors.Add(ValidationError.TextRequired);
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            errors.Add(ValidationError.LabelRequired);
        }

        return errors;
    }
}

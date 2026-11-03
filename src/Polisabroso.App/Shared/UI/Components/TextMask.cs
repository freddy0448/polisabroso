using System.Text;

namespace Polisabroso.Shared.UI.Components;

internal static class TextMask
{
    internal readonly record struct Token(char Character, bool IsSlot);

    internal static void Validate(string mask)
    {
        var tokens = Parse(mask);
        if (!tokens.Any(token => token.IsSlot))
        {
            throw new ArgumentException("A mask must contain at least one slot (0, A, or *).", nameof(mask));
        }
    }

    internal static List<Token> Parse(string mask)
    {
        ArgumentNullException.ThrowIfNull(mask);
        var tokens = new List<Token>();
        for (var index = 0; index < mask.Length; index++)
        {
            var character = mask[index];
            if (character == '\\')
            {
                if (++index == mask.Length)
                {
                    throw new ArgumentException("A mask cannot end with an escape character.", nameof(mask));
                }
                tokens.Add(new Token(mask[index], false));
            }
            else
            {
                tokens.Add(new Token(character, character is '0' or 'A' or '*'));
            }
        }
        return tokens;
    }

    internal static bool Accepts(char slot, char value) => slot switch
    {
        '0' => value is >= '0' and <= '9',
        'A' => value is >= 'a' and <= 'z' or >= 'A' and <= 'Z',
        '*' => char.IsAsciiLetterOrDigit(value),
        _ => false
    };

    internal static string Format(string rawValue, string mask)
    {
        var output = new StringBuilder();
        var literals = new StringBuilder();
        var position = 0;
        foreach (var token in Parse(mask))
        {
            if (!token.IsSlot)
            {
                literals.Append(token.Character);
                continue;
            }
            while (position < rawValue.Length && !Accepts(token.Character, rawValue[position]))
            {
                position++;
            }
            if (position == rawValue.Length)
            {
                return output.ToString();
            }
            output.Append(literals);
            literals.Clear();
            output.Append(rawValue[position++]);
        }
        output.Append(literals);
        return output.ToString();
    }

    internal static string Unmask(string displayedValue, string mask)
    {
        var tokens = Parse(mask);
        var output = new StringBuilder();
        var position = 0;
        for (var index = 0; index < tokens.Count && position < displayedValue.Length; index++)
        {
            var token = tokens[index];
            if (!token.IsSlot)
            {
                var literals = new StringBuilder();
                while (index < tokens.Count && !tokens[index].IsSlot)
                {
                    literals.Append(tokens[index++].Character);
                }
                index--;
                if (displayedValue.AsSpan(position).StartsWith(literals.ToString(), StringComparison.Ordinal))
                {
                    position += literals.Length;
                }
                continue;
            }
            while (position < displayedValue.Length && !Accepts(token.Character, displayedValue[position]))
            {
                position++;
            }
            if (position < displayedValue.Length)
            {
                output.Append(displayedValue[position++]);
            }
        }
        return output.ToString();
    }
}

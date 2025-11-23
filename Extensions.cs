using System.Text;

namespace Mentat;

public static class Extensions
{
    public static IEnumerable<IEnumerable<T>> SplitBy<T>(this IEnumerable<T> values, Predicate<T> predicate)
    {
        var buffer = new List<T>();

        foreach (var value in values)
        {
            if (predicate(value))
            {
                if (buffer.Count > 0)
                {
                    yield return buffer;

                    buffer = new List<T>();
                }
            }
            else
            {
                buffer.Add(value);
            }
        }

        if (buffer.Count > 0)
        {
            yield return buffer;
        }
    }

    public static IEnumerable<string> SplitByParagraphs(this string value, int minLength)
    {
        if (value.Length <= minLength)
        {
            yield return value;
            yield break;
        }

        var builder = new StringBuilder();
        var reader = new StringReader(value);

        while (reader.ReadLine() is string line)
        {
            builder.AppendLine(line);

            if (line.Length == 0 && builder.Length > minLength)
            {
                yield return builder.ToString().Trim('\n', '\t');

                builder.Clear();
            }
        }

        if (builder.Length > 0)
        {
            yield return builder.ToString().Trim('\n', '\t');
        }
    }
}
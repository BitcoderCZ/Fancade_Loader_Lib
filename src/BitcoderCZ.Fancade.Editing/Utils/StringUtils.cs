// <copyright file="StringUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using static BitcoderCZ.Fancade.Utils.ThrowHelper;

namespace BitcoderCZ.Fancade.Editing.Utils;

internal static class StringUtils
{
    /// <summary>
    /// Splits <paramref name="str"/> into ranges of <paramref name="maxLength"/>, trying to split on spaces.
    /// </summary>
    /// <param name="str">The <see cref="string"/> to split.</param>
    /// <param name="maxLength">The maximum length of each <see cref="Range"/>.</param>
    /// <returns>The splits.</returns>
    public static List<Range> SplitByMaxLength(ReadOnlySpan<char> str, int maxLength)
    {
        if (maxLength < 1)
        {
            ThrowArgumentOutOfRangeException(nameof(maxLength));
        }

        if (str.IsEmpty)
        {
            return [];
        }
        else if (maxLength >= str.Length)
        {
            return [new Range(0, str.Length)];
        }

        List<Range> ranges;

        if (maxLength == 1)
        {
            ranges = new List<Range>(str.Length);

            for (int i = 0; i < str.Length; i++)
            {
                ranges.Add(new Range(i, i + 1));
            }

            return ranges;
        }

        ranges = [];

        int index = 0;

        List<Range> currentLine = new List<Range>(maxLength / 2);
        int currentLineLength = 0;

        while (index < str.Length)
        {
            int spaceIndex = str[index..].IndexOf(' ') + index;

            if (spaceIndex - index == -1)
            {
                spaceIndex = str.Length;
            }

            Range wordRange = new Range(index, spaceIndex);
            int wordLength = spaceIndex - index;
            if (wordLength == 0)
            {
                goto nextWord;
            }

            int newLineIndex = str[wordRange].IndexOf('\n');
            if (newLineIndex != -1)
            {
                spaceIndex = newLineIndex + index;
                wordRange = new Range(index, spaceIndex);
                wordLength = spaceIndex - index;
            }

            // + 1 -> the space between the words and the current word
            if (currentLineLength + wordLength + 1 > maxLength)
            {
                CurrentLineToRange();
            }

            if (wordLength >= maxLength)
            {
                for (int i = 0; i < wordLength; i += maxLength)
                {
                    int lengthToAdd = Math.Min(wordLength - i, maxLength);
                    if (lengthToAdd == 0)
                    {
                        break;
                    }

                    currentLine.Add(new Range(index + i, index + i + lengthToAdd));
                    AddLength(lengthToAdd);

                    if (currentLineLength >= maxLength)
                    {
                        CurrentLineToRange();
                    }
                }
            }
            else
            {
                currentLine.Add(wordRange);
                AddLength(wordLength);
            }

            if (newLineIndex != -1)
            {
                CurrentLineToRange();
            }

        nextWord:
            index = spaceIndex + 1;
        }

        if (currentLineLength > 0)
        {
            CurrentLineToRange();
        }

        return ranges;

        void AddLength(int length)
        {
            if (currentLineLength == 0)
            {
                currentLineLength = length;
            }
            else
            {
                currentLineLength += length + 1;
            }
        }

        void CurrentLineToRange()
        {
            if (currentLine.Count == 0)
            {
                return;
            }

            ranges.Add(new Range(currentLine[0].Start, currentLine[^1].End));

            currentLine.Clear();
            currentLineLength = 0;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipboardUtility.src.Services;

internal class TextProcessingService
{
    public int CountCharacters(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return 0;
        }
        return input.Length;
    }
}

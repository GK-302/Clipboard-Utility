using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clippo.src.Services;

internal class TextProcessingService
{
    public string CountCharacters(string input)
    {
        if (input == null) return "0";
        return input.Length.ToString();
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace FancadeLoaderLib.Runtime.Exceptions;

public sealed class InvalidInputException : FancadeException
{
    public InvalidInputException(string blockName)
        : base($"{blockName} got invalid (inf or nan) input!")
    {
    }
}

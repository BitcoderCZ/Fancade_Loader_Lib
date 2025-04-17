using System;
using System.Collections.Generic;
using System.Text;

namespace FancadeLoaderLib.Runtime.Exceptions;

public abstract class FancadeException : Exception
{
    protected FancadeException(string message)
        : base(message)
    {
        FancadeMessage = message;
    }

    protected FancadeException(string fancadeMessage, string message)
        : base(message)
    {
        FancadeMessage = fancadeMessage;
    }

    public string FancadeMessage { get; }
}

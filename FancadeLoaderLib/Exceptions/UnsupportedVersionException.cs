using System;
using System.Collections.Generic;
using System.Text;

namespace FancadeLoaderLib.Exceptions
{
    public class UnsupportedVersionException : Exception
    {
        public int Version { get; private set; }

        public UnsupportedVersionException(int _version)
            : base(_version > RawGame.CurrentFileVersion ? $"File version '{_version}' isn't supported, highest supported version is {RawGame.CurrentFileVersion}." : $"File version '{_version}' isn't supported.")
        {
            Version = _version;
        }
    }
}

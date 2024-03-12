# Fancade Loader Lib
C# lib for loading and saving games for [Fancade](https://www.fancade.com/)

If you aren't using this from a .NET Framefork project, Download [this](https://www.dropbox.com/scl/fi/kjhoriojssv4qctso2k1n/Unity.Compression.dll?rlkey=o6rmlcgep4di9qjh0oeywkp5p&dl=0) (Bassically some classes from System.IO.Compression modified to allow for specifing the compressor used, the name is like that because I originally used it with Unity)  
Then paste this **before** you use Game.Load()  
```csharp
 // Make sure Unity.Compression uses ZLib
 Unity.Compression.DeflateStream.DeflaterType = Unity.Compression.DeflateStream.WorkerType.ZLib;
 Unity.Compression.DeflateStream.InflaterType = Unity.Compression.DeflateStream.WorkerType.ZLib;
 // Make Game use Unity.Compression
 FancadeLoaderLib.GZip.CompressMain = (from, to) =>
 {
     using Unity.Compression.GZipStream destination = new Unity.Compression.GZipStream(to, CompressionLevel.Optimal, leaveOpen: true);
     from.Position = 0;
     from.CopyTo(destination);
     from.Dispose();
 };
 FancadeLoaderLib.GZip.DecompressMain = (from, to) =>
 {
     using Unity.Compression.GZipStream gZipStream = new Unity.Compression.GZipStream(from, CompressionMode.Decompress, leaveOpen: false);
     gZipStream.CopyTo(to);
 };
```
Also make sure [this](https://www.dropbox.com/scl/fi/xzdvbs6rozwd93einq7g1/clrcompression.dll?rlkey=vqoacrd5z1253h00tqfawmysq&dl=0) is next to the built exe

namespace Outlet.Registry.Storage;

/// <summary>Options for the filesystem adapter, bound via <c>IOptions&lt;FileSystemObjectStorageOptions&gt;</c>.</summary>
public sealed class FileSystemObjectStorageOptions
{
    /// <summary>Root directory under which objects are stored. Created on first write if missing.</summary>
    public string RootPath { get; set; } = "";
}

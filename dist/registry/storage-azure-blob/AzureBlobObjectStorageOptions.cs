namespace Outlet.Registry.Storage;

/// <summary>Options for the Azure Blob Storage adapter, bound via <c>IOptions&lt;AzureBlobObjectStorageOptions&gt;</c>.</summary>
public sealed class AzureBlobObjectStorageOptions
{
    /// <summary>
    /// Storage account connection string. Required. Accepts a real account-key string,
    /// a SAS connection string, or <c>UseDevelopmentStorage=true</c> for Azurite.
    /// A SAS read URI can only be generated when the string carries an account key.
    /// </summary>
    public string ConnectionString { get; set; } = "";

    /// <summary>Blob container that holds the objects. Required.</summary>
    public string ContainerName { get; set; } = "";

    /// <summary>Create the container on first write when it does not exist (handy for local dev / tests).</summary>
    public bool CreateContainerIfNotExists { get; set; }
}

namespace Domain.Models
{
    /// <summary>
    /// Represents the capabilities supported by a storage provider.
    /// Used for capability negotiation and feature detection.
    /// </summary>
    public class ProviderCapabilities
    {
        /// <summary>
        /// Provider supports atomic rename operations
        /// </summary>
        public bool SupportsAtomicRename { get; set; }

        /// <summary>
        /// Provider supports symbolic links
        /// </summary>
        public bool SupportsSymlinks { get; set; }

        /// <summary>
        /// Provider supports hard links
        /// </summary>
        public bool SupportsHardLinks { get; set; }

        /// <summary>
        /// Provider supports file metadata (timestamps, permissions, etc.)
        /// </summary>
        public bool SupportsMetadata { get; set; }

        /// <summary>
        /// Provider supports directory operations
        /// </summary>
        public bool SupportsDirectories { get; set; } = true;

        /// <summary>
        /// Provider supports recursive directory operations
        /// </summary>
        public bool SupportsRecursiveOperations { get; set; } = true;

        /// <summary>
        /// Provider supports streaming operations for large files
        /// </summary>
        public bool SupportsStreaming { get; set; } = true;

        /// <summary>
        /// Provider supports partial file reads (range requests)
        /// </summary>
        public bool SupportsPartialReads { get; set; }

        /// <summary>
        /// Provider supports file locking
        /// </summary>
        public bool SupportsFileLocking { get; set; }

        /// <summary>
        /// Provider supports versioning/history
        /// </summary>
        public bool SupportsVersioning { get; set; }

        /// <summary>
        /// Provider supports server-side copy operations
        /// </summary>
        public bool SupportsServerSideCopy { get; set; }

        /// <summary>
        /// Provider supports server-side move operations
        /// </summary>
        public bool SupportsServerSideMove { get; set; }

        /// <summary>
        /// Provider supports batch operations
        /// </summary>
        public bool SupportsBatchOperations { get; set; }

        /// <summary>
        /// Provider supports search/query operations
        /// </summary>
        public bool SupportsSearch { get; set; }

        /// <summary>
        /// Provider supports access control lists (ACLs)
        /// </summary>
        public bool SupportsAcls { get; set; }

        /// <summary>
        /// Provider supports encryption at rest
        /// </summary>
        public bool SupportsEncryption { get; set; }

        /// <summary>
        /// Provider supports compression
        /// </summary>
        public bool SupportsCompression { get; set; }

        /// <summary>
        /// Maximum file size supported (in bytes), null if unlimited
        /// </summary>
        public long? MaxFileSize { get; set; }

        /// <summary>
        /// Maximum path length supported, null if unlimited
        /// </summary>
        public int? MaxPathLength { get; set; }

        /// <summary>
        /// Supported file extensions (null or empty if all supported)
        /// </summary>
        public List<string>? SupportedExtensions { get; set; }

        /// <summary>
        /// Provider version
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Additional provider-specific capabilities
        /// </summary>
        public Dictionary<string, object>? CustomCapabilities { get; set; }

        /// <summary>
        /// Creates default capabilities for a basic provider
        /// </summary>
        public static ProviderCapabilities Default()
        {
            return new ProviderCapabilities
            {
                SupportsAtomicRename = false,
                SupportsSymlinks = false,
                SupportsHardLinks = false,
                SupportsMetadata = true,
                SupportsDirectories = true,
                SupportsRecursiveOperations = true,
                SupportsStreaming = true,
                SupportsPartialReads = false,
                SupportsFileLocking = false,
                SupportsVersioning = false,
                SupportsServerSideCopy = false,
                SupportsServerSideMove = false,
                SupportsBatchOperations = false,
                SupportsSearch = false,
                SupportsAcls = false,
                SupportsEncryption = false,
                SupportsCompression = false
            };
        }

        /// <summary>
        /// Creates capabilities for a local filesystem provider
        /// </summary>
        public static ProviderCapabilities ForLocal()
        {
            return new ProviderCapabilities
            {
                SupportsAtomicRename = true,
                SupportsSymlinks = true,
                SupportsHardLinks = true,
                SupportsMetadata = true,
                SupportsDirectories = true,
                SupportsRecursiveOperations = true,
                SupportsStreaming = true,
                SupportsPartialReads = true,
                SupportsFileLocking = true,
                SupportsVersioning = false,
                SupportsServerSideCopy = true,
                SupportsServerSideMove = true,
                SupportsBatchOperations = false,
                SupportsSearch = false,
                SupportsAcls = true,
                SupportsEncryption = false,
                SupportsCompression = false
            };
        }

        /// <summary>
        /// Creates capabilities for an S3 provider
        /// </summary>
        public static ProviderCapabilities ForS3()
        {
            return new ProviderCapabilities
            {
                SupportsAtomicRename = false,
                SupportsSymlinks = false,
                SupportsHardLinks = false,
                SupportsMetadata = true,
                SupportsDirectories = false, // S3 uses key prefixes, not true directories
                SupportsRecursiveOperations = true,
                SupportsStreaming = true,
                SupportsPartialReads = true,
                SupportsFileLocking = false,
                SupportsVersioning = true,
                SupportsServerSideCopy = true,
                SupportsServerSideMove = false,
                SupportsBatchOperations = true,
                SupportsSearch = false,
                SupportsAcls = true,
                SupportsEncryption = true,
                SupportsCompression = false,
                MaxFileSize = 5L * 1024 * 1024 * 1024 * 1024 // 5 TB
            };
        }

        /// <summary>
        /// Creates capabilities for a Google Drive provider
        /// </summary>
        public static ProviderCapabilities ForGoogleDrive()
        {
            return new ProviderCapabilities
            {
                SupportsAtomicRename = false,
                SupportsSymlinks = false,
                SupportsHardLinks = false,
                SupportsMetadata = true,
                SupportsDirectories = true,
                SupportsRecursiveOperations = true,
                SupportsStreaming = true,
                SupportsPartialReads = true,
                SupportsFileLocking = false,
                SupportsVersioning = true,
                SupportsServerSideCopy = true,
                SupportsServerSideMove = true,
                SupportsBatchOperations = true,
                SupportsSearch = true,
                SupportsAcls = true,
                SupportsEncryption = true,
                SupportsCompression = false,
                MaxFileSize = 5L * 1024 * 1024 * 1024 * 1024 // 5 TB
            };
        }

        /// <summary>
        /// Creates capabilities for an FTP/FTPS provider
        /// </summary>
        public static ProviderCapabilities ForFtp()
        {
            return new ProviderCapabilities
            {
                SupportsAtomicRename = true,
                SupportsSymlinks = false,
                SupportsHardLinks = false,
                SupportsMetadata = true,
                SupportsDirectories = true,
                SupportsRecursiveOperations = true,
                SupportsStreaming = true,
                SupportsPartialReads = false,
                SupportsFileLocking = false,
                SupportsVersioning = false,
                SupportsServerSideCopy = false,
                SupportsServerSideMove = true,
                SupportsBatchOperations = false,
                SupportsSearch = false,
                SupportsAcls = false,
                SupportsEncryption = false,
                SupportsCompression = false
            };
        }

        /// <summary>
        /// Creates capabilities for a WebDAV provider
        /// </summary>
        public static ProviderCapabilities ForWebDAV()
        {
            return new ProviderCapabilities
            {
                SupportsAtomicRename = false,
                SupportsSymlinks = false,
                SupportsHardLinks = false,
                SupportsMetadata = true,
                SupportsDirectories = true,
                SupportsRecursiveOperations = true,
                SupportsStreaming = true,
                SupportsPartialReads = true,
                SupportsFileLocking = true,
                SupportsVersioning = false,
                SupportsServerSideCopy = true,
                SupportsServerSideMove = true,
                SupportsBatchOperations = false,
                SupportsSearch = false,
                SupportsAcls = true,
                SupportsEncryption = false,
                SupportsCompression = false
            };
        }
    }
}


using Microsoft.AspNetCore.StaticFiles;

namespace FormCMS.Infrastructure.FileStore;

public record LocalFileStoreOptions(string PathPrefix, string UrlPrefix);

public class LocalFileStore(
    LocalFileStoreOptions options
) : IFileStore
{
    private readonly FileExtensionContentTypeProvider _provider = new();

    public Task Upload(string localPath, string path, CancellationToken ct)
    {
        var dest = Path.Join(options.PathPrefix, path);
        if (File.Exists(localPath))
        {
            CreateDirAndCopy(localPath, dest);
        }

        return Task.CompletedTask;
    }

    public async Task Upload(IEnumerable<(string, IFormFile)> files, CancellationToken ct)
    {
        var set = new HashSet<string>();
        foreach (var (fileName, file) in files)
        {
            var dest = Path.Join(options.PathPrefix, fileName);
            var dir = Path.GetDirectoryName(dest);

            if (!string.IsNullOrEmpty(dir) && !set.Contains(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                set.Add(dir);
            }

            await using var fileStream = new FileStream(dest, FileMode.Create, FileAccess.Write);
            await file.CopyToAsync(fileStream);
        }
    }


    public Task<FileMetadata?> GetMetadata(string filePath, CancellationToken ct)
    {
        string fullPath = Path.Join(options.PathPrefix, filePath);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<FileMetadata?>(null);
        }

        var sysFileInfo = new FileInfo(fullPath);
        var ret = new FileMetadata(sysFileInfo.Length, GetContentType(filePath));
        return Task.FromResult<FileMetadata?>(ret);
    }

    public string GetUrl(string file) => Path.Join(options.UrlPrefix, file);

    public Task Download(string path, string localPath, CancellationToken ct)
    {
        path = Path.Join(options.PathPrefix, path);
        if (File.Exists(path))
        {
            CreateDirAndCopy(path, localPath);
        }

        return Task.CompletedTask;
    }

    public async Task DownloadFileWithRelated(string path, string localPath, CancellationToken ct)
    {
        var sourceRoot = options.PathPrefix.TrimEnd('/');
        var fullFilePath = Path.Join(sourceRoot, path);
        await Download(path, localPath, ct);
        

        var parentDir = Path.GetDirectoryName(fullFilePath) ?? sourceRoot;
        var filePrefix = Path.GetFileName(path);
        var matchingDirs = Directory.EnumerateDirectories(parentDir, $"{filePrefix}*", SearchOption.TopDirectoryOnly);

        foreach (var matchingDir in matchingDirs)
        {
            var files = Directory.GetFiles(matchingDir, "*", SearchOption.AllDirectories);

            foreach (var sourceFile in files)
            {
                ct.ThrowIfCancellationRequested();

                var relativePath = Path.GetRelativePath(fullFilePath, sourceFile);
                var destPath = Path.Combine(localPath, relativePath);
                CreateDirAndCopy(sourceFile, destPath);
            }
        }

    }


    public Task Del(string file, CancellationToken ct)
    {
        file = Path.Join(options.PathPrefix, file);
        File.Delete(file);
        return Task.CompletedTask;
    }
    
    public Task DelByPrefix(string prefix, CancellationToken ct)
    {
        var fullPrefixPath = Path.Combine(options.PathPrefix, prefix);

        if (Directory.Exists(fullPrefixPath))
        {
            Directory.Delete(fullPrefixPath, recursive: true);
        }

        return Task.CompletedTask;
    }

    private string GetContentType(string filePath)
        => _provider.TryGetContentType(filePath, out var contentType)
            ? contentType
            : "application/octet-stream"; // Default fallback


    private void CreateDirAndCopy(string source, string dest)
    {
        FileUtils.EnsureParentFolder(dest);
        File.Copy(source, dest, true);
    }
}
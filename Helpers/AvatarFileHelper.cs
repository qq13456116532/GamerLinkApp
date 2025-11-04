using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace GamerLinkApp.Helpers;

public static class AvatarFileHelper
{
    private const string AvatarDirectoryName = "avatars";

    public static async Task<string> SaveAvatarFileAsync(FileResult file)
    {
        ArgumentNullException.ThrowIfNull(file);

        var extension = Path.GetExtension(file.FileName);
        extension = string.IsNullOrWhiteSpace(extension) ? ".png" : extension.ToLowerInvariant();

        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" };
        if (Array.IndexOf(allowedExtensions, extension) < 0)
        {
            throw new InvalidOperationException("仅支持常见的图片格式（jpg/png/gif/bmp/webp）。");
        }

        var avatarsDirectory = Path.Combine(FileSystem.AppDataDirectory, AvatarDirectoryName);
        Directory.CreateDirectory(avatarsDirectory);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var destinationPath = Path.Combine(avatarsDirectory, fileName);

        await using var sourceStream = await file.OpenReadAsync();
        await using var destinationStream = File.Create(destinationPath);
        await sourceStream.CopyToAsync(destinationStream);

        return destinationPath;
    }

    public static void DeleteLocalAvatarFileIfOwned(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            var avatarsDirectory = Path.Combine(FileSystem.AppDataDirectory, AvatarDirectoryName);
            if (!path.StartsWith(avatarsDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Avatar cleanup failed: {ex}");
        }
    }
}

namespace BookShopMVC.Shared;
//this file used to manage the file operations like saving and deleting files(images) like we should delete the image from the solution instead of just deleting it from the database
public interface IFileService
{
    void DeleteFile(string fileName);
    Task<string> SaveFile(IFormFile file, string[] allowedExtensions);
}

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _environment;
    public FileService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> SaveFile(IFormFile file, string[] allowedExtensions)
    {
        //WebRootPth is wwwroot folder
        var wwwPath = _environment.WebRootPath;
        //the path will be wwwroot/images
        var path = Path.Combine(wwwPath, "images");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        var extension = Path.GetExtension(file.FileName);
        if (!allowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"Only {string.Join(",", allowedExtensions)} files allowed");
        }
        string fileName = $"{Guid.NewGuid()}{extension}";
        string fileNameWithPath = Path.Combine(path, fileName);
        //the stream will close automatically after using block when it's not needed
        using var stream = new FileStream(fileNameWithPath, FileMode.Create);
        await file.CopyToAsync(stream);
        return fileName;
    }

    public void DeleteFile(string fileName)
    {
        var wwwPath = _environment.WebRootPath;
        var fileNameWithPath = Path.Combine(wwwPath, "images\\", fileName);
        if (!File.Exists(fileNameWithPath))
            throw new FileNotFoundException(fileName);
        File.Delete(fileNameWithPath);

    }
}
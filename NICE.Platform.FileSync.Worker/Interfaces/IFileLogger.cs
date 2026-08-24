namespace NICE.Platform.FileSync.Worker.Interfaces;

internal interface IFileLogger
{
    void Log(string message, bool isError = false);
}

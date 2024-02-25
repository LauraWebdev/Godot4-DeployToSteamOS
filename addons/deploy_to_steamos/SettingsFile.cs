public class SettingsFile
{
    public enum UploadMethods
    {
        Differential,
        Incremental,
        CleanReplace
    }
    
    public string BuildPath { get; set; } = "";
    public string StartParameters { get; set; } = "";
    public UploadMethods UploadMethod { get; set; } = UploadMethods.Differential;
}
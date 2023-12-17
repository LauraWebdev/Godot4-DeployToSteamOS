public class SettingsFile
{
    public enum UploadMethods
    {
        Differential,
        Incremental,
        CleanReplace
    }
    
    public string BuildPath = "";
    public string StartParameters = "";
    public UploadMethods UploadMethod = UploadMethods.Differential;
}
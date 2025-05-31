using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public sealed class FileCompareTask : Task
{
    [Required]
    public string Source { get; set; }

    [Required]
    public string Target { get; set; }

    public override bool Execute()
    {
        try
        {
            if (!File.Exists(Source))
            {
                Log.LogError($"File not found: {Source}");
                return false;
            }

            if (!File.Exists(Target))
            {
                Log.LogError($"File not found: {Target}");
                return false;
            }

            var content1 = System.IO.File.ReadAllText(Source);
            var content2 = System.IO.File.ReadAllText(Target);

            if (content1 != content2)
            {
                Log.LogError("Error: contents do not match. Expected:\n" +
                             content1 + "\n\n" +
                             "Actual:\n" + content2);

                return false; // This causes the task to fail
            }

            return true;
        }
        catch (IOException ex)
        {
            Log.LogErrorFromException(ex);
            return false;
        }
    }
}

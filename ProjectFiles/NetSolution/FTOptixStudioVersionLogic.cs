#region Using directives
using System.IO;
using FTOptix.NetLogic;
using FTOptix.UI;
#endregion

public class FTOptixStudioVersionLogic : BaseNetLogic
{
    public override void Start()
    {
        string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        string strWorkPath = Path.GetDirectoryName(strExeFilePath);
        string versionFile = strWorkPath + Path.DirectorySeparatorChar
            + ".." + Path.DirectorySeparatorChar
            + ".." + Path.DirectorySeparatorChar
            + ".." + Path.DirectorySeparatorChar
            + "IDEVersion.txt";

        var fileStream = new FileStream(versionFile, FileMode.Open, FileAccess.Read);
        // Read the source file into a byte array.
        byte[] bytes = new byte[fileStream.Length];
        int numBytesToRead = (int)fileStream.Length;
        int numBytesRead = 0;
        while (numBytesToRead > 0)
        {
            // Read may return anything from 0 to numBytesToRead.
            var n = fileStream.Read(bytes, numBytesRead, numBytesToRead);

            // Break when the end of the file is reached.
            if (n == 0)
                break;

            numBytesRead += n;
            numBytesToRead -= n;
        }

        var versionString = System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);

        Label label = Owner as Label;
        label.Text = versionString;
    }

    public override void Stop()
    {
    }
}

using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using MimeMapping;
// ReSharper disable MustUseReturnValue

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ReplaceSubstringWithRangeIndexer

namespace END.Embedded;

public static class Prop
{
    private static Hashtable? _props;
    private static readonly object Padlock = new();

    public const string SessionKeyUser = "PROP_SESSIONKEY_USER";
    public const string SessionKeyUserid = "PROP_SESSIONKEY_USERID";
    private const string ResourceDll = "END.Embedded";

    [DebuggerStepThrough]
    private static string GetResourcePath()
    {
        return string.Concat(ResourceDll, ".", "resources", ".");
    }
    
    public static string GetEmbeddedResource(string folder, string fileName)
    {
        var fileStr = string.Concat(folder, ".", fileName);
        var fileData = GetEmbeddedFile(fileStr);

        if (fileData == null)
            return "";

        var ret = System.Text.Encoding.UTF8.GetString(fileData);
        ret = Regex.Replace(ret, "\\p{C}+", string.Empty);

        return ret;
    }

    public static string GetEmbeddedString(string fileName)
    {
        var fileData = GetEmbeddedFile(fileName);

        if (fileData == null)
            return "";

        var ret = System.Text.Encoding.UTF8.GetString(fileData);
        ret = Regex.Replace(ret, "\\p{C}+", string.Empty);

        return ret;
    }

    public static string GetEmbeddedFile(ref HttpResponse response, string fileName, bool isDefault = false)
    {
        var fileData = GetEmbeddedFile(fileName);
        var contentType = MimeUtility.GetMimeMapping(fileName);

        if (fileData == null) return contentType;
        
        response.ContentType = contentType;
        response.BinaryWrite(fileData);

        return contentType;
    }
    
    private static byte[]? GetEmbeddedFile(string fileName)
    {
        var appDomain = HttpRuntime.AppDomainAppVirtualPath;
        var context = HttpContext.Current;
        if (context == null) return null;
        var appPath = context.Server.MapPath(appDomain);
        
        // ReSharper disable once IdentifierTypo
        var assyFile = Path.Combine(appPath, "bin", string.Concat(ResourceDll,  ".dll"));

        var a = Assembly.LoadFrom(assyFile);
        var resName = string.Concat(GetResourcePath(), fileName);
        byte[]? fileData = null;
        var resFilestream = a.GetManifestResourceStream(resName);

        if (resFilestream == null) return fileData;

        using var br = new BinaryReader(resFilestream);
        fileData = new byte[resFilestream.Length];
        resFilestream.Read(fileData, 0, fileData.Length);
        br.Close();

        return fileData;
    }

    public static void LoadEmbeddedProps()
    {
        var appDomain = HttpRuntime.AppDomainAppVirtualPath;
        var context = HttpContext.Current;
        if (context == null) return;
        var appPath = context.Server.MapPath(appDomain);
        // ReSharper disable once IdentifierTypo
        var assyFile = Path.Combine(appPath, "bin", string.Concat(ResourceDll, ".dll"));
        var a = Assembly.LoadFrom(assyFile);
        var files = a.GetManifestResourceNames();

        foreach (var file in files)
        {
            if (!file.Contains("resources"))
                continue;

            byte[]? fileData = null;
            var resFilestream = a.GetManifestResourceStream(file);
            var fileName = file.Substring(file.IndexOf(@"resources", StringComparison.Ordinal) + 10);
            
            if (resFilestream != null)
            {
                using var br = new BinaryReader(resFilestream);
                fileData = new byte[resFilestream.Length];
                resFilestream.Read(fileData, 0, fileData.Length);
                br.Close();
            }

            if (fileData == null) continue;
            var fileContent = System.Text.Encoding.UTF8.GetString(fileData);
            fileContent = Regex.Replace(fileContent, "\\p{C}+", string.Empty);

            _props?.Add(fileName.ToLower(), fileContent);
        }
    }

    private static void LoadAllProps()
    {
        _props = new Hashtable();
        //String baseDir = HttpContext.Current.Server.MapPath( "~/resources" );
        var baseDir = HttpRuntime.AppDomainAppPath + "resources";

        LoadEmbeddedProps();
        if (!Directory.Exists(baseDir)) return;
        LoadBaseDirProps(baseDir);
        LoadSubDirProps(baseDir);
    }

    public static void LoadBaseDirProps(string inBaseRootDir)
    {
        var files = Directory.GetFiles(inBaseRootDir);
        foreach (var file in files)
        {
            if (_props == null) continue;
            if (_props.ContainsKey(file)) continue; // only one key per prop (1st wins)
            StoreFileContents(file);
        }
    }

    private static void LoadSubDirProps(string inDir)
    {
        var directories = Directory.GetDirectories(inDir);
        foreach (var directory in directories)
        {
            var files = Directory.GetFiles(directory);
            foreach (var file in files)
            {
                if (_props == null) continue;
                if (_props.ContainsKey(file)) continue; // only one key per prop (1st wins)
                StoreFileContents(file);
            }

            LoadSubDirProps(directory); // recurse.
        }
    }

    public static string StoreFileContents(string inFilePath)
    {
        var fileName = inFilePath;
        using var sr = new StreamReader(inFilePath);
        fileName = fileName.Substring(fileName.IndexOf(@"resources\", StringComparison.Ordinal) + 10);
        
        var fileContents = File.ReadAllText(inFilePath);
        if (_props == null) return fileContents;
        
        if (!_props.Contains(fileName.ToLower().Replace("\\", ".")))
            _props.Add(fileName.ToLower().Replace("\\","."), fileContents);
        return fileContents;
    }

    public static void RefreshProps()
    {
        _props = null;
        lock (Padlock)
        {
            LoadAllProps();
        } // "padlock" mutex
    }

    [DebuggerStepThrough]
    public static string GetProperty(string inProp)
    {
        if (_props == null)
            RefreshProps();

        if (_props == null)
            return string.Empty;

        var propKey = inProp.Replace("\\", ".");

        if (!_props.ContainsKey(propKey.ToLower()))
            return string.Empty;
                
        var ret = (string?)_props[propKey.ToLower()];

        return ret ?? string.Empty;
    }

    public static string[,] GetPropNameArray()
    {
        _props ??= new Hashtable();

        var retArr = new string[_props.Count, 2];
        var al = new ArrayList();

        foreach (DictionaryEntry pName in _props)
        {
            al.Add(pName.Key.ToString());
        }

        al.Sort();
        for (var index = 0; index < al.Count; index++)
        {
            var value = (string?)al[index];
            if (value == null) continue;
            retArr[index, 0] = value;
            retArr[index, 1] = value;
        }

        return retArr;
    }
    
}
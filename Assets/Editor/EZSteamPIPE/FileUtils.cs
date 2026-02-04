using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public static class FileUtils
{
    public static string ReadScriptTextByName(string className)
    {
        var guids = AssetDatabase.FindAssets(className + " t:Script");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(className + ".cs", StringComparison.OrdinalIgnoreCase)) continue;

            var fullPath = ToFullPath(path);
            try
            {
                return File.ReadAllText(fullPath);
            }
            catch { }
        }
        return string.Empty;
    }

    public static string ToFullPath(string assetRelativePath)
    {
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, assetRelativePath.Replace('/', Path.DirectorySeparatorChar));
    }
    public static string FindFileByClass(string className)
    {
        var guids = AssetDatabase.FindAssets(className + " t:Script");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(className + ".cs", StringComparison.OrdinalIgnoreCase))
            {
                return ToFullPath(path);
            }
        }
        return null;
    }
}

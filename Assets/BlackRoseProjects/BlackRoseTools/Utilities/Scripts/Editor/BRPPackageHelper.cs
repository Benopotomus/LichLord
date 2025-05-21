using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("BRPUtilities.Editor", AllInternalsVisible = true)]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("MeshUtility.Editor", AllInternalsVisible = false)]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("InstancedAnimationSystem.Editor", AllInternalsVisible = true)]
namespace BlackRoseProjects.Utility
{
    internal static class BRPPackageHelper
    {
        static bool IsWorking;
        static AddRequest Request;
        static AddAndRemoveRequest ComplexRequest;
        static string packageName;

        public static void InstallPackage(string packageName)
        {
            if (Request != null || IsWorking)
                return;
            BRPPackageHelper.packageName = packageName;
            EditorUtility.DisplayProgressBar("Installing Package", "Downloading package", 0.0f);
            IsWorking = true;
            Request = Client.Add(packageName);
            EditorApplication.update += ProgressDownload;
        }

        public static void InstallPackages(string[] packageName)
        {
            if (Request != null || IsWorking)
                return;
            EditorUtility.DisplayProgressBar("Installing Packages", "Downloading packages", 0.0f);
            IsWorking = true;
            ComplexRequest = Client.AddAndRemove(packagesToAdd: packageName);
            EditorApplication.update += ProgressDownloadMultiple;
        }
        static void ProgressDownloadMultiple()
        {
            if (ComplexRequest.IsCompleted)
            {
                EditorUtility.ClearProgressBar();
                if (ComplexRequest.Status == StatusCode.Success)
                {
                    EditorUtility.DisplayDialog("Package installer", "Packages downloaded!", "OK");
                }
                else if (ComplexRequest.Status >= StatusCode.Failure)
                    Debug.Log(ComplexRequest.Error.message);
                ComplexRequest = null;
                EditorApplication.update -= ProgressDownloadMultiple;
                IsWorking = false;
            }
        }

        static void ProgressDownload()
        {
            if (Request.IsCompleted)
            {
                EditorUtility.ClearProgressBar();
                if (Request.Status == StatusCode.Success)
                {
                    EditorUtility.DisplayDialog("Package installer", "Packages downloaded!", "OK");
                }
                else if (Request.Status >= StatusCode.Failure)
                    Debug.Log(Request.Error.message);
                Request = null;
                EditorApplication.update -= ProgressDownload;
                IsWorking = false;
            }
        }
    }
}
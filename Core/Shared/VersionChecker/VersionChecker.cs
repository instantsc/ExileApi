﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ExileCore.Shared.VersionChecker
{
    public class VersionChecker
    {
        public const string VERSION_FILE_NAME = "version.json";
        private const string VERSION_LATEST_URL = @"https://api.github.com/repos/Queuete/ExileApi/releases/latest";
        public VersionResult VersionResult { get; private set; }
        public VersionJson? LocalVersion { get; private set; }
        public VersionJson? LatestVersion { get; private set; }
        public AutoUpdate AutoUpdate { get; }

        public VersionChecker()
        {
            VersionResult = VersionResult.Loading;
            AutoUpdate = new AutoUpdate();
        }

        public void CheckVersionAndPrepareUpdate(bool autoPrepareUpdate)
        {
            var updateCallback = (autoPrepareUpdate) ? new Action<GithubReleaseResponse>(AutoUpdate.PrepareUpdate) : null;
            Task.Run(() => HandleVersion(updateCallback));
        }

        private void HandleVersion(Action<GithubReleaseResponse> updateCallback = null)
        {
            LocalVersion = LoadLocalVersion(VERSION_FILE_NAME);
            if (LocalVersion == null)
            {
                VersionResult = VersionResult.Error;
                return;
            }

            var remoteVersionTask = Task.Run(() => LoadLatestVersion());
            remoteVersionTask.Wait();
            var remoteVersionResponse = remoteVersionTask.Result;
            if (remoteVersionResponse == null) return;

            LatestVersion = ConvertStringToVersionJson(remoteVersionResponse.Value.VersionString);
            if (LatestVersion == null)
            {
                VersionResult = VersionResult.Error;
                DebugWindow.LogError($"VersionChecker -> String from remote version cant be converted: {remoteVersionResponse.Value.VersionString}");
                return;
            }

            VersionResult = VersionComparison(LocalVersion.Value, LatestVersion.Value);
            if (VersionResult.IsUpdateAvailable())
            {
                DebugWindow.LogMsg($"VersionChecker -> Update Available");
                updateCallback?.Invoke(remoteVersionResponse.Value);
            }
        }

        public static VersionResult VersionComparison(VersionJson LocalVersion, VersionJson LatestVersion)
        {
            if (LocalVersion.Major < LatestVersion.Major)
            {
                return VersionResult.MajorUpdate;
            }
            else if (LocalVersion.Minor < LatestVersion.Minor)
            {
                return VersionResult.MinorUpdate;
            }
            else if (LocalVersion.Patch < LatestVersion.Patch)
            {
                return VersionResult.PatchUpdate;
            }
            return VersionResult.UpToDate;
        }

        public static VersionJson? LoadLocalVersion(string versionFile)
        {
            if (!File.Exists(versionFile))
            {
                DebugWindow.LogError($"VersionChecker -> Version file not found: {versionFile}");
                return null;
            }
            var readAllText = File.ReadAllText(versionFile);
            return JsonConvert.DeserializeObject<VersionJson>(readAllText);
        }

        public GithubReleaseResponse? LoadLatestVersion()
        {
            string content = string.Empty;
            string url = VERSION_LATEST_URL;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = "ExileApi";
            request.AutomaticDecompression = DecompressionMethods.GZip;

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    content = reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                DebugWindow.LogError($"VersionChecker -> Remote version url not reachable, Exception: {e}");
                return null;
            }

            return JsonConvert.DeserializeObject<GithubReleaseResponse>(content);
        }

        public static VersionJson? ConvertStringToVersionJson(string input)
        {
            if (input.Length < 5) return null;
            var slices = input.Split('.');
            if (slices.Length != 3) return null;

            int major, minor, patch;
            if (int.TryParse(slices[0], out major) == false
                || int.TryParse(slices[1], out minor) == false
                || int.TryParse(slices[2], out patch) == false)
            {
                return null;
            }

            return new VersionJson() { Major = major, Minor = minor, Patch = patch };
        }
    }
}

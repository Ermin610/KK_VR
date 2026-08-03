using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KKCharaStudioVR;

[Flags]
internal enum VRVmdContent
{
    None = 0,
    Motion = 1,
    Morph = 2,
    Camera = 4
}

internal struct VRVmdMetadata
{
    public VRVmdContent Content;
    public uint MotionFrames;
    public uint MorphFrames;
    public uint CameraFrames;

    public bool HasActorData => (Content & (VRVmdContent.Motion | VRVmdContent.Morph)) != 0;
    public bool HasCameraData => (Content & VRVmdContent.Camera) != 0;
}

internal sealed class VRWristFileEntry
{
    public string DisplayName;
    public string FullPath;
    public bool IsDirectory;
    public bool IsSkipAction;
    public bool IsActorTarget;
    public int ObjectKey;
    public bool HasVmdMetadata;
    public VRVmdMetadata VmdMetadata;
}

internal static class VRWristFileCatalog
{
    private const int RelatedFileScanLimit = 512;

    public static List<VRWristFileEntry> ListDrives()
    {
        List<VRWristFileEntry> entries = new List<VRWristFileEntry>();
        DriveInfo[] drives;
        try
        {
            drives = DriveInfo.GetDrives();
        }
        catch (Exception)
        {
            return entries;
        }

        Array.Sort(drives, (left, right) =>
            StringComparer.OrdinalIgnoreCase.Compare(left.Name, right.Name));
        foreach (DriveInfo drive in drives)
        {
            try
            {
                if (!drive.IsReady)
                    continue;

                entries.Add(new VRWristFileEntry
                {
                    DisplayName = drive.Name,
                    FullPath = drive.RootDirectory.FullName,
                    IsDirectory = true
                });
            }
            catch (Exception)
            {
                // Disconnected removable and network drives should not hide usable disks.
            }
        }
        return entries;
    }

    public static List<VRWristFileEntry> ListDirectories(string directory)
    {
        List<VRWristFileEntry> entries = new List<VRWristFileEntry>();
        try
        {
            string fullDirectory = Path.GetFullPath(directory);
            if (!Directory.Exists(fullDirectory))
                return entries;

            string[] directories = Directory.GetDirectories(fullDirectory);
            Array.Sort(directories, StringComparer.CurrentCultureIgnoreCase);
            foreach (string child in directories)
            {
                entries.Add(new VRWristFileEntry
                {
                    DisplayName = Path.GetFileName(child),
                    FullPath = child,
                    IsDirectory = true
                });
            }
        }
        catch (Exception)
        {
            entries.Clear();
        }
        return entries;
    }

    public static List<VRWristFileEntry> ListDirectory(
        string root,
        string directory,
        string[] extensions,
        bool inspectVmd,
        Func<string, bool> fileFilter,
        bool filesBeforeDirectories)
    {
        List<VRWristFileEntry> entries = new List<VRWristFileEntry>();
        List<VRWristFileEntry> directoryEntries = new List<VRWristFileEntry>();
        List<VRWristFileEntry> fileEntries = new List<VRWristFileEntry>();
        string safeRoot;
        string safeDirectory;
        if (!TryResolveDirectory(root, directory, out safeRoot, out safeDirectory))
            return entries;

        try
        {
            string[] directories = Directory.GetDirectories(safeDirectory);
            Array.Sort(directories, StringComparer.CurrentCultureIgnoreCase);
            foreach (string child in directories)
            {
                directoryEntries.Add(new VRWristFileEntry
                {
                    DisplayName = Path.GetFileName(child),
                    FullPath = child,
                    IsDirectory = true
                });
            }

            string[] files = Directory.GetFiles(safeDirectory);
            Array.Sort(files, CompareFilesNewestFirst);
            foreach (string file in files)
            {
                if (!HasExtension(file, extensions))
                    continue;
                if (fileFilter != null && !fileFilter(file))
                    continue;

                VRWristFileEntry entry = new VRWristFileEntry
                {
                    DisplayName = Path.GetFileNameWithoutExtension(file),
                    FullPath = file
                };

                if (inspectVmd)
                {
                    VRVmdMetadata metadata;
                    if (!TryReadVmdMetadata(file, out metadata))
                        continue;
                    entry.HasVmdMetadata = true;
                    entry.VmdMetadata = metadata;
                }

                fileEntries.Add(entry);
            }

            if (filesBeforeDirectories)
            {
                entries.AddRange(fileEntries);
                entries.AddRange(directoryEntries);
            }
            else
            {
                entries.AddRange(directoryEntries);
                entries.AddRange(fileEntries);
            }
        }
        catch (Exception)
        {
            entries.Clear();
        }

        return entries;
    }

    public static List<VRWristFileEntry> FindRelatedCameraFiles(string motionPath)
    {
        List<VRWristFileEntry> entries = new List<VRWristFileEntry>();
        if (string.IsNullOrEmpty(motionPath))
            return entries;

        string baseDirectory = Path.GetDirectoryName(Path.GetFullPath(motionPath));
        List<string> files = FindRelatedFiles(baseDirectory, new[] { ".vmd" });
        foreach (string file in files)
        {
            if (string.Equals(file, motionPath, StringComparison.OrdinalIgnoreCase))
                continue;

            VRVmdMetadata metadata;
            if (!TryReadVmdMetadata(file, out metadata) || !metadata.HasCameraData)
                continue;

            string relativeName = GetRelativeDisplayPath(baseDirectory, file);
            entries.Add(new VRWristFileEntry
            {
                DisplayName = relativeName,
                FullPath = file,
                HasVmdMetadata = true,
                VmdMetadata = metadata
            });
        }

        entries.Sort((left, right) =>
            StringComparer.CurrentCultureIgnoreCase.Compare(left.DisplayName, right.DisplayName));
        return entries;
    }

    public static string FindBestRelatedAudio(string vmdPath)
    {
        if (string.IsNullOrEmpty(vmdPath))
            return null;

        string baseDirectory = Path.GetDirectoryName(Path.GetFullPath(vmdPath));
        List<string> audioFiles = FindRelatedFiles(baseDirectory, new[] { ".wav", ".mp3", ".ogg" });
        if (audioFiles.Count == 0)
            return null;
        if (audioFiles.Count == 1)
            return audioFiles[0];

        string motionStem = NormalizeMediaStem(Path.GetFileNameWithoutExtension(vmdPath));
        string motionDirectory = Path.GetDirectoryName(vmdPath);
        string best = audioFiles[0];
        int bestScore = int.MinValue;

        foreach (string audio in audioFiles)
        {
            string audioStem = NormalizeMediaStem(Path.GetFileNameWithoutExtension(audio));
            int score = LongestCommonSubstringLength(motionStem, audioStem) * 6
                + CommonPrefixLength(motionStem, audioStem) * 2;
            if (motionStem.Length > 0 && string.Equals(motionStem, audioStem, StringComparison.Ordinal))
                score += 1000;
            else if (motionStem.Length > 0
                && audioStem.Length > 0
                && (motionStem.Contains(audioStem) || audioStem.Contains(motionStem)))
            {
                score += 400 + Math.Min(motionStem.Length, audioStem.Length);
            }
            if (string.Equals(motionDirectory, Path.GetDirectoryName(audio), StringComparison.OrdinalIgnoreCase))
                score += 40;
            if (string.Equals(Path.GetExtension(audio), ".wav", StringComparison.OrdinalIgnoreCase))
                score += 8;

            if (score > bestScore)
            {
                best = audio;
                bestScore = score;
            }
        }

        return best;
    }

    public static bool TryReadVmdMetadata(string path, out VRVmdMetadata metadata)
    {
        metadata = new VRVmdMetadata();
        try
        {
            using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using BinaryReader reader = new BinaryReader(stream);
            if (stream.Length < 64)
                return false;

            string header = Encoding.ASCII.GetString(reader.ReadBytes(30)).TrimEnd('\0');
            if (!header.StartsWith("Vocaloid Motion Data", StringComparison.Ordinal))
                return false;

            int modelNameLength = header.StartsWith("Vocaloid Motion Data 0002", StringComparison.Ordinal)
                ? 20
                : 10;
            if (reader.ReadBytes(modelNameLength).Length != modelNameLength)
                return false;

            uint motionFrames;
            if (!TryReadFrameCount(reader, 111, out motionFrames))
                return false;

            uint morphFrames;
            if (!TryReadFrameCount(reader, 23, out morphFrames))
                return false;

            uint cameraFrames;
            if (!TryReadFrameCount(reader, 61, out cameraFrames))
                return false;

            VRVmdContent content = VRVmdContent.None;
            if (motionFrames > 0)
                content |= VRVmdContent.Motion;
            if (morphFrames > 0)
                content |= VRVmdContent.Morph;
            if (cameraFrames > 0)
                content |= VRVmdContent.Camera;
            if (content == VRVmdContent.None)
                return false;

            metadata = new VRVmdMetadata
            {
                Content = content,
                MotionFrames = motionFrames,
                MorphFrames = morphFrames,
                CameraFrames = cameraFrames
            };
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static bool IsInsideRoot(string root, string path)
    {
        try
        {
            string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.Equals(fullRoot, fullPath, StringComparison.OrdinalIgnoreCase))
                return true;
            return fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool TryResolveDirectory(
        string root,
        string directory,
        out string safeRoot,
        out string safeDirectory)
    {
        safeRoot = null;
        safeDirectory = null;
        try
        {
            safeRoot = Path.GetFullPath(root);
            safeDirectory = Path.GetFullPath(directory);
            return Directory.Exists(safeRoot)
                && Directory.Exists(safeDirectory)
                && IsInsideRoot(safeRoot, safeDirectory);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool TryReadFrameCount(BinaryReader reader, int recordSize, out uint count)
    {
        count = 0;
        Stream stream = reader.BaseStream;
        if (stream.Length - stream.Position < 4)
            return false;

        count = reader.ReadUInt32();
        long remaining = stream.Length - stream.Position;
        ulong bytesToSkip = (ulong)count * (ulong)recordSize;
        if (bytesToSkip > (ulong)remaining)
            return false;

        stream.Seek((long)bytesToSkip, SeekOrigin.Current);
        return true;
    }

    private static bool HasExtension(string path, string[] extensions)
    {
        if (extensions == null || extensions.Length == 0)
            return true;

        string extension = Path.GetExtension(path);
        foreach (string candidate in extensions)
        {
            if (string.Equals(extension, candidate, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static int CompareFilesNewestFirst(string left, string right)
    {
        int dateComparison = File.GetLastWriteTimeUtc(right).CompareTo(File.GetLastWriteTimeUtc(left));
        return dateComparison != 0
            ? dateComparison
            : StringComparer.CurrentCultureIgnoreCase.Compare(left, right);
    }

    private static List<string> FindRelatedFiles(string baseDirectory, string[] extensions)
    {
        List<string> result = new List<string>();
        if (string.IsNullOrEmpty(baseDirectory) || !Directory.Exists(baseDirectory))
            return result;

        Queue<string> pending = new Queue<string>();
        pending.Enqueue(baseDirectory);
        while (pending.Count > 0 && result.Count < RelatedFileScanLimit)
        {
            string directory = pending.Dequeue();
            try
            {
                string[] files = Directory.GetFiles(directory);
                Array.Sort(files, StringComparer.CurrentCultureIgnoreCase);
                foreach (string file in files)
                {
                    if (HasExtension(file, extensions))
                    {
                        result.Add(file);
                        if (result.Count >= RelatedFileScanLimit)
                            break;
                    }
                }

                string[] children = Directory.GetDirectories(directory);
                Array.Sort(children, StringComparer.CurrentCultureIgnoreCase);
                foreach (string child in children)
                    pending.Enqueue(child);
            }
            catch (Exception)
            {
                // A single inaccessible folder should not hide the other related files.
            }
        }

        return result;
    }

    private static string GetRelativeDisplayPath(string baseDirectory, string path)
    {
        string prefix = baseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        string display = path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? path.Substring(prefix.Length)
            : Path.GetFileName(path);
        return Path.ChangeExtension(display, null);
    }

    private static string NormalizeMediaStem(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        StringBuilder builder = new StringBuilder(value.Length);
        foreach (char character in value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
                builder.Append(character);
        }

        return builder
            .Replace("camera", string.Empty)
            .Replace("motion", string.Empty)
            .Replace("audio", string.Empty)
            .Replace("sound", string.Empty)
            .Replace("music", string.Empty)
            .Replace("model", string.Empty)
            .Replace("镜头", string.Empty)
            .Replace("動作", string.Empty)
            .Replace("动作", string.Empty)
            .Replace("音频", string.Empty)
            .Replace("音樂", string.Empty)
            .Replace("音乐", string.Empty)
            .ToString();
    }

    private static int CommonPrefixLength(string left, string right)
    {
        int limit = Math.Min(left.Length, right.Length);
        int count = 0;
        while (count < limit && left[count] == right[count])
            count++;
        return count;
    }

    private static int LongestCommonSubstringLength(string left, string right)
    {
        if (left.Length == 0 || right.Length == 0)
            return 0;

        int[] previous = new int[right.Length + 1];
        int best = 0;
        for (int leftIndex = 1; leftIndex <= left.Length; leftIndex++)
        {
            int[] current = new int[right.Length + 1];
            for (int rightIndex = 1; rightIndex <= right.Length; rightIndex++)
            {
                if (left[leftIndex - 1] != right[rightIndex - 1])
                    continue;

                current[rightIndex] = previous[rightIndex - 1] + 1;
                if (current[rightIndex] > best)
                    best = current[rightIndex];
            }
            previous = current;
        }
        return best;
    }
}

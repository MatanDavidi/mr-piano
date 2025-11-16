using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class PartialSongData
{
    public string FileName { get; private set; }
    public string FilePath { get; private set; }
    /// <summary>
    /// The path to this MIDI song's corresponding `.json` file, that contains
    /// the data of its notes, required to display them to the user.
    /// </summary>
    public string JsonPath { get; private set; }
    /// <summary>
    /// The path to this MIDI song's corresponding metadata's `.json` file, that contains
    /// the song's metadata of its notes (see <see cref="ISongMetadata"/> ).
    /// A `.json` file is considered corresponding with a song file `song.midi` if
    /// it has name `song{METADATA_SUFFIX}.json` (see <see cref="SongsFinder.METADATA_SUFFIX"/>) and is 
    /// found within the same directory.
    /// </summary>
    public string MetadataPath { get; private set; }
    /// <summary>
    /// Whether this song's `.midi` file has a corresponding metadata `.json` file.
    /// A `.json` file is considered corresponding with a song file `song.midi` if
    /// it has name `song{METADATA_SUFFIX}.json` (see <see cref="SongsFinder.METADATA_SUFFIX"/>) and is 
    /// found within the same directory
    /// </summary>
    public bool HasCorrespondingMetadata { get; private set; }

    public PartialSongData(string fileName, string filePath, string jsonPath, string metadataPath, bool hasCorrespondingMetadata)
    {
        FileName = fileName;
        FilePath = filePath;
        JsonPath = jsonPath;
        MetadataPath = metadataPath;
        HasCorrespondingMetadata = hasCorrespondingMetadata;
    }
}

public class SongsFinder : MonoBehaviour
{
    public const string METADATA_SUFFIX = "_metadata";

    /// <summary>
    /// Finds MIDI files and their corresponding JSON data on Android/Quest by reading a pre-generated manifest.
    /// </summary>
    private IEnumerator FindMidisAndroid(string streamingAssetsPath, LinkedList<PartialSongData> foundSongs)
    {
        string manifestUrl = Path.Combine(streamingAssetsPath, "file_manifest.json");

        using (UnityWebRequest www = UnityWebRequest.Get(manifestUrl))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to load file manifest: " + www.error);
                yield break; // Exit, leaving the song list empty. The callback will be handled by the parent.
            }

            string json = www.downloadHandler.text;
            FileList manifest = JsonUtility.FromJson<FileList>(json);
            var allPaths = new HashSet<string>(manifest.paths);
            var midiFiles = allPaths.Where(p => p.EndsWith(".mid") || p.EndsWith(".midi"));

            foreach (string midiFilePath in midiFiles)
            {
                string fullFileName = Path.GetFileName(midiFilePath);

                string relativeMidiPath = midiFilePath.Substring(streamingAssetsPath.Length + 1).Replace('\\', '/');
                string relativeJsonPath = Path.ChangeExtension(relativeMidiPath, ".json");

                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(midiFilePath);
                string directoryName = Path.GetDirectoryName(relativeMidiPath);
                string relativeMetadataPath = Path.Combine(directoryName, fileNameWithoutExt + METADATA_SUFFIX + ".json").Replace('\\', '/');

                // Check for metadata using the full path, as File.Exists needs it.
                string fullMetadataPath = Path.Combine(Application.streamingAssetsPath, relativeMetadataPath);
                bool hasMetadata = File.Exists(fullMetadataPath);

                foundSongs.AddLast(new PartialSongData(
                    fullFileName,
                    relativeMidiPath,
                    relativeJsonPath,
                    hasMetadata ? relativeMetadataPath : null,
                    hasMetadata
                ));
            }
        }
    }

    /// <summary>
    /// Finds MIDI files and their corresponding JSON data on Windows/Editor using direct file access.
    /// </summary>
    private IEnumerator FindMidisWindows(string streamingAssetsPath, LinkedList<PartialSongData> foundSongs)
    {
        // If you're wondering why the `.Where` clause is necessary, see https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.getfiles?view=net-9.0 and its remarks
        // Basically, calling `Directory.GetFiles` with searchPattern `*.abc` returns both `*.abc` files, as well as `*.abc*` files.
        string[] midiFiles = Directory.GetFiles(streamingAssetsPath, "*.mid", SearchOption.AllDirectories).Where(file => file.EndsWith(".mid") || file.EndsWith(".midi")).ToArray();

        foreach (string midiFilePath in midiFiles)
        {
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(midiFilePath);
            string fullFileName = Path.GetFileName(midiFilePath);
            string relativePath = midiFilePath.Substring(streamingAssetsPath.Length + 1).Replace('\\', '/');

            string relativeJsonPath = Path.ChangeExtension(midiFilePath, ".json");
            string expectedMetadataPath = Path.Combine(
                Path.GetDirectoryName(midiFilePath),
                fileNameWithoutExt + METADATA_SUFFIX + ".json"
            );

            bool hasMetadata = File.Exists(expectedMetadataPath);

            foundSongs.AddLast(
                new PartialSongData(
                    fullFileName, 
                    relativePath, 
                    relativeJsonPath, 
                    hasMetadata ? expectedMetadataPath : null,
                    hasMetadata
                )
            );
        }

        yield return null; // Yield once to maintain coroutine structure.
    }

    /// <summary>
    /// Looks through the `StreamingAssets` folder to find all .midi files.
    /// Delegates to platform-specific methods.
    /// </summary>
    public IEnumerator FindMidis(System.Action<LinkedList<PartialSongData>> callback)
    {
        var foundSongs = new LinkedList<PartialSongData>();
        string streamingAssetsPath = Application.streamingAssetsPath;

#if UNITY_ANDROID && !UNITY_EDITOR
        yield return FindMidisAndroid(streamingAssetsPath, foundSongs);
#else
        yield return FindMidisWindows(streamingAssetsPath, foundSongs);
#endif

        // The callback is called exactly once, after the appropriate helper method has finished.
        callback(foundSongs);
    }
}

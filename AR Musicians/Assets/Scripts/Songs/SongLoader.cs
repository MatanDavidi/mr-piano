using Melanchall.DryWetMidi.MusicTheory;
using Meta.Voice.Net.WebSockets;
using Newtonsoft.Json;
using NUnit.Framework.Constraints;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

internal class ISongJson : ISongMetadata { };

internal class SongData : ISongMetadata
{
    public PartialSongData PartialSongData { get; private set; }

    public SongData(string title, string[] artists, short releaseYear, uint duration, PartialSongData partialSongData)
    {
        this.title = title;
        this.releaseYear = releaseYear;
        this.duration = duration;
        PartialSongData = new PartialSongData(partialSongData);

        // Copy array over to prevent side effects in client
        this.artists = new string[artists.Length];
        Array.Copy(artists, this.artists, artists.Length);
    }

    public SongData(ISongMetadata metadata, PartialSongData partialSongData) : this(metadata.title, metadata.artists, metadata.releaseYear, metadata.duration, partialSongData) { }

    public override string ToString()
    {
        return $"{string.Join(", ", this.artists)} - {this.title} [{this.releaseYear}] ({this.duration}s)";
    }
}

public class SongLoader : MonoBehaviour
{
    #region Private members
    private LinkedList<SongData> songs;
    internal static Action<LinkedList<SongData>> OnSongsLoaded;
    #endregion

    #region Serialized members
    [SerializeField]
    private SongsFinder _songsFinder;
    #endregion

    private IEnumerator FetchSongs()
    {
        if (_songsFinder == null)
        {
            throw new MissingComponentException("Could not find necessary 'SongsFinder' component.");
        }
        LinkedList<PartialSongData> foundSongs = new();
        // Fetch songs from SongsFinder
        yield return StartCoroutine(
            _songsFinder.FindMidis(foundMidis => foundSongs = foundMidis)
        );
        // Use data from foundSongs to populate new list of SongData
        LinkedList<SongData> songsData = new();
        songs = new();
        foreach (PartialSongData song in foundSongs)
        {
            SongData songData;
            if (song.HasCorrespondingMetadata)
            {
                // Read the metadata JSON
                string json = File.ReadAllText(song.MetadataPath);
                ISongJson songJson = JsonConvert.DeserializeObject<ISongJson>(json);
                songData = new SongData(
                    songJson,
                    song
                );
            }
            else
            {
                songData = new SongData(
                    song.FileName,
                    new string[] { "n/a" },
                    -1,
                    0,
                    song
                );
            }
            songs.AddLast(songData);
        }
        OnSongsLoaded?.Invoke(songs);
    }

    private void Awake()
    {
        StartCoroutine(FetchSongs());
    }
}

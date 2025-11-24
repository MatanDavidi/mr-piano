using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SongsUIHandler : MonoBehaviour
{
    #region Serialized fields
    [SerializeField]
    private TMP_Text songTitleTMP;
    [SerializeField]
    private TMP_Text songArtistTMP;
    [SerializeField]
    private TMP_Text songReleaseYearTMP;
    [SerializeField]
    private TMP_Text songDurationTMP;
    [SerializeField]
    private TMP_Text songDifficultyTMP;
    #endregion

    public NoteCubeManager notecubemanager;

    #region Private members
    private LinkedList<SongData> songsData;
    #endregion

    #region Properties
    private LinkedListNode<SongData> selectedSong;

    internal LinkedListNode<SongData> SelectedSong
    {
        get { return selectedSong; }
        set
        {
            Debug.Log($"Selected new song: {value.Value}. Updating song selection UI");
            selectedSong = value;
            UpdateUI();
        }
    }

    #endregion
    private void Awake()
    {
        SongLoader.OnSongsLoaded += OnSongsLoaded;
    }

    #region Event handlers
    private void OnSongsLoaded(LinkedList<SongData> songsData)
    {
        this.songsData = songsData;
        SelectedSong = songsData.First;
    }

    public void OnSongSelected()
    {
        notecubemanager.updateSong(SelectedSong.Value.PartialSongData);
    }

    public void OnNextSongSelected()
    {
        SelectedSong = (selectedSong.Next ?? songsData.First);
    }

    public void OnPreviousSongSelected()
    {
        SelectedSong = (selectedSong.Previous ?? songsData.Last);
    }

    private void UpdateUI()
    {
        SongData currentSongData = selectedSong.Value;
        if (currentSongData == null)
        {
            Debug.LogWarning("Expected selected song data. Got null");
            OnNextSongSelected();
            return;
        }
        songTitleTMP.text = currentSongData.title;
        songArtistTMP.text = string.Join(", ", currentSongData.artists);
        songReleaseYearTMP.text = Convert.ToString(currentSongData.releaseYear);
        songDurationTMP.text = Convert.ToString(currentSongData.duration);
        songDifficultyTMP.text = currentSongData.difficulty.ToString();

        //songTitleTMP.ForceMeshUpdate();
        //songArtistTMP.ForceMeshUpdate();
        //songReleaseYearTMP.ForceMeshUpdate();
        //songDurationTMP.ForceMeshUpdate();
        //songDifficultyTMP.ForceMeshUpdate();
    }
    #endregion
}

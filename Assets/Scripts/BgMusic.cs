using UnityEngine;
using System.Collections;

public class BgMusic : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public AudioClip bgmusic;
    public AudioClip victoryClip;  // Tambahan: clip untuk suara kemenangan

    private AudioSource aus;
    void Start()
    {
        // Cek dan ambil AudioSource
        aus = GetComponent<AudioSource>();
        if (aus == null)
        {
            aus = gameObject.AddComponent<AudioSource>();
        }

        // Mainkan musik latar jika tersedia
        if (bgmusic != null)
        {
            aus.clip = bgmusic;
            aus.loop = true;
            aus.playOnAwake = false;
            aus.Play();
        }
        else
        {
            Debug.LogWarning("Background music clip is not assigned!");
        }
    }

    /// <summary>
    /// Memainkan suara kemenangan dengan jeda pendek agar audio dapat berganti sempurna
    /// </summary>
    public void PlayVictorySound()
    {
        StartCoroutine(PlayVictoryAfterDelay());
    }

    private IEnumerator PlayVictoryAfterDelay()
    {
        if (aus.isPlaying)
            aus.Stop();

        yield return new WaitForSeconds(0.1f);

        if (victoryClip != null)
        {
            aus.loop = false;
            aus.clip = victoryClip;
            aus.Play();
        }
    }
}

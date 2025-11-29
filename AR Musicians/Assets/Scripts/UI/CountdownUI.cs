using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CountdownUI : MonoBehaviour
{
    public GameObject countdown;
    public TextMeshProUGUI countdownText;
    public Image countdownCircle;

    public NoteCubeManager notecubemanager;

    public float countdownTime = 3f;

    public void StartCountdown()
    {
        countdown.SetActive(true);
        StartCoroutine(PlayCountdownRoutine());
    }

    public void ResumeCountdown()
    {
        countdown.SetActive(true);
        StartCoroutine(ResumeCountdownRoutine());
    }

    private IEnumerator PlayCountdownRoutine()
    {
        float timeLeft = countdownTime;

        while (timeLeft > 0)
        {
            // Update number display
            countdownText.text = Mathf.Ceil(timeLeft).ToString();

            // Update circle fill (1 to 0)
            countdownCircle.fillAmount = timeLeft % 1f;

            timeLeft -= Time.deltaTime;
            yield return null;
        }

        // Final "GO!"
        countdownCircle.fillAmount = 0f;
        countdownText.text = "GO!";

        yield return new WaitForSeconds(0.5f);

        // Hide the countdown UI when done
        countdown.SetActive(false);


        notecubemanager.Play();
    }

    private IEnumerator ResumeCountdownRoutine()
    {
        float timeLeft = countdownTime;

        while (timeLeft > 0)
        {
            // Update number display
            countdownText.text = Mathf.Ceil(timeLeft).ToString();

            // Update circle fill (1 to 0)
            countdownCircle.fillAmount = timeLeft % 1f;

            timeLeft -= Time.deltaTime;
            yield return null;
        }

        // Final "GO!"
        countdownCircle.fillAmount = 0f;
        countdownText.text = "GO!";

        yield return new WaitForSeconds(0.5f);

        // Hide the countdown UI when done
        countdown.SetActive(false);


        notecubemanager.Resume();
    }
}
